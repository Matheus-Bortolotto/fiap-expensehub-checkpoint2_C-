using System;
using ExpenseHub.Api.Domain.Enums;

namespace ExpenseHub.Api.Domain.Entities;

public class ExpenseHistory
{
    public int Id { get; set; }

    public int ExpenseId { get; set; }

    public Expense Expense { get; set; } = null!;

    public string Action { get; set; } = string.Empty;

    public string ActorId { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public ExpenseStatus? PreviousStatus { get; set; }

    public ExpenseStatus NewStatus { get; set; }

    public string? Justification { get; set; }

    public string? Changes { get; set; }
}
