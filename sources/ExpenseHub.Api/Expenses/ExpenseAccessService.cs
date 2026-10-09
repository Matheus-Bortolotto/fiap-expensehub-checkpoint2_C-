using System;
using System.Linq;
using System.Security.Claims;
using ExpenseHub.Api.Domain.Entities;
using ExpenseHub.Api.Domain.Enums;
using ExpenseHub.Api.Identity;

namespace ExpenseHub.Api.Expenses;

/// <summary>
/// Applies the contextual read rules for expense resources.
/// </summary>
public static class ExpenseAccessService
{
    /// <summary>
    /// Filters an expense query according to the authenticated user's roles.
    /// </summary>
    /// <param name="expenses">The database query to filter.</param>
    /// <param name="user">The authenticated user.</param>
    /// <returns>A query containing only expenses visible to the user.</returns>
    public static IQueryable<Expense> ApplyVisibility(
        IQueryable<Expense> expenses,
        ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(expenses);
        ArgumentNullException.ThrowIfNull(user);

        string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        bool employee = user.IsInRole(ApplicationRoles.Employee);
        bool approver = user.IsInRole(ApplicationRoles.Approver);
        bool finance = user.IsInRole(ApplicationRoles.Finance);
        bool auditor = user.IsInRole(ApplicationRoles.Auditor);

        if (auditor)
        {
            return expenses;
        }

        return expenses.Where(expense =>
            (employee && expense.OwnerId == userId) ||
            (approver && expense.Status == ExpenseStatus.Submitted) ||
            (finance &&
                (expense.Status == ExpenseStatus.Approved ||
                 expense.Status == ExpenseStatus.Paid)));
    }

    /// <summary>
    /// Gets the authenticated user's required identifier.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>The user identifier contained in the bearer token.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token does not contain an identifier.
    /// </exception>
    public static string GetRequiredUserId(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException(
                "The authenticated user identifier was not found.");
    }
}
