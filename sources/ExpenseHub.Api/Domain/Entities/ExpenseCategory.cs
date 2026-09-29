using System.Collections.Generic;

namespace ExpenseHub.Api.Domain.Entities;

/// <summary>
/// Represents a category used to classify expenses.
/// </summary>
public class ExpenseCategory
{
    /// <summary>
    /// Gets or sets the category identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expenses associated with this category.
    /// </summary>
    public ICollection<Expense> Expenses { get; set; } = [];
}
