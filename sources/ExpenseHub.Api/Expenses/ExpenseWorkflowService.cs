using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Domain.Entities;
using ExpenseHub.Api.Domain.Enums;
using ExpenseHub.Api.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Expenses;

/// <summary>
/// Executes protected expense workflow transitions and records their audit trail.
/// </summary>
public sealed class ExpenseWorkflowService(AppDbContext context)
{
    private readonly AppDbContext _context = context;

    /// <summary>
    /// Approves a submitted expense when the actor has the Approver role and is not its owner.
    /// </summary>
    /// <param name="expenseId">The expense identifier.</param>
    /// <param name="actorId">The identifier of the actor from the bearer token.</param>
    /// <param name="isApprover">Whether the actor holds the Approver role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow result.</returns>
    public Task<ExpenseWorkflowResult> ApproveAsync(
        int expenseId,
        string actorId,
        bool isApprover,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            expenseId,
            actorId,
            isApprover,
            ApplicationRoles.Approver,
            ExpenseStatus.Submitted,
            ExpenseStatus.Approved,
            "Approved",
            null,
            cancellationToken);

    /// <summary>
    /// Rejects a submitted expense when the actor has the Approver role and is not its owner.
    /// </summary>
    /// <param name="expenseId">The expense identifier.</param>
    /// <param name="actorId">The identifier of the actor from the bearer token.</param>
    /// <param name="isApprover">Whether the actor holds the Approver role.</param>
    /// <param name="justification">Validated rejection justification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow result.</returns>
    public Task<ExpenseWorkflowResult> RejectAsync(
        int expenseId,
        string actorId,
        bool isApprover,
        string justification,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            expenseId,
            actorId,
            isApprover,
            ApplicationRoles.Approver,
            ExpenseStatus.Submitted,
            ExpenseStatus.Rejected,
            "Rejected",
            justification,
            cancellationToken);

    /// <summary>
    /// Pays an approved expense when the actor has the Finance role and is not its owner.
    /// </summary>
    /// <param name="expenseId">The expense identifier.</param>
    /// <param name="actorId">The identifier of the actor from the bearer token.</param>
    /// <param name="isFinance">Whether the actor holds the Finance role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow result.</returns>
    public async Task<ExpenseWorkflowResult> PayAsync(
        int expenseId,
        string actorId,
        bool isFinance,
        CancellationToken cancellationToken)
    {
        ExpenseWorkflowResult precondition = await ValidateTransitionAsync(
            expenseId,
            actorId,
            isFinance,
            ApplicationRoles.Finance,
            ExpenseStatus.Approved,
            cancellationToken);

        if (precondition.Outcome != ExpenseWorkflowOutcome.Success)
        {
            return precondition;
        }

        Expense expense = precondition.Expense!;
        DateTime now = DateTime.UtcNow;
        expense.Status = ExpenseStatus.Paid;
        expense.PaymentRecord = new PaymentRecord
        {
            ActorId = actorId,
            PaidAtUtc = now
        };
        AddHistory(expense, actorId, "Paid", ExpenseStatus.Approved, ExpenseStatus.Paid, now);
        await _context.SaveChangesAsync(cancellationToken);
        return ExpenseWorkflowResult.Success(expense);
    }

    /// <summary>
    /// Adds a history entry to an expense. This is used by the draft and submit features during integration.
    /// </summary>
    /// <param name="expense">The expense whose history will be updated.</param>
    /// <param name="actorId">The user who performed the action.</param>
    /// <param name="action">A concise action name.</param>
    /// <param name="previousStatus">The status before the action.</param>
    /// <param name="newStatus">The status after the action.</param>
    /// <param name="occurredAtUtc">The server UTC instant of the action.</param>
    /// <param name="justification">Optional rejection justification.</param>
    /// <param name="changes">Optional description of draft changes.</param>
    public static void AddHistory(
        Expense expense,
        string actorId,
        string action,
        ExpenseStatus? previousStatus,
        ExpenseStatus newStatus,
        DateTime occurredAtUtc,
        string? justification = null,
        string? changes = null)
    {
        ArgumentNullException.ThrowIfNull(expense);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        expense.History.Add(new ExpenseHistory
        {
            Action = action,
            ActorId = actorId,
            OccurredAtUtc = occurredAtUtc,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Justification = justification,
            Changes = changes
        });
    }

