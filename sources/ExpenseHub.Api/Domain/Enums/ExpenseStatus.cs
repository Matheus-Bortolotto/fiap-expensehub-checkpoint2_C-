namespace ExpenseHub.Api.Domain.Enums;

/// <summary>
/// Represents the current state of an expense reimbursement request.
/// </summary>
public enum ExpenseStatus
{
    /// <summary>
    /// The expense is still being edited by its owner.
    /// </summary>
    Draft,

    /// <summary>
    /// The expense was submitted for approval.
    /// </summary>
    Submitted,

    /// <summary>
    /// The expense was approved.
    /// </summary>
    Approved,

    /// <summary>
    /// The expense was rejected.
    /// </summary>
    Rejected,

    /// <summary>
    /// The expense was paid.
    /// </summary>
    Paid
}
