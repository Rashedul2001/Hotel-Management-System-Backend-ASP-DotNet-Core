using Hotel_Management_System_Backend_dotNet.Data;
using Hotel_Management_System_Backend_dotNet.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

/*using postgresql database you can change it to any database you want

* if you change the database you need to change the UseNpgsql to the corresponding database provider,
  for example UseSqlServer for SQL Server, UseMySql for MySQL, etc. 
*/
var connectionString = builder.Configuration.GetConnectionString("PostgreSql");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var frontendBaseUrl = builder.Configuration["Frontend:BaseUrl"]?? "http://localhost:3000"; // Default to localhost if not set in configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("NextJsFrontend", policy =>
    {
        policy
            .WithOrigins(frontendBaseUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddControllers();

/* Configure custom error response globally 
 *without this the ASP .NET Core will return a default error response for validation errors, 
 *which may not be user-friendly or consistent with your API's error handling strategy 
 */

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        //detail information are only for development environment
        var env = builder.Environment;
        IEnumerable<object> errors = [];
        if (env.IsDevelopment())
        {
            
            errors = context.ModelState
                .Where(e => e.Value != null && e.Value.Errors.Count > 0)
                .Select(e => new { Field = e.Key, Error = e.Value?.Errors.First()?.ErrorMessage ?? "Unknown error" });

        }


        return new BadRequestObjectResult(new
        {
            message = "Validation failed. Please check your input.",
            errors
        });
    };
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{

    app.MapOpenApi();
}

app.UseHttpsRedirection();

/*below 3 lines should be in this order to work properly */
app.UseCors("NextJsFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapIdentityApi<ApplicationUser>();


app.Run();
