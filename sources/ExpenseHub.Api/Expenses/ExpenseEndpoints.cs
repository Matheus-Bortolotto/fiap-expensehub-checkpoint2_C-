using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain.Entities;
using ExpenseHub.Api.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Expenses;

/// <summary>
/// Maps endpoints for creating and editing expense drafts.
/// </summary>
public static class ExpenseEndpoints
{
    /// <summary>
    /// Maps expense draft endpoints.
    /// </summary>
    /// <param name="app">The application route builder.</param>
    /// <returns>The supplied route builder.</returns>
    public static IEndpointRouteBuilder MapExpenseEndpoints(
        this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder expenses =
            app.MapGroup("/api/expenses")
                .RequireAuthorization();

        expenses.MapPost(
                string.Empty,
                CreateAsync)
            .RequireAuthorization(
                policy =>
                    policy.RequireRole(
                        ApplicationRoles.Employee))
            .WithName("CreateExpense");

        expenses.MapPut(
                "/{id:int}",
                UpdateAsync)
            .RequireAuthorization(
                policy =>
                    policy.RequireRole(
                        ApplicationRoles.Employee))
            .WithName("UpdateExpense");

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateExpenseRequest request,
        ClaimsPrincipal user,
        ExpenseDraftService draftService,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> validationErrors =
            ValidateRequest(request);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        string ownerId =
            ExpenseAccessService.GetRequiredUserId(user);

        ExpenseDraftResult result =
            await draftService.CreateAsync(
                request,
                ownerId,
                cancellationToken);

        if (result.Outcome ==
            ExpenseDraftOutcome.InvalidCategory)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["ExpenseCategoryId"] =
                    [
                        "The selected expense category does not exist."
                    ]
                });
        }

        if (result.Outcome ==
            ExpenseDraftOutcome.InvalidExpenseDate)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["ExpenseDate"] =
                    [
                        "The expense date cannot be in the future."
                    ]
                });
        }

        if (result.Outcome !=
                ExpenseDraftOutcome.Success ||
            result.Expense is null)
        {
            return Results.StatusCode(
                StatusCodes.Status500InternalServerError);
        }

        ExpenseResponse response =
            ToResponse(result.Expense);

        return Results.Created(
            $"/api/expenses/{response.Id}",
            response);
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateExpenseRequest request,
        ClaimsPrincipal user,
        ExpenseDraftService draftService,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> validationErrors =
            ValidateRequest(request);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        string ownerId =
            ExpenseAccessService.GetRequiredUserId(user);

        ExpenseDraftResult result =
            await draftService.UpdateAsync(
                id,
                request,
                ownerId,
                cancellationToken);

        if (result.Outcome ==
            ExpenseDraftOutcome.NotFound)
        {
            return Results.NotFound();
        }

        if (result.Outcome ==
            ExpenseDraftOutcome.Conflict)
        {
            return Results.Conflict();
        }

        if (result.Outcome ==
            ExpenseDraftOutcome.InvalidCategory)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["ExpenseCategoryId"] =
                    [
                        "The selected expense category does not exist."
                    ]
                });
        }

        if (result.Outcome ==
            ExpenseDraftOutcome.InvalidExpenseDate)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["ExpenseDate"] =
                    [
                        "The expense date cannot be in the future."
                    ]
                });
        }

        if (result.Outcome !=
                ExpenseDraftOutcome.Success ||
            result.Expense is null)
        {
            return Results.StatusCode(
                StatusCodes.Status500InternalServerError);
        }

        return Results.Ok(
            ToResponse(result.Expense));
    }

    private static Dictionary<string, string[]>
        ValidateRequest(object request)
    {
        List<ValidationResult> validationResults = [];

        bool isValid =
            Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                validationResults,
                validateAllProperties: true);

        if (isValid)
        {
            return [];
        }

        return validationResults
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
    }

    private static ExpenseResponse ToResponse(
        Expense expense) =>
        new()
        {
            Id = expense.Id,
            OwnerId = expense.OwnerId,
            Description = expense.Description,
            Amount = expense.Amount,
            ExpenseDate = expense.ExpenseDate,
            Status = expense.Status,
            ExpenseCategoryId =
                expense.ExpenseCategoryId
        };
}
