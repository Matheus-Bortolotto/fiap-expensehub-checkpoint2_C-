using System;

namespace ExpenseHub.Api.Domain.Entities;

/// <summary>
/// Represents the payment record of an approved expense.
/// </summary>
public class PaymentRecord
{
    /// <summary>
    /// Gets or sets the payment record identifier.
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
    /// Gets or sets the identifier of the finance user who registered the payment.
    /// </summary>
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC instant when the payment was registered.
    /// </summary>
    public DateTime PaidAtUtc { get; set; }
}
