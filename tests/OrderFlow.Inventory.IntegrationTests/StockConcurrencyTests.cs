using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Domain;
using OrderFlow.Inventory.Infrastructure;

namespace OrderFlow.Inventory.IntegrationTests;

[Trait("Category", "Concurrency")]
public class StockConcurrencyTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private InventoryDbContext NewContext() =>
        new(new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options);

    [Fact]
    public async Task Two_reservations_of_the_last_unit_only_one_wins()
    {
        var sku = "LAST-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        // Seed one item with quantity 1
        await using (var seed = NewContext())
        {
            seed.InventoryItems.Add(InventoryItem.Create(Guid.NewGuid(), sku, "Last unit", 1, 10m));
            await seed.SaveChangesAsync();
        }

        await using var first = NewContext();
        await using var second = NewContext();

        // Both read the same row while it still says quantity 1.
        var a = await first.InventoryItems.SingleAsync(i => i.Sku == sku);
        var b = await second.InventoryItems.SingleAsync(i => i.Sku == sku);

        // Both believe they got the last unit.
        Assert.True(a.TryReserve(1));
        Assert.True(b.TryReserve(1));

        // The first save wins; the second must be rejected.
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        // Read the real row from a fresh context.
        await using var check = NewContext();
        var final = await check.InventoryItems.SingleAsync(i => i.Sku == sku);
        Assert.Equal(0, final.Quantity);
    }
}