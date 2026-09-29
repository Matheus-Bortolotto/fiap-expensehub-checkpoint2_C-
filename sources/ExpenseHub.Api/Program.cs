using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ExpenseHub.Api;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        string connectionString =
            builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        string jwtKey =
            builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key configuration was not found.");

        string jwtIssuer =
            builder.Configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "Jwt:Issuer configuration was not found.");

        string jwtAudience =
            builder.Configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "Jwt:Audience configuration was not found.");

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtKey))
                    };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddSingleton<JwtTokenService>();

        builder.Services.AddOpenApi();

        WebApplication app = builder.Build();

        using (IServiceScope scope = app.Services.CreateScope())
        {
            await IdentitySeeder.SeedAsync(scope.ServiceProvider);
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet(
                "/health",
                () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth");

        app.MapPost(
            "/login",
            async (
                LoginRequest request,
                UserManager<ApplicationUser> userManager,
                JwtTokenService tokenService) =>
            {
                ApplicationUser? user =
                    await userManager.FindByEmailAsync(request.Email);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                bool passwordIsValid =
                    await userManager.CheckPasswordAsync(
                        user,
                        request.Password);

                if (!passwordIsValid)
                {
                    return Results.Unauthorized();
                }

                IList<string> userRoles =
                    await userManager.GetRolesAsync(user);

                LoginResponse response =
                    tokenService.CreateToken(
                        user,
                        [.. userRoles]);

                return Results.Ok(response);
            });

        await app.RunAsync();
    }
}