    private async Task<ExpenseWorkflowResult> TransitionAsync(
        int expenseId,
        string actorId,
        bool hasRequiredRole,
        string requiredRole,
        ExpenseStatus expectedStatus,
        ExpenseStatus resultingStatus,
        string action,
        string? justification,
        CancellationToken cancellationToken)
    {
        ExpenseWorkflowResult precondition = await ValidateTransitionAsync(
            expenseId,
            actorId,
            hasRequiredRole,
            requiredRole,
            expectedStatus,
            cancellationToken);

        if (precondition.Outcome != ExpenseWorkflowOutcome.Success)
        {
            return precondition;
        }

        Expense expense = precondition.Expense!;
        DateTime now = DateTime.UtcNow;
        expense.Status = resultingStatus;
        AddHistory(expense, actorId, action, expectedStatus, resultingStatus, now, justification);
        await _context.SaveChangesAsync(cancellationToken);
        return ExpenseWorkflowResult.Success(expense);
    }

    private async Task<ExpenseWorkflowResult> ValidateTransitionAsync(
        int expenseId,
        string actorId,
        bool hasRequiredRole,
        string requiredRole,
        ExpenseStatus expectedStatus,
        CancellationToken cancellationToken)
    {
        if (!hasRequiredRole)
        {
            return ExpenseWorkflowResult.Forbidden(
                $"The {requiredRole} role is required for this operation.");
        }

        Expense? expense = await _context.Expenses
            .Include(item => item.History)
            .Include(item => item.PaymentRecord)
            .SingleOrDefaultAsync(item => item.Id == expenseId, cancellationToken);

        if (expense is null)
        {
            return ExpenseWorkflowResult.NotFound();
        }

        if (expense.OwnerId == actorId)
        {
            return ExpenseWorkflowResult.Forbidden(
                "A user cannot execute this operation on their own expense.");
        }

        if (expense.Status != expectedStatus)
        {
            return ExpenseWorkflowResult.Conflict(expense);
        }

        return ExpenseWorkflowResult.Success(expense);
    }
}

/// <summary>
/// Represents the result of a contextual workflow operation.
/// </summary>
public sealed class ExpenseWorkflowResult
{
    private ExpenseWorkflowResult(
        ExpenseWorkflowOutcome outcome,
        Expense? expense,
        string? detail)
    {
        Outcome = outcome;
        Expense = expense;
        Detail = detail;
    }

    /// <summary>
    /// Gets the outcome of the operation.
    /// </summary>
    public ExpenseWorkflowOutcome Outcome { get; }

    /// <summary>
    /// Gets the related expense when it was found.
    /// </summary>
    public Expense? Expense { get; }

    /// <summary>
    /// Gets a safe error description when applicable.
    /// </summary>
    public string? Detail { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="expense">The affected expense.</param>
    /// <returns>A successful result.</returns>
    public static ExpenseWorkflowResult Success(Expense expense) =>
        new(ExpenseWorkflowOutcome.Success, expense, null);

    /// <summary>
    /// Creates a forbidden result.
    /// </summary>
    /// <param name="detail">The explanation of the denial.</param>
    /// <returns>A forbidden result.</returns>
    public static ExpenseWorkflowResult Forbidden(string detail) =>
        new(ExpenseWorkflowOutcome.Forbidden, null, detail);

    /// <summary>
    /// Creates a not found result.
    /// </summary>
    /// <returns>A not found result.</returns>
    public static ExpenseWorkflowResult NotFound() =>
        new(ExpenseWorkflowOutcome.NotFound, null, null);

    /// <summary>
    /// Creates a conflict result.
    /// </summary>
    /// <param name="expense">The expense with an incompatible status.</param>
    /// <returns>A conflict result.</returns>
    public static ExpenseWorkflowResult Conflict(Expense expense) =>
        new(ExpenseWorkflowOutcome.Conflict, expense, null);
}

/// <summary>
/// Represents the possible outcomes of a workflow operation.
/// </summary>
public enum ExpenseWorkflowOutcome
{
    /// <summary>
    /// The operation succeeded.
    /// </summary>
    Success,

    /// <summary>
    /// The expense does not exist.
    /// </summary>
    NotFound,

    /// <summary>
    /// The actor is authenticated but cannot perform the operation.
    /// </summary>
    Forbidden,

    /// <summary>
    /// The expense is in an incompatible status.
    /// </summary>
    Conflict
}
