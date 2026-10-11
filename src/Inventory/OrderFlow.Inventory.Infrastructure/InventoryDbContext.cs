using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Domain;

namespace OrderFlow.Inventory.Infrastructure;

public class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<InventoryItem> InventoryItems
        => Set<InventoryItem>();
    //A DbSet<T> represents a collection of entities of type T that EF Core can query and track.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(
            new InventoryItemConfiguration());
    }
}