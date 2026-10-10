using System;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Expenses;

/// <summary>
/// Represents the data required to create an expense draft.
/// </summary>
public sealed class CreateExpenseRequest
{
    /// <summary>
    /// Gets or sets the expense description.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expense amount.
    /// </summary>
    [Range(typeof(decimal), "0.01", "2147483647.00")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the date when the expense occurred.
    /// </summary>
    [Required]
    public DateTime? ExpenseDate { get; set; }

    /// <summary>
    /// Gets or sets the expense category identifier.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ExpenseCategoryId { get; set; }
}
