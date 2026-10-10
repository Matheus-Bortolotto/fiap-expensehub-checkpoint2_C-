using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Domain.Entities;
using ExpenseHub.Api.Domain.Enums;
using ExpenseHub.Api.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests;

/// <summary>
/// Tests creation, editing and submission rules for expense drafts.
/// </summary>
[TestClass]
public sealed class ExpenseDraftServiceTests
{
    /// <summary>
    /// Verifies that creation derives the owner and records the initial Draft history.
    /// </summary>
    [TestMethod]
    public async Task CreateAsync_WhenRequestIsValid_CreatesOwnedDraftAndHistory()
    {
        await using AppDbContext context = CreateContext();
        AddCategory(context);
        ExpenseDraftService service = new(context);

        ExpenseDraftResult result = await service.CreateAsync(
            CreateRequest(),
            "employee-a",
            CancellationToken.None);

        Assert.AreEqual(ExpenseDraftOutcome.Success, result.Outcome);
        Expense expense = result.Expense!;
        Assert.AreEqual("employee-a", expense.OwnerId);
        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        ExpenseHistory history = context.ExpenseHistories.Single();
        Assert.AreEqual("Created", history.Action);
        Assert.IsNull(history.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Draft, history.NewStatus);
    }

    /// <summary>
    /// Verifies that a different employee cannot edit a Draft they do not own.
    /// </summary>
    [TestMethod]
    public async Task UpdateAsync_WhenExpenseBelongsToAnotherEmployee_ReturnsNotFound()
    {
        await using AppDbContext context = CreateContext();
        AddCategory(context);
        Expense expense = AddExpense(context, "employee-a", ExpenseStatus.Draft);
        ExpenseDraftService service = new(context);

        ExpenseDraftResult result = await service.UpdateAsync(
            expense.Id,
            CreateUpdateRequest(),
            "employee-b",
            CancellationToken.None);

        Assert.AreEqual(ExpenseDraftOutcome.NotFound, result.Outcome);
        Assert.AreEqual("Business travel expense", expense.Description);
        Assert.AreEqual(0, context.ExpenseHistories.Count());
    }

    /// <summary>
    /// Verifies that updates to a Draft are persisted with a matching history entry.
    /// </summary>
    [TestMethod]
    public async Task UpdateAsync_WhenOwnerEditsDraft_UpdatesAndRecordsChangedFields()
    {
        await using AppDbContext context = CreateContext();
        AddCategory(context);
        Expense expense = AddExpense(context, "employee-a", ExpenseStatus.Draft);
        ExpenseDraftService service = new(context);

        ExpenseDraftResult result = await service.UpdateAsync(
            expense.Id,
            CreateUpdateRequest(),
            "employee-a",
            CancellationToken.None);

        Assert.AreEqual(ExpenseDraftOutcome.Success, result.Outcome);
        Assert.AreEqual("Updated business travel expense", expense.Description);
        Assert.AreEqual(150m, expense.Amount);
        ExpenseHistory history = context.ExpenseHistories.Single();
        Assert.AreEqual("Updated", history.Action);
        StringAssert.Contains(history.Changes!, "Description");
        StringAssert.Contains(history.Changes!, "Amount");
    }

    /// <summary>
    /// Verifies that submission is allowed only once and creates no duplicate history.
    /// </summary>
    [TestMethod]
    public async Task SubmitAsync_WhenRepeated_ReturnsConflictWithoutDuplicateHistory()
    {
        await using AppDbContext context = CreateContext();
        AddCategory(context);
        Expense expense = AddExpense(context, "employee-a", ExpenseStatus.Draft);
        ExpenseDraftService service = new(context);

        ExpenseDraftResult first = await service.SubmitAsync(
            expense.Id,
            "employee-a",
            CancellationToken.None);
        ExpenseDraftResult repeated = await service.SubmitAsync(
            expense.Id,
            "employee-a",
            CancellationToken.None);

        Assert.AreEqual(ExpenseDraftOutcome.Success, first.Outcome);
        Assert.AreEqual(ExpenseDraftOutcome.Conflict, repeated.Outcome);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.AreEqual(1, context.ExpenseHistories.Count());
        Assert.AreEqual("Submitted", context.ExpenseHistories.Single().Action);
    }

    private static AppDbContext CreateContext()
    {
        DbContextOptions<AppDbContext> options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

        return new AppDbContext(options);
    }

    private static void AddCategory(AppDbContext context)
    {
        context.ExpenseCategories.Add(
            new ExpenseCategory
            {
                Id = 1,
                Name = "Transportation"
            });
        context.SaveChanges();
    }

    private static Expense AddExpense(
        AppDbContext context,
        string ownerId,
        ExpenseStatus status)
    {
        Expense expense = new()
        {
            OwnerId = ownerId,
            Description = "Business travel expense",
            Amount = 100m,
            ExpenseDate = DateTime.UtcNow.Date,
            ExpenseCategoryId = 1,
            Status = status
        };

        context.Expenses.Add(expense);
        context.SaveChanges();
        return expense;
    }

    private static CreateExpenseRequest CreateRequest() =>
        new()
        {
            Description = "New business travel expense",
            Amount = 125m,
            ExpenseDate = DateTime.UtcNow.Date,
            ExpenseCategoryId = 1
        };

    private static UpdateExpenseRequest CreateUpdateRequest() =>
        new()
        {
            Description = "Updated business travel expense",
            Amount = 150m,
            ExpenseDate = DateTime.UtcNow.Date,
            ExpenseCategoryId = 1
        };
}
