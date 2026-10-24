using Microsoft.EntityFrameworkCore;
using OrderFlow.Payments.Domain;

namespace OrderFlow.Payments.Infrastructure;

public class PaymentsDbContext(
    DbContextOptions<PaymentsDbContext> options)
    : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(
            new PaymentConfiguration());

        modelBuilder.ApplyConfiguration(
    new ProcessedMessageConfiguration());
    }
}