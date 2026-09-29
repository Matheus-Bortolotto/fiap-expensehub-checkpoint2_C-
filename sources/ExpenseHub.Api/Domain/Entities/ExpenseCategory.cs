using System.Collections.Generic;

namespace ExpenseHub.Api.Domain.Entities;

public class ExpenseCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Expense> Expenses { get; set; } = [];
}
