using System.Text;
using LanGeng.API.Data;
using LanGeng.API.Interfaces;
using LanGeng.API.Middlewares;
using LanGeng.API.Seeders;
using LanGeng.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

namespace LanGeng.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add configuration to the container
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Define connection strings
        var sqliteConnection = builder.Configuration.GetConnectionString("LanGeng_Prod");
        if (builder.Environment.IsDevelopment()) sqliteConnection = builder.Configuration.GetConnectionString("LanGeng");
        // var sqlServerConnection = builder.Configuration.GetConnectionString("DefaultConnection");

        // // Attempt SQL Server connection
        // bool isSqlServerAvailable = false;
        // try
        // {
        //     using var connection = new SqlConnection(sqlServerConnection);
        //     connection.Open();
        //     isSqlServerAvailable = true;
        //     Console.WriteLine("✅ SQL Server is available, using SQL Server.");
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine($"⚠ SQL Server not available, switching to SQLite: {ex.Message}");
        // }

        // // Add Database Context services to the container based on availability
        // if (isSqlServerAvailable)
        // {
        //     builder.Services.AddDbContext<SocialMediaDatabaseContext>(options => options.UseSqlServer(sqlServerConnection));
        // }
        // else
        {
            builder.Services.AddDbContext<SocialMediaDatabaseContext>(options => options.UseSqlite(sqliteConnection));
        }

        // Add services to the container.
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "" + builder.Configuration["Jwt:Issuer"],
                    ValidAudience = "" + builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("" + builder.Configuration["Jwt:SecretKey"]))
                };
            });
        builder.Services
            .AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins", builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .SetIsOriginAllowed(origin => true);
                });
            });
        builder.Services.AddSingleton<ITokenService, TokenService>();
        builder.Services.AddTransient<IEmailService, EmailService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IPostService, PostService>();
        builder.Services.AddScoped<IGroupService, GroupService>();
        builder.Services.AddAuthorization();
        builder.Services
            .AddControllers()
            .AddNewtonsoftJson();
        builder.Services
            .AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });
        builder.Services
            .AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // if (app.Environment.IsDevelopment())
        // {
        // }
        // Register the endpoint for viewing the OpenAPI Documentation
        string openApiRoute = "/api/docs/{documentName}.json";
        app.MapOpenApi(openApiRoute);
        app.MapScalarApiReference(options =>
        {
            string scalarApiRoute = "/api/docs/{documentName}";
            options
                .WithTheme(ScalarTheme.BluePlanet)
                .WithEndpointPrefix(scalarApiRoute)
                .WithOpenApiRoutePattern(openApiRoute)
                .WithTitle((builder.Configuration["AppName"] ?? "LanGeng") + " - REST API");
            foreach (var description in app.Services.GetRequiredService<IApiVersionDescriptionProvider>().ApiVersionDescriptions)
            {
                options.WithOpenApiRoutePattern(openApiRoute.Replace("{documentName}", description.GroupName));
            }
        });
        // Migrate the database
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        await DatabaseSeeder.Seed(services);

        // Configure the HTTP request pipeline
        app.UseCors();
        app.UseHsts();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCookiePolicy();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<AuthMiddleware>();
        app.UseMiddleware<UserSessionLoggingMiddleware>();
        app.MapControllers();

        app.Run();
    }
}
