using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Users;

/// <summary>
/// Defines endpoints related to user registration.
/// </summary>
public static class UserEndpoints
{
    /// <summary>
    /// Maps user-related endpoints.
    /// </summary>
    /// <param name="app">The application route builder.</param>
    /// <returns>The route builder.</returns>
    public static IEndpointRouteBuilder MapUserEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .WithName("RegisterUser");

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager)
    {
        List<ValidationResult> validationResults = [];

        bool isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        if (!isValid)
        {
            Dictionary<string, string[]> errors = validationResults
                .GroupBy(
                    result =>
                        result.MemberNames.FirstOrDefault()
                        ?? string.Empty)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(
                            result =>
                                result.ErrorMessage
                                ?? "Invalid value.")
                        .ToArray());

            return Results.ValidationProblem(errors);
        }

        ApplicationUser? existingUser =
            await userManager.FindByEmailAsync(request.Email);

        if (existingUser is not null)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["Email"] =
                    [
                        "A user with this email already exists."
                    ]
                });
        }

        ApplicationUser user = new()
        {
            UserName = request.Email,
            Email = request.Email
        };

        IdentityResult result =
            await userManager.CreateAsync(
                user,
                request.Password);

        if (!result.Succeeded)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["Identity"] = result.Errors
                        .Select(error => error.Description)
                        .ToArray()
                });
        }

        return Results.Created(
            $"/api/admin/users/{user.Id}",
            new
            {
                user.Id,
                user.Email
            });
    }
}
