using System;

namespace ExpenseHub.Api.Domain.Entities;

public class PaymentRecord
{
    public int Id { get; set; }

    public int ExpenseId { get; set; }

    public Expense Expense { get; set; } = null!;

    public string ActorId { get; set; } = string.Empty;

    public DateTime PaidAtUtc { get; set; }
}
