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
/// Tests protected expense workflow transitions and their audit entries.
/// </summary>
[TestClass]
public sealed class ExpenseWorkflowServiceTests
{
    /// <summary>
    /// Verifies that an approver can approve another user's submitted expense.
    /// </summary>
    [TestMethod]
    public async Task ApproveAsync_WhenSubmittedExpenseBelongsToAnotherUser_ApprovesAndAddsHistory()
    {
        await using AppDbContext context = CreateContext();
        Expense expense = AddExpense(context, "employee-a", ExpenseStatus.Submitted);
        ExpenseWorkflowService service = new(context);

        ExpenseWorkflowResult result = await service.ApproveAsync(
            expense.Id,
            "approver-a",
            true,
            CancellationToken.None);

        Assert.AreEqual(ExpenseWorkflowOutcome.Success, result.Outcome);
        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
        ExpenseHistory history = context.ExpenseHistories.Single();
        Assert.AreEqual("Approved", history.Action);
        Assert.AreEqual("approver-a", history.ActorId);
        Assert.AreEqual(ExpenseStatus.Submitted, history.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Approved, history.NewStatus);
    }

    /// <summary>
    /// Verifies that an approver cannot approve their own expense.
    /// </summary>
    [TestMethod]
    public async Task ApproveAsync_WhenApproverOwnsExpense_ReturnsForbiddenWithoutHistory()
    {
        await using AppDbContext context = CreateContext();
        Expense expense = AddExpense(context, "approver-a", ExpenseStatus.Submitted);
        ExpenseWorkflowService service = new(context);

        ExpenseWorkflowResult result = await service.ApproveAsync(
            expense.Id,
            "approver-a",
            true,
            CancellationToken.None);

        Assert.AreEqual(ExpenseWorkflowOutcome.Forbidden, result.Outcome);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.AreEqual(0, context.ExpenseHistories.Count());
    }

    /// <summary>
    /// Verifies that the service still rejects an actor without the required role.
    /// </summary>
    [TestMethod]
    public async Task ApproveAsync_WhenActorLacksApproverRole_ReturnsForbidden()
    {
        await using AppDbContext context = CreateContext();
        Expense expense = AddExpense(context, "employee-a", ExpenseStatus.Submitted);
        ExpenseWorkflowService service = new(context);

        ExpenseWorkflowResult result = await service.ApproveAsync(
            expense.Id,
            "employee-b",
            false,
            CancellationToken.None);

        Assert.AreEqual(ExpenseWorkflowOutcome.Forbidden, result.Outcome);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.AreEqual(0, context.ExpenseHistories.Count());
    }

    /// <summary>
    /// Verifies that whitespace-only rejection justifications are rejected.
    /// </summary>
    [TestMethod]
    public async Task RejectAsync_WhenJustificationIsWhitespace_ReturnsInvalidJustification()
    {
        await using AppDbContext context = CreateContext();
        Expense expense = AddExpense(context, "employee-a", ExpenseStatus.Submitted);
        ExpenseWorkflowService service = new(context);

        ExpenseWorkflowResult result = await service.RejectAsync(
            expense.Id,
            "approver-a",
            true,
            "          ",
            CancellationToken.None);

        Assert.AreEqual(ExpenseWorkflowOutcome.InvalidJustification, result.Outcome);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.AreEqual(0, context.ExpenseHistories.Count());
    }

    /// <summary>
    /// Verifies that a rejection stores its normalized justification only once.
    /// </summary>
    [TestMethod]
    public async Task RejectAsync_WhenRepeated_ReturnsConflictWithoutDuplicatingHistory()
    {
        await using AppDbContext context = CreateContext();
        Expense expense = AddExpense(context, "employee-a", ExpenseStatus.Submitted);
        ExpenseWorkflowService service = new(context);

        ExpenseWorkflowResult first = await service.RejectAsync(
            expense.Id,
            "approver-a",
            true,
            "  Missing receipt attachment.  ",
            CancellationToken.None);
        ExpenseWorkflowResult repeated = await service.RejectAsync(
            expense.Id,
            "approver-a",
            true,
            "Missing receipt attachment.",
            CancellationToken.None);

        Assert.AreEqual(ExpenseWorkflowOutcome.Success, first.Outcome);
        Assert.AreEqual(ExpenseWorkflowOutcome.Conflict, repeated.Outcome);
        Assert.AreEqual(ExpenseStatus.Rejected, expense.Status);
        Assert.AreEqual(1, context.ExpenseHistories.Count());
        Assert.AreEqual(
            "Missing receipt attachment.",
            context.ExpenseHistories.Single().Justification);
    }

    /// <summary>
    /// Verifies that Finance cannot pay an expense it owns.
    /// </summary>
    [TestMethod]
    public async Task PayAsync_WhenFinanceOwnsExpense_ReturnsForbiddenWithoutPayment()
    {
        await using AppDbContext context = CreateContext();
        Expense expense = AddExpense(context, "finance-a", ExpenseStatus.Approved);
        ExpenseWorkflowService service = new(context);

        ExpenseWorkflowResult result = await service.PayAsync(
            expense.Id,
            "finance-a",
            true,
            CancellationToken.None);

        Assert.AreEqual(ExpenseWorkflowOutcome.Forbidden, result.Outcome);
        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
        Assert.AreEqual(0, context.PaymentRecords.Count());
        Assert.AreEqual(0, context.ExpenseHistories.Count());
    }

    /// <summary>
    /// Verifies that Finance records payment details and the matching audit entry.
    /// </summary>
    [TestMethod]
    public async Task PayAsync_WhenApprovedExpenseBelongsToAnotherUser_PersistsPaymentAndHistory()
    {
        await using AppDbContext context = CreateContext();
        Expense expense = AddExpense(context, "employee-a", ExpenseStatus.Approved);
        ExpenseWorkflowService service = new(context);

        ExpenseWorkflowResult result = await service.PayAsync(
            expense.Id,
            "finance-a",
            true,
            CancellationToken.None);

        Assert.AreEqual(ExpenseWorkflowOutcome.Success, result.Outcome);
        Assert.AreEqual(ExpenseStatus.Paid, expense.Status);
        PaymentRecord payment = context.PaymentRecords.Single();
        Assert.AreEqual("finance-a", payment.ActorId);
        Assert.AreEqual(DateTimeKind.Utc, payment.PaidAtUtc.Kind);
        Assert.AreEqual("Paid", context.ExpenseHistories.Single().Action);
    }

    private static AppDbContext CreateContext()
    {
        DbContextOptions<AppDbContext> options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

        return new AppDbContext(options);
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
}
