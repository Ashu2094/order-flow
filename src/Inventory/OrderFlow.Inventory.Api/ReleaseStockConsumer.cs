
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts;
using OrderFlow.Inventory.Infrastructure;

namespace OrderFlow.Inventory.Api;

public class ReleaseStockConsumer(
    InventoryDbContext db,
    ILogger<ReleaseStockConsumer> logger)
    : IConsumer<ReleaseStock>
{
    public async Task Consume(ConsumeContext<ReleaseStock> context)
    {
        var skus = context.Message.Items
            .Select(i => i.Sku)
            .ToList();

        var stock = await db.InventoryItems
            .Where(i => skus.Contains(i.Sku))
            .ToListAsync(context.CancellationToken);

        foreach (var line in context.Message.Items)
        {
            stock.FirstOrDefault(i => i.Sku == line.Sku)
                ?.Release(line.Quantity);
        }

        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Stock released for order {OrderId}",
            context.Message.OrderId);
    }
}
