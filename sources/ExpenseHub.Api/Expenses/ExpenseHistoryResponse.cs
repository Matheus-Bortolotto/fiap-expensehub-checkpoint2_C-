using System;
using ExpenseHub.Api.Domain.Enums;

namespace ExpenseHub.Api.Expenses;

/// <summary>
/// Represents an auditable expense history entry returned by the API.
/// </summary>
public sealed class ExpenseHistoryResponse
{
    /// <summary>
    /// Gets or sets the action performed.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the actor who performed the action.
    /// </summary>
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC instant at which the action occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the expense status before the action.
    /// </summary>
    public ExpenseStatus? PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the expense status after the action.
    /// </summary>
    public ExpenseStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the rejection justification when applicable.
    /// </summary>
    public string? Justification { get; set; }

    /// <summary>
    /// Gets or sets a description of draft changes when applicable.
    /// </summary>
    public string? Changes { get; set; }
}
