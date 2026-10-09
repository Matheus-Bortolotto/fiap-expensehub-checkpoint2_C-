using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

internal static class ExpenseCategorySeeder
{
    private static readonly string[] DefaultCategoryNames =
    [
        "Food",
        "Transportation",
        "Accommodation",
        "Other"
    ];

    internal static async Task SeedAsync(AppDbContext context)
    {
        List<string> existingNames =
            await context.ExpenseCategories
                .Select(category => category.Name)
                .ToListAsync();

        HashSet<string> existingNameSet =
            new(
                existingNames,
                System.StringComparer.OrdinalIgnoreCase);

        string[] missingCategoryNames =
            DefaultCategoryNames
                .Where(name => !existingNameSet.Contains(name))
                .ToArray();

        if (missingCategoryNames.Length == 0)
        {
            return;
        }

        ExpenseCategory[] missingCategories =
            missingCategoryNames
                .Select(
                    name =>
                        new ExpenseCategory
                        {
                            Name = name
                        })
                .ToArray();

        context.ExpenseCategories.AddRange(missingCategories);

        await context.SaveChangesAsync();
    }
}
