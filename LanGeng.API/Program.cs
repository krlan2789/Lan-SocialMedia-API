using System.Text;
using LanGeng.API.Data;
using LanGeng.API.Middlewares;
using LanGeng.API.Seeders;
using LanGeng.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

        // Add Database Context services to the container
        builder.Services
            .AddDbContext<SocialMediaDatabaseContext>(option =>
            {
                option.UseSqlServer("" + builder.Configuration.GetConnectionString("DefaultConnection"));
            });

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
        // builder.Services
        //     .AddCors(options =>
        //     {
        //         options.AddPolicy("AllowSpecificOrigins", builder =>
        //         {
        //             builder
        //                 .WithOrigins("http://localhost")
        //                 .AllowAnyMethod()
        //                 .AllowAnyHeader()
        //                 .SetIsOriginAllowed(origin => true)
        //                 .WithHeaders("Content-Type", "Authorization");
        //         });
        //     });
        builder.Services.AddSingleton<TokenService>();
        builder.Services.AddTransient<EmailService>();
        builder.Services.AddAuthorization();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Migrate the database
        if (app.Environment.IsDevelopment())
        {
            // Register the endpoint for viewing the OpenAPI Documentation
            string openApiRoute = "/api/docs/{documentName}/openapi.json";
            app.MapOpenApi(openApiRoute);
            app.MapScalarApiReference(options =>
            {
                string scalarApiRoute = "/api/docs/{documentName}";
                options
                    .WithTheme(ScalarTheme.BluePlanet)
                    .WithEndpointPrefix(scalarApiRoute)
                    .WithOpenApiRoutePattern(openApiRoute)
                    .WithTitle((builder.Configuration["AppName"] ?? "LanGeng") + " - REST API");
            });

            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            await DatabaseSeeder.Seed(services);
        }

        // Configure the HTTP request pipeline
        app.UseHsts();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCookiePolicy();
        app.UseRouting();
        // app.UseCors("AllowSpecificOrigins");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<AuthMiddleware>();
        app.UseMiddleware<UserSessionLoggingMiddleware>();
        app.MapControllers();

        app.Run();
    }
}
