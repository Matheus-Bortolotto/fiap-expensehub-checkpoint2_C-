using System;
using ExpenseHub.Api.Domain.Enums;

namespace ExpenseHub.Api.Domain.Entities;

/// <summary>
/// Represents an auditable history entry for an expense.
/// </summary>
public class ExpenseHistory
{
    /// <summary>
    /// Gets or sets the history entry identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the related expense identifier.
    /// </summary>
    public int ExpenseId { get; set; }

    /// <summary>
    /// Gets or sets the related expense.
    /// </summary>
    public Expense Expense { get; set; } = null!;

    /// <summary>
    /// Gets or sets the action performed.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the actor who performed the action.
    /// </summary>
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC instant when the action occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the previous expense status.
    /// </summary>
    public ExpenseStatus? PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the resulting expense status.
    /// </summary>
    public ExpenseStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the rejection justification when applicable.
    /// </summary>
    public string? Justification { get; set; }

    /// <summary>
    /// Gets or sets a description of changes made while the expense was in Draft.
    /// </summary>
    public string? Changes { get; set; }
}
