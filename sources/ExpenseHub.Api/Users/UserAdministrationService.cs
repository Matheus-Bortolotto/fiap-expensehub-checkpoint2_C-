using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Users;

/// <summary>
/// Provides administrative operations for users and their roles.
/// </summary>
public sealed class UserAdministrationService
{
    private readonly UserManager<ApplicationUser> _userManager;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="UserAdministrationService"/> class.
    /// </summary>
    /// <param name="userManager">
    /// The ASP.NET Core Identity user manager.
    /// </param>
    public UserAdministrationService(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Gets all registered users and their assigned roles.
    /// </summary>
    /// <returns>
    /// A list containing the registered users and their roles.
    /// </returns>
    public async Task<IReadOnlyList<UserResponse>> GetUsersAsync()
    {
        List<ApplicationUser> users =
            await _userManager.Users
                .OrderBy(user => user.Email)
                .ToListAsync();

        List<UserResponse> response = [];

        foreach (ApplicationUser user in users)
        {
            IList<string> roles =
                await _userManager.GetRolesAsync(user);

            response.Add(
                new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    Roles = [.. roles]
                });
        }

        return response;
    }

    /// <summary>
    /// Replaces the roles assigned to a user.
    /// </summary>
    /// <param name="userId">
    /// The identifier of the user whose roles will be changed.
    /// </param>
    /// <param name="roles">
    /// The complete set of roles that should remain assigned.
    /// </param>
    /// <param name="actorUserId">
    /// The identifier of the Admin performing the operation.
    /// </param>
    /// <returns>
    /// A result describing whether the operation succeeded.
    /// </returns>
    public async Task<UpdateUserRolesResult> UpdateRolesAsync(
        string userId,
        IEnumerable<string> roles,
        string actorUserId)
    {
        ApplicationUser? user =
            await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return UpdateUserRolesResult.UserNotFound;
        }

        string[] requestedRoles = roles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        bool containsInvalidRole = requestedRoles.Any(
            role =>
                !ApplicationRoles.All.Contains(
                    role,
                    StringComparer.OrdinalIgnoreCase));

        if (containsInvalidRole)
        {
            return UpdateUserRolesResult.InvalidRole;
        }

        bool actorIsUpdatingOwnAccount =
            string.Equals(
                user.Id,
                actorUserId,
                StringComparison.Ordinal);

        bool keepsAdminRole = requestedRoles.Contains(
            ApplicationRoles.Admin,
            StringComparer.OrdinalIgnoreCase);

        if (actorIsUpdatingOwnAccount &&
            !keepsAdminRole)
        {
            return UpdateUserRolesResult.CannotRemoveOwnAdminRole;
        }

        IList<string> currentRoles =
            await _userManager.GetRolesAsync(user);

        string[] rolesToRemove = currentRoles
            .Where(
                currentRole =>
                    !requestedRoles.Contains(
                        currentRole,
                        StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (rolesToRemove.Length > 0)
        {
            IdentityResult removeResult =
                await _userManager.RemoveFromRolesAsync(
                    user,
                    rolesToRemove);

            if (!removeResult.Succeeded)
            {
                return UpdateUserRolesResult.IdentityError;
            }
        }

        string[] rolesToAdd = requestedRoles
            .Where(
                requestedRole =>
                    !currentRoles.Contains(
                        requestedRole,
                        StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (rolesToAdd.Length > 0)
        {
            IdentityResult addResult =
                await _userManager.AddToRolesAsync(
                    user,
                    rolesToAdd);

            if (!addResult.Succeeded)
            {
                return UpdateUserRolesResult.IdentityError;
            }
        }

        return UpdateUserRolesResult.Success;
    }
}

/// <summary>
/// Represents the result of a user role update operation.
/// </summary>
public enum UpdateUserRolesResult
{
    /// <summary>
    /// The roles were updated successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The requested user does not exist.
    /// </summary>
    UserNotFound,

    /// <summary>
    /// At least one requested role is invalid.
    /// </summary>
    InvalidRole,

    /// <summary>
    /// An Admin attempted to remove their own Admin role.
    /// </summary>
    CannotRemoveOwnAdminRole,

    /// <summary>
    /// ASP.NET Core Identity failed to apply the requested change.
    /// </summary>
    IdentityError
}
