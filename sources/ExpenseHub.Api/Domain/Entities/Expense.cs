using System;
using System.Collections.Generic;
using ExpenseHub.Api.Domain.Enums;

namespace ExpenseHub.Api.Domain.Entities;

public class Expense
{
    public int Id { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime ExpenseDate { get; set; }

    public ExpenseStatus Status { get; set; }

    public int ExpenseCategoryId { get; set; }

    public ExpenseCategory ExpenseCategory { get; set; } = null!;

    public ICollection<ExpenseHistory> History { get; set; } = [];

    public PaymentRecord? PaymentRecord { get; set; }
}
