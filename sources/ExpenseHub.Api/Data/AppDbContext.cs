using ExpenseHub.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    public DbSet<ExpenseHistory> ExpenseHistories => Set<ExpenseHistory>();

    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.Property(expense => expense.OwnerId)
                .IsRequired();

            entity.Property(expense => expense.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(expense => expense.Amount)
                .HasPrecision(18, 2);

            entity.HasOne(expense => expense.ExpenseCategory)
                .WithMany(category => category.Expenses)
                .HasForeignKey(expense => expense.ExpenseCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(expense => expense.PaymentRecord)
                .WithOne(payment => payment.Expense)
                .HasForeignKey<PaymentRecord>(payment => payment.ExpenseId);
        });

        modelBuilder.Entity<ExpenseCategory>(entity =>
        {
            entity.Property(category => category.Name)
                .IsRequired()
                .HasMaxLength(100);
        });

        modelBuilder.Entity<ExpenseHistory>(entity =>
        {
            entity.Property(history => history.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(history => history.ActorId)
                .IsRequired();

            entity.Property(history => history.Justification)
                .HasMaxLength(500);

            entity.HasOne(history => history.Expense)
                .WithMany(expense => expense.History)
                .HasForeignKey(history => history.ExpenseId);
        });

        modelBuilder.Entity<PaymentRecord>(entity =>
        {
            entity.Property(payment => payment.ActorId)
                .IsRequired();
        });
    }
}
