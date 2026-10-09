using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using ExpenseHub.Api.Domain.Entities;
using ExpenseHub.Api.Domain.Enums;
using ExpenseHub.Api.Expenses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests;

/// <summary>
/// Tests the read-visibility rules applied before expense queries are materialized.
/// </summary>
[TestClass]
public sealed class ExpenseAccessServiceTests
{
    /// <summary>
    /// Verifies that an employee can read only expenses they own.
    /// </summary>
    [TestMethod]
    public void ApplyVisibility_WhenUserIsEmployee_ReturnsOnlyOwnExpenses()
    {
        ClaimsPrincipal employee = CreateUser("employee-a", "Employee");
        IQueryable<Expense> expenses = CreateExpenses();

        List<int> visibleIds = ExpenseAccessService.ApplyVisibility(expenses, employee)
            .Select(expense => expense.Id)
            .ToList();

        CollectionAssert.AreEqual(new List<int> { 1 }, visibleIds);
    }

    /// <summary>
    /// Verifies that an approver can read submitted expenses but not drafts.
    /// </summary>
    [TestMethod]
    public void ApplyVisibility_WhenUserIsApprover_ReturnsOnlySubmittedExpenses()
    {
        ClaimsPrincipal approver = CreateUser("approver-a", "Approver");
        IQueryable<Expense> expenses = CreateExpenses();

        List<int> visibleIds = ExpenseAccessService.ApplyVisibility(expenses, approver)
            .Select(expense => expense.Id)
            .ToList();

        CollectionAssert.AreEqual(new List<int> { 2 }, visibleIds);
    }

    /// <summary>
    /// Verifies that Finance can read approved and paid expenses.
    /// </summary>
    [TestMethod]
    public void ApplyVisibility_WhenUserIsFinance_ReturnsApprovedAndPaidExpenses()
    {
        ClaimsPrincipal finance = CreateUser("finance-a", "Finance");
        IQueryable<Expense> expenses = CreateExpenses();

        List<int> visibleIds = ExpenseAccessService.ApplyVisibility(expenses, finance)
            .Select(expense => expense.Id)
            .ToList();

        CollectionAssert.AreEqual(new List<int> { 3, 4 }, visibleIds);
    }

    /// <summary>
    /// Verifies that the Auditor role can read every expense.
    /// </summary>
    [TestMethod]
    public void ApplyVisibility_WhenUserIsAuditor_ReturnsAllExpenses()
    {
        ClaimsPrincipal auditor = CreateUser("auditor-a", "Auditor");
        IQueryable<Expense> expenses = CreateExpenses();

        List<int> visibleIds = ExpenseAccessService.ApplyVisibility(expenses, auditor)
            .Select(expense => expense.Id)
            .ToList();

        CollectionAssert.AreEqual(new List<int> { 1, 2, 3, 4 }, visibleIds);
    }

    private static ClaimsPrincipal CreateUser(string id, string role)
    {
        ClaimsIdentity identity = new(
        [
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(ClaimTypes.Role, role)
        ], "test");

        return new ClaimsPrincipal(identity);
    }

    private static IQueryable<Expense> CreateExpenses() =>
        new List<Expense>
        {
            new() { Id = 1, OwnerId = "employee-a", Status = ExpenseStatus.Draft },
            new() { Id = 2, OwnerId = "employee-b", Status = ExpenseStatus.Submitted },
            new() { Id = 3, OwnerId = "employee-b", Status = ExpenseStatus.Approved },
            new() { Id = 4, OwnerId = "employee-b", Status = ExpenseStatus.Paid }
        }.AsQueryable();
}
