using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay_.Api.Data;
using Microsoft.AspNetCore.Identity;
using RaceDay_.Api.Models;
using RaceDay_.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers.
builder.Services.AddControllers();

// Register Swagger/OpenAPI.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description =
            "RESTful API for the RaceDay running, walking and cycling event management system."
    });
});

// Configure the SQL Server connection.
var connectionString =
    builder.Configuration.GetConnectionString("RaceDayConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "The RaceDayConnection connection string is missing from appsettings.json.");
}

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register session storage.
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);

    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Register authorization services.
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Enable Swagger in the Development environment.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Session middleware must run before controllers access session state.
app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Makes the entry point accessible to the automated test project.
public partial class Program
{
}