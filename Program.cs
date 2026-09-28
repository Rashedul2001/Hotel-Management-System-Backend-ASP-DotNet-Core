using System.Security.Claims;
using Hotel_Management_System_Backend_dotNet.Data;
using Hotel_Management_System_Backend_dotNet.Data.Configuration;
using Hotel_Management_System_Backend_dotNet.Data.Seeds;
using Hotel_Management_System_Backend_dotNet.Entities;
using Hotel_Management_System_Backend_dotNet.Entities.Authorization;
using Hotel_Management_System_Backend_dotNet.Services.Implementations;
using Hotel_Management_System_Backend_dotNet.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("PostgreSql");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings")
);

builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// ---------------------------------------------------------
// ASP.NET CORE IDENTITY
// ---------------------------------------------------------

builder
    .Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ---------------------------------------------------------
// FRONTEND URL
// ---------------------------------------------------------

var frontendBaseUrl =
    builder.Configuration["Frontend:BaseUrl"]
    ?? throw new InvalidOperationException(
        "Frontend base URL is not configured in appsettings.json or environment variables."
    );

// ---------------------------------------------------------
// CORS
// ---------------------------------------------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "NextJsFrontend",
        policy =>
        {
            policy
                .WithOrigins(frontendBaseUrl)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});

// ---------------------------------------------------------
// AUTHENTICATION
// ---------------------------------------------------------
//
// IMPORTANT:
// AddIdentityApiEndpoints already configures Identity authentication.
//
// We additionally register the external OAuth/OIDC schemes below.
// The external providers will temporarily authenticate the user
// using IdentityConstants.ExternalScheme.
//
// ---------------------------------------------------------

