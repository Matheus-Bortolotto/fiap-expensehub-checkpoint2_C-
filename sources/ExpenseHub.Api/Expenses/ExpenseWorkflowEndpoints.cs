using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Domain.Entities;
using ExpenseHub.Api.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Expenses;

/// <summary>
/// Maps API endpoints for approving, rejecting, paying and auditing expenses.
/// </summary>
public static class ExpenseWorkflowEndpoints
{
    /// <summary>
    /// Maps the expense workflow endpoints.
    /// </summary>
    /// <param name="app">The application route builder.</param>
    /// <returns>The supplied route builder.</returns>
    public static IEndpointRouteBuilder MapExpenseWorkflowEndpoints(
        this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder expenses = app.MapGroup("/api/expenses")
            .RequireAuthorization();

        expenses.MapPost("/{id:int}/approve", ApproveAsync);
        expenses.MapPost("/{id:int}/reject", RejectAsync);
        expenses.MapPost("/{id:int}/pay", PayAsync);
        expenses.MapGet("/{id:int}/history", GetHistoryAsync);

        return app;
    }

    private static async Task<IResult> ApproveAsync(
        int id,
        ClaimsPrincipal user,
        ExpenseWorkflowService workflowService,
        CancellationToken cancellationToken)
    {
        ExpenseWorkflowResult result = await workflowService.ApproveAsync(
            id,
            ExpenseAccessService.GetRequiredUserId(user),
            user.IsInRole(ApplicationRoles.Approver),
            cancellationToken);

        return ToHttpResult(result);
    }

    private static async Task<IResult> RejectAsync(
        int id,
        RejectExpenseRequest request,
        ClaimsPrincipal user,
        ExpenseWorkflowService workflowService,
        CancellationToken cancellationToken)
    {
        List<ValidationResult> validationResults = [];
        bool isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            true);

        if (!isValid)
        {
            return Results.ValidationProblem(
                validationResults.ToDictionary(
                    result => result.MemberNames.FirstOrDefault() ?? string.Empty,
                    result => new[] { result.ErrorMessage ?? "Invalid value." }));
        }

        ExpenseWorkflowResult result = await workflowService.RejectAsync(
            id,
            ExpenseAccessService.GetRequiredUserId(user),
            user.IsInRole(ApplicationRoles.Approver),
            request.Justification.Trim(),
            cancellationToken);

        return ToHttpResult(result);
    }

    private static async Task<IResult> PayAsync(
        int id,
        ClaimsPrincipal user,
        ExpenseWorkflowService workflowService,
        CancellationToken cancellationToken)
    {
        ExpenseWorkflowResult result = await workflowService.PayAsync(
            id,
            ExpenseAccessService.GetRequiredUserId(user),
            user.IsInRole(ApplicationRoles.Finance),
            cancellationToken);

        return ToHttpResult(result);
    }

    private static async Task<IResult> GetHistoryAsync(
        int id,
        ClaimsPrincipal user,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        Expense? expense = await ExpenseAccessService.ApplyVisibility(
                context.Expenses,
                user)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (expense is null)
        {
            return Results.NotFound();
        }

        List<ExpenseHistoryResponse> history = await context.ExpenseHistories
            .Where(item => item.ExpenseId == id)
            .OrderBy(item => item.OccurredAtUtc)
            .Select(item => new ExpenseHistoryResponse
            {
                Action = item.Action,
                ActorId = item.ActorId,
                OccurredAtUtc = item.OccurredAtUtc,
                PreviousStatus = item.PreviousStatus,
                NewStatus = item.NewStatus,
                Justification = item.Justification,
                Changes = item.Changes
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(history);
    }

    private static IResult ToHttpResult(ExpenseWorkflowResult result) =>
        result.Outcome switch
        {
            ExpenseWorkflowOutcome.Success => Results.NoContent(),
            ExpenseWorkflowOutcome.NotFound => Results.NotFound(),
            ExpenseWorkflowOutcome.Forbidden => Results.Forbid(),
            ExpenseWorkflowOutcome.Conflict => Results.Conflict(),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
}
