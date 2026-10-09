using System;
using ExpenseHub.Api.Domain.Enums;

namespace ExpenseHub.Api.Expenses;

/// <summary>
/// Represents an expense returned by the API.
/// </summary>
public sealed class ExpenseResponse
{
    /// <summary>
    /// Gets or sets the expense identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the expense owner.
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
    /// Gets or sets the expense category identifier.
    /// </summary>
    public int ExpenseCategoryId { get; set; }
}
