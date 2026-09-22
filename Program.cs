using Hotel_Management_System_Backend_dotNet.Data;
using Hotel_Management_System_Backend_dotNet.Data.Configuration;
using Hotel_Management_System_Backend_dotNet.Data.Seeds;
using Hotel_Management_System_Backend_dotNet.Entities;
using Hotel_Management_System_Backend_dotNet.Entities.Authorization;
using Hotel_Management_System_Backend_dotNet.Services.Implementations;
using Hotel_Management_System_Backend_dotNet.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("PostgreSql");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("Cloudinary"));

builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

builder
    .Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();


var frontendBaseUrl =
    builder.Configuration["Frontend:BaseUrl"]
    ?? throw new InvalidOperationException(
        "Frontend base URL is not configured in appsettings.json or environment variables."
    );
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

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Velora.Auth";

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.SameSite =
        SameSiteMode.None;

    options.ExpireTimeSpan =
        TimeSpan.FromDays(14);

    options.SlidingExpiration = true;
});

builder.Services.AddAuthentication();

builder
    .Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.SuperAdmin, Roles.Admin))
    .AddPolicy(
        "StaffAccess",
        policy => policy.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Staff)
    );

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedAsync(roleManager);
}

/*below 4 lines should be in this order to work properly*/
app.UseHttpsRedirection();
app.UseCors("NextJsFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapIdentityApi<ApplicationUser>();

app.Run();
