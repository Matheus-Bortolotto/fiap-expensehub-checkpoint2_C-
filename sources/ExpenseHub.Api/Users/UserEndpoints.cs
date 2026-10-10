using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExpenseHub.Api.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Users;

/// <summary>
/// Defines endpoints related to user registration and administration.
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

        app.MapGet("/api/admin/users", GetUsersAsync)
            .RequireAuthorization(
                policy => policy.RequireRole(ApplicationRoles.Admin))
            .WithName("GetUsers");

        app.MapPut(
                "/api/admin/users/{id}/roles",
                UpdateUserRolesAsync)
            .RequireAuthorization(
                policy => policy.RequireRole(ApplicationRoles.Admin))
            .WithName("UpdateUserRoles");

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

    private static async Task<IResult> GetUsersAsync(
        UserAdministrationService userAdministrationService)
    {
        IReadOnlyList<UserResponse> users =
            await userAdministrationService.GetUsersAsync();

        return Results.Ok(users);
    }

    private static async Task<IResult> UpdateUserRolesAsync(
        string id,
        UpdateUserRolesRequest request,
        ClaimsPrincipal principal,
        UserAdministrationService userAdministrationService)
    {
        string? actorUserId =
            principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return Results.Unauthorized();
        }

        if (request.Roles is null)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["Roles"] =
                    [
                        "The roles field is required."
                    ]
                });
        }

        UpdateUserRolesResult result =
            await userAdministrationService.UpdateRolesAsync(
                id,
                request.Roles,
                actorUserId);

        return result switch
        {
            UpdateUserRolesResult.Success =>
                Results.NoContent(),

            UpdateUserRolesResult.UserNotFound =>
                Results.NotFound(),

            UpdateUserRolesResult.InvalidRole =>
                Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["Roles"] =
                        [
                            "One or more roles are invalid."
                        ]
                    }),

            UpdateUserRolesResult.CannotRemoveOwnAdminRole =>
                Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["Roles"] =
                        [
                            "An Admin cannot remove their own Admin role."
                        ]
                    }),

            _ =>
                Results.Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Unable to update the user's roles.")
        };
    }
}
