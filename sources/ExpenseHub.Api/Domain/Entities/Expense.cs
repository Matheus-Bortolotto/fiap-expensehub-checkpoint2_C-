using System;
using System.Collections.Generic;
using ExpenseHub.Api.Domain.Enums;

namespace ExpenseHub.Api.Domain.Entities;

/// <summary>
/// Represents an expense reimbursement request.
/// </summary>
public class Expense
{
    /// <summary>
    /// Gets or sets the expense identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who owns the expense.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expense description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expense amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the date when the expense occurred.
    /// </summary>
    public DateTime ExpenseDate { get; set; }

    /// <summary>
    /// Gets or sets the current expense status.
    /// </summary>
    public ExpenseStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the category identifier.
    /// </summary>
    public int ExpenseCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category associated with the expense.
    /// </summary>
    public ExpenseCategory ExpenseCategory { get; set; } = null!;

    /// <summary>
    /// Gets or sets the expense history entries.
    /// </summary>
    public ICollection<ExpenseHistory> History { get; set; } = [];

    /// <summary>
    /// Gets or sets the payment record associated with the expense.
    /// </summary>
    public PaymentRecord? PaymentRecord { get; set; }
}
