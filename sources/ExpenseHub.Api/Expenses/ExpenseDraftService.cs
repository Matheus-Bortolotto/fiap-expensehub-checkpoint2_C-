using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Domain.Entities;
using ExpenseHub.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Expenses;

/// <summary>
/// Handles the creation and editing of expense drafts.
/// </summary>
public sealed class ExpenseDraftService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ExpenseDraftService"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public ExpenseDraftService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new expense draft owned by the authenticated user.
    /// </summary>
    /// <param name="request">The validated expense data.</param>
    /// <param name="ownerId">The authenticated user's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ExpenseDraftResult> CreateAsync(
        CreateExpenseRequest request,
        string ownerId,
        CancellationToken cancellationToken)
    {
        if (!request.ExpenseDate.HasValue ||
            request.ExpenseDate.Value.Date > DateTime.UtcNow.Date)
        {
            return ExpenseDraftResult.InvalidExpenseDate();
        }

        bool categoryExists =
            await _context.ExpenseCategories.AnyAsync(
                category =>
                    category.Id == request.ExpenseCategoryId,
                cancellationToken);

        if (!categoryExists)
        {
            return ExpenseDraftResult.InvalidCategory();
        }

        Expense expense = new()
        {
            OwnerId = ownerId,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate.Value.Date,
            ExpenseCategoryId = request.ExpenseCategoryId,
            Status = ExpenseStatus.Draft
        };

        ExpenseWorkflowService.AddHistory(
            expense,
            ownerId,
            "Created",
            null,
            ExpenseStatus.Draft,
            DateTime.UtcNow);

        _context.Expenses.Add(expense);

        await _context.SaveChangesAsync(cancellationToken);

        return ExpenseDraftResult.Success(expense);
    }

    /// <summary>
    /// Updates an expense when it belongs to the authenticated user
    /// and is still in Draft status.
    /// </summary>
    /// <param name="expenseId">The expense identifier.</param>
    /// <param name="request">The validated updated expense data.</param>
    /// <param name="ownerId">The authenticated user's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ExpenseDraftResult> UpdateAsync(
        int expenseId,
        UpdateExpenseRequest request,
        string ownerId,
        CancellationToken cancellationToken)
    {
        Expense? expense =
            await _context.Expenses.SingleOrDefaultAsync(
                item =>
                    item.Id == expenseId &&
                    item.OwnerId == ownerId,
                cancellationToken);

        if (expense is null)
        {
            return ExpenseDraftResult.NotFound();
        }

        if (expense.Status != ExpenseStatus.Draft)
        {
            return ExpenseDraftResult.Conflict(expense);
        }

        if (!request.ExpenseDate.HasValue ||
            request.ExpenseDate.Value.Date > DateTime.UtcNow.Date)
        {
            return ExpenseDraftResult.InvalidExpenseDate();
        }

        bool categoryExists =
            await _context.ExpenseCategories.AnyAsync(
                category =>
                    category.Id == request.ExpenseCategoryId,
                cancellationToken);

        if (!categoryExists)
        {
            return ExpenseDraftResult.InvalidCategory();
        }

        string normalizedDescription =
            request.Description.Trim();

        DateTime normalizedExpenseDate =
            request.ExpenseDate.Value.Date;

        List<string> changes = [];

        if (!string.Equals(
                expense.Description,
                normalizedDescription,
                StringComparison.Ordinal))
        {
            expense.Description = normalizedDescription;
            changes.Add("Description");
        }

        if (expense.Amount != request.Amount)
        {
            expense.Amount = request.Amount;
            changes.Add("Amount");
        }

        if (expense.ExpenseDate.Date != normalizedExpenseDate)
        {
            expense.ExpenseDate = normalizedExpenseDate;
            changes.Add("ExpenseDate");
        }

        if (expense.ExpenseCategoryId !=
            request.ExpenseCategoryId)
        {
            expense.ExpenseCategoryId =
                request.ExpenseCategoryId;

            changes.Add("ExpenseCategoryId");
        }

        if (changes.Count == 0)
        {
            return ExpenseDraftResult.Success(expense);
        }

        ExpenseWorkflowService.AddHistory(
            expense,
            ownerId,
            "Updated",
            ExpenseStatus.Draft,
            ExpenseStatus.Draft,
            DateTime.UtcNow,
            changes: string.Join(", ", changes));

        await _context.SaveChangesAsync(cancellationToken);

        return ExpenseDraftResult.Success(expense);
    }
}

/// <summary>
/// Represents the result of a draft creation or update operation.
/// </summary>
public sealed class ExpenseDraftResult
{
    private ExpenseDraftResult(
        ExpenseDraftOutcome outcome,
        Expense? expense)
    {
        Outcome = outcome;
        Expense = expense;
    }

    /// <summary>
    /// Gets the outcome of the operation.
    /// </summary>
    public ExpenseDraftOutcome Outcome { get; }

    /// <summary>
    /// Gets the related expense when available.
    /// </summary>
    public Expense? Expense { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="expense">The affected expense.</param>
    /// <returns>A successful result.</returns>
    public static ExpenseDraftResult Success(Expense expense) =>
        new(
            ExpenseDraftOutcome.Success,
            expense);

    /// <summary>
    /// Creates a result for an expense that was not found
    /// in the user's ownership scope.
    /// </summary>
    /// <returns>A not found result.</returns>
    public static ExpenseDraftResult NotFound() =>
        new(
            ExpenseDraftOutcome.NotFound,
            null);

    /// <summary>
    /// Creates a result for an expense in an incompatible state.
    /// </summary>
    /// <param name="expense">The expense with an invalid state.</param>
    /// <returns>A conflict result.</returns>
    public static ExpenseDraftResult Conflict(Expense expense) =>
        new(
            ExpenseDraftOutcome.Conflict,
            expense);

    /// <summary>
    /// Creates a result for an unknown expense category.
    /// </summary>
    /// <returns>An invalid category result.</returns>
    public static ExpenseDraftResult InvalidCategory() =>
        new(
            ExpenseDraftOutcome.InvalidCategory,
            null);

    /// <summary>
    /// Creates a result for an invalid or future expense date.
    /// </summary>
    /// <returns>An invalid expense date result.</returns>
    public static ExpenseDraftResult InvalidExpenseDate() =>
        new(
            ExpenseDraftOutcome.InvalidExpenseDate,
            null);
}

/// <summary>
/// Represents the possible outcomes of a draft operation.
/// </summary>
public enum ExpenseDraftOutcome
{
    /// <summary>
    /// The operation succeeded.
    /// </summary>
    Success,

    /// <summary>
    /// The requested expense was not found in the user's scope.
    /// </summary>
    NotFound,

    /// <summary>
    /// The expense is no longer editable.
    /// </summary>
    Conflict,

    /// <summary>
    /// The supplied expense category does not exist.
    /// </summary>
    InvalidCategory,

    /// <summary>
    /// The supplied expense date is invalid or in the future.
    /// </summary>
    InvalidExpenseDate
}
