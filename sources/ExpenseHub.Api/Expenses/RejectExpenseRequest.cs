using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Expenses;

/// <summary>
/// Represents the reason supplied when an expense is rejected.
/// </summary>
public sealed class RejectExpenseRequest
{
    /// <summary>
    /// Gets or sets the reason for rejecting the expense.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Justification { get; set; } = string.Empty;
}