builder
    .Services.AddAuthentication()
    .AddGoogle(
        GoogleDefaults.AuthenticationScheme,
        options =>
        {
            options.ClientId =
                builder.Configuration["Authentication:Google:ClientId"]
                ?? throw new InvalidOperationException("Google ClientId is not configured.");

            options.ClientSecret =
                builder.Configuration["Authentication:Google:ClientSecret"]
                ?? throw new InvalidOperationException("Google ClientSecret is not configured.");

            // Google already requests openid/profile/email.
            // We explicitly map the profile image claim.
            options.ClaimActions.MapJsonKey("urn:velora:profile_picture", "picture");

            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");

            // Keep the temporary external identity in Identity's
            // external cookie so the AuthController can process it.
            options.SignInScheme = IdentityConstants.ExternalScheme;
        }
    )
    .AddFacebook(
        FacebookDefaults.AuthenticationScheme,
        options =>
        {
            options.AppId =
                builder.Configuration["Authentication:Facebook:AppId"]
                ?? throw new InvalidOperationException("Facebook AppId is not configured.");

            options.AppSecret =
                builder.Configuration["Authentication:Facebook:AppSecret"]
                ?? throw new InvalidOperationException("Facebook AppSecret is not configured.");

            options.SignInScheme = IdentityConstants.ExternalScheme;

            // Facebook's ASP.NET Core handler already requests email
            // and basic identity fields.
            //
            // We add the picture field because we want the user's
            // Facebook profile image.
            options.Fields.Clear();
            options.Fields.Add("id");
            options.Fields.Add("name");
            options.Fields.Add("email");
            options.Fields.Add("first_name");
            options.Fields.Add("last_name");
            options.Fields.Add("picture.type(large)");

            // Facebook's handler maps the normal name/email claims.
            //
            // PROJECT-SPECIFIC NOTE:
            // The profile-picture JSON is provider-specific, so we
            // extract it later in AuthController if necessary.
        }
    )
    .AddOAuth(
        "LinkedIn",
        options =>
        {
            options.ClientId =
                builder.Configuration["Authentication:LinkedIn:ClientId"]
                ?? throw new InvalidOperationException("LinkedIn ClientId is not configured.");

            options.ClientSecret =
                builder.Configuration["Authentication:LinkedIn:ClientSecret"]
                ?? throw new InvalidOperationException("LinkedIn ClientSecret is not configured.");

            // ---------------------------------------------------------
            // LinkedIn OAuth 2.0 endpoints
            // ---------------------------------------------------------

            options.AuthorizationEndpoint = "https://www.linkedin.com/oauth/v2/authorization";

            options.TokenEndpoint = "https://www.linkedin.com/oauth/v2/accessToken";

            options.UserInformationEndpoint = "https://api.linkedin.com/v2/userinfo";

            options.CallbackPath = "/signin-linkedin";

            options.SignInScheme = IdentityConstants.ExternalScheme;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;

            // ---------------------------------------------------------
            // LinkedIn OIDC scopes
            // ---------------------------------------------------------

            options.Scope.Clear();

            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");

            // ---------------------------------------------------------
            // IMPORTANT:
            //
            // LinkedIn expects client credentials during the token
            // request.
            //
            // We explicitly send them as form parameters.
            // ---------------------------------------------------------

            options.Events.OnCreatingTicket = async context =>
            {
                // -----------------------------------------------------
                // The OAuth handler has already exchanged the code for
                // an access token.
                //
                // Now request LinkedIn's OIDC userInfo endpoint.
                // -----------------------------------------------------

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    "https://api.linkedin.com/v2/userinfo"
                );

                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        context.AccessToken
                    );

                request.Headers.Accept.ParseAdd("application/json");

                using var response = await context.Backchannel.SendAsync(request);

                response.EnsureSuccessStatusCode();

                using var document = System.Text.Json.JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync()
                );

                var root = document.RootElement;

                // -----------------------------------------------------
                // sub
                // -----------------------------------------------------

                if (root.TryGetProperty("sub", out var sub))
                {
                    context.Identity?.AddClaim(
                        new Claim(ClaimTypes.NameIdentifier, sub.GetString() ?? string.Empty)
                    );
                }

                // -----------------------------------------------------
                // name
                // -----------------------------------------------------

                if (root.TryGetProperty("name", out var name))
                {
                    context.Identity?.AddClaim(
                        new Claim(ClaimTypes.Name, name.GetString() ?? string.Empty)
                    );
                }

                // -----------------------------------------------------
                // email
                // -----------------------------------------------------

                if (root.TryGetProperty("email", out var email))
                {
                    context.Identity?.AddClaim(
                        new Claim(ClaimTypes.Email, email.GetString() ?? string.Empty)
                    );
                }

                // -----------------------------------------------------
                // profile picture
                // -----------------------------------------------------

                if (root.TryGetProperty("picture", out var picture))
                {
                    var pictureUrl = picture.GetString();

                    if (!string.IsNullOrWhiteSpace(pictureUrl))
                    {
                        context.Identity?.AddClaim(
                            new Claim("urn:velora:profile_picture", pictureUrl)
                        );
                    }
                }
            };
        }
    )
    .AddOAuth(
        "GitHub",
        options =>
        {
            options.ClientId =
                builder.Configuration["Authentication:GitHub:ClientId"]
                ?? throw new InvalidOperationException("GitHub ClientId is not configured.");

            options.ClientSecret =
                builder.Configuration["Authentication:GitHub:ClientSecret"]
                ?? throw new InvalidOperationException("GitHub ClientSecret is not configured.");

            options.CallbackPath = "/signin-github";

            options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";

            options.TokenEndpoint = "https://github.com/login/oauth/access_token";

            options.UserInformationEndpoint = "https://api.github.com/user";

            options.SignInScheme = IdentityConstants.ExternalScheme;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;

            options.Scope.Add("read:user");
            options.Scope.Add("user:email");

            options.SaveTokens = false;

            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");

            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");

            options.ClaimActions.MapJsonKey("urn:github:login", "login");

            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

            options.ClaimActions.MapJsonKey("urn:velora:profile_picture", "avatar_url");

            options.Events.OnCreatingTicket = async context =>
            {
                // 1) Load the GitHub profile. The generic OAuth handler does NOT do this
                //    for us, so ClaimActions would otherwise have no data to map.
                using (var userRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    context.Options.UserInformationEndpoint))
                {
                    userRequest.Headers.Accept.ParseAdd("application/vnd.github+json");
                    userRequest.Headers.UserAgent.ParseAdd("HotelVelora/1.0");
                    userRequest.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue(
                            "Bearer",
                            context.AccessToken
                        );

                    using var userResponse = await context.Backchannel.SendAsync(
                        userRequest,
                        context.HttpContext.RequestAborted
                    );

                    userResponse.EnsureSuccessStatusCode();

                    using var userJson = System.Text.Json.JsonDocument.Parse(
                        await userResponse.Content.ReadAsStringAsync(context.HttpContext.RequestAborted)
                    );

                    // Applies the MapJsonKey(...) mappings configured above
                    // (id, name, login, email, avatar_url).
                    context.RunClaimActions(userJson.RootElement);
                }

                // 2) GitHub returns a null email when it is private,
                //    so fall back to /user/emails (your existing logic).
                var emailClaim = context.Identity?.FindFirst(ClaimTypes.Email)?.Value;

                if (!string.IsNullOrWhiteSpace(emailClaim))
                {
                    return;
                }

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    "https://api.github.com/user/emails"
                );

                request.Headers.Accept.ParseAdd("application/vnd.github+json");
                request.Headers.UserAgent.ParseAdd("HotelVelora/1.0");
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        context.AccessToken
                    );

                using var response = await context.Backchannel.SendAsync(
                    request,
                    context.HttpContext.RequestAborted
                );

                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

                using var document = System.Text.Json.JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted)
                );

                foreach (var item in document.RootElement.EnumerateArray())
                {
                    var isPrimary =
                        item.TryGetProperty("primary", out var primary) && primary.GetBoolean();

                    var isVerified =
                        item.TryGetProperty("verified", out var verified) && verified.GetBoolean();

                    if (!isPrimary || !isVerified)
                    {
                        continue;
                    }

                    if (
                        item.TryGetProperty("email", out var emailElement)
                        && !string.IsNullOrWhiteSpace(emailElement.GetString())
                    )
                    {
                        context.Identity?.AddClaim(
                            new Claim(ClaimTypes.Email, emailElement.GetString()!)
                        );

                        break;
                    }
                }
            };
        }
    );

