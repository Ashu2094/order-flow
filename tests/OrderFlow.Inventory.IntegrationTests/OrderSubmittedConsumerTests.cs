using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Contracts;
using OrderFlow.Inventory.Api;
using OrderFlow.Inventory.Domain;
using OrderFlow.Inventory.Infrastructure;

namespace OrderFlow.Inventory.IntegrationTests;

public class OrderSubmittedConsumerTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private ServiceProvider BuildProvider() => new ServiceCollection()
                                                    .AddLogging()
                                                    .AddDbContext<InventoryDbContext>(options => options.UseNpgsql(database.ConnectionString))
                                                    .AddMassTransitTestHarness(x => x.AddConsumer<OrderSubmittedConsumer>())
                                                    .BuildServiceProvider(true);

    private static string NewSku() => "H-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static async Task SeedAsync(ServiceProvider provider, string sku, int quantity)
    {
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        db.InventoryItems.Add(
            InventoryItem.Create(
                Guid.NewGuid(),
                sku,
                "Harness item",
                quantity,
                10m,
                "harness test"));

        await db.SaveChangesAsync();
    }

    private static async Task<int> StockAsync(
        ServiceProvider provider,
        string sku)
    {
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<InventoryDbContext>();

        return await db.InventoryItems
            .Where(i => i.Sku == sku)
            .Select(i => i.Quantity)
            .SingleAsync();
    }

    [Fact]
    public async Task Publishes_StockReserved_when_stock_exists()
    {
        await using var provider = BuildProvider();

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var sku = NewSku();
        await SeedAsync(provider, sku, 10);

        var orderId = Guid.NewGuid();

        await harness.Bus.Publish(
            new OrderSubmitted(
                orderId,
                DateTime.UtcNow,
                [new OrderSubmittedItem(sku, 2)]));

        Assert.True(await harness.Consumed.Any<OrderSubmitted>());

        Assert.True(
            await harness.Published.Any<StockReserved>(
                m => m.Context.Message.OrderId == orderId));

        Assert.False(
            await harness.Published.Any<StockReservationFailed>());

        Assert.Equal(8, await StockAsync(provider, sku));
    }

    [Fact]
    public async Task Publishes_StockReservationFailed_when_stock_is_too_low()
    {
        await using var provider = BuildProvider();

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var sku = NewSku();
        await SeedAsync(provider, sku, 1);

        var orderId = Guid.NewGuid();

        await harness.Bus.Publish(
            new OrderSubmitted(
                orderId,
                DateTime.UtcNow,
                [new OrderSubmittedItem(sku, 5)]));

        Assert.True(
            await harness.Published.Any<StockReservationFailed>(
                m => m.Context.Message.OrderId == orderId));

        Assert.False(
            await harness.Published.Any<StockReserved>());

        Assert.Equal(1, await StockAsync(provider, sku));
    }
}