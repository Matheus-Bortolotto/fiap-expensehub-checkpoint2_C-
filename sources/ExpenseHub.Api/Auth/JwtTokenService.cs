using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpenseHub.Api.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Creates JSON Web Tokens for authenticated ExpenseHub users.
/// </summary>
public sealed class JwtTokenService(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Creates a bearer token for an authenticated user.
    /// </summary>
    public LoginResponse CreateToken(
        ApplicationUser user,
        IReadOnlyCollection<string> roles)
    {
        string key =
            _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key configuration was not found.");

        string issuer =
            _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "Jwt:Issuer configuration was not found.");

        string audience =
            _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "Jwt:Audience configuration was not found.");

        int expirationMinutes =
            _configuration.GetValue<int>("Jwt:ExpirationMinutes");

        DateTime expiresAtUtc =
            DateTime.UtcNow.AddMinutes(expirationMinutes);

        List<Claim> claims =
        [
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id),

            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id),

            new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email ?? string.Empty)
        ];

        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        SymmetricSecurityKey securityKey =
            new(Encoding.UTF8.GetBytes(key));

        SigningCredentials credentials =
            new(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token =
            new(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

        return new LoginResponse
        {
            AccessToken =
                new JwtSecurityTokenHandler().WriteToken(token),

            ExpiresAtUtc = expiresAtUtc,
            Roles = roles
        };
    }
}