// ---------------------------------------------------------
// APPLICATION COOKIE
// ---------------------------------------------------------
//
// IMPORTANT FOR CURRENT HTTP LOCAL DEVELOPMENT:
//
// old code used:
// CookieSecurePolicy.Always
//
// That is correct for production HTTPS,
// but current local environment is HTTP.
//
// SameAsRequest allows:
// http://localhost -> non-secure cookie
//
// Production:
// https://your-domain -> secure cookie
// ---------------------------------------------------------

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Velora.Auth";

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

    options.Cookie.SameSite = SameSiteMode.Lax;

    options.ExpireTimeSpan = TimeSpan.FromDays(1);

    options.SlidingExpiration = false;

    options.Events.OnSigningIn = context =>
    {
        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.Add(
            context.Properties.IsPersistent
                ? TimeSpan.FromDays(30)
                : TimeSpan.FromDays(1)
        );

        return Task.CompletedTask;
    };

    // When authentication fails for an API request,
    // don't redirect the browser to an HTML login page.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// The external Identity cookie is used between the provider callback and
// AuthController. It must be usable over HTTP during local development.
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.Name = "Velora.External";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ---------------------------------------------------------
// AUTHORIZATION
// ---------------------------------------------------------

builder
    .Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.SuperAdmin, Roles.Admin))
    .AddPolicy(
        "StaffAccess",
        policy => policy.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Staff)
    );

// ---------------------------------------------------------
// API MODEL VALIDATION
// ---------------------------------------------------------

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var env = builder.Environment;

        IEnumerable<object> errors = [];

        if (env.IsDevelopment())
        {
            errors = context
                .ModelState.Where(e => e.Value != null && e.Value.Errors.Count > 0)
                .Select(e => new
                {
                    Field = e.Key,
                    Error = e.Value?.Errors.First()?.ErrorMessage ?? "Unknown error",
                });
        }

        return new BadRequestObjectResult(
            new { message = "Validation failed. Please check your input.", errors }
        );
    };
});

var app = builder.Build();

// ---------------------------------------------------------
// DEVELOPMENT OPENAPI
// ---------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ---------------------------------------------------------
// ROLE SEEDING
// ---------------------------------------------------------

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    await RoleSeeder.SeedAsync(roleManager);
}

// ---------------------------------------------------------
// HTTP PIPELINE
// ---------------------------------------------------------
//
// IMPORTANT:
//
// Current local backend is HTTP.
//
// Do NOT use UseHttpsRedirection() during this HTTP OAuth
// development setup.
//
// For production HTTPS, enable it again.
// ---------------------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("NextJsFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapIdentityApi<ApplicationUser>();

app.Run();
