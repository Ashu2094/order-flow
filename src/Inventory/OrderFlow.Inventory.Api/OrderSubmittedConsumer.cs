
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts;
using OrderFlow.Inventory.Infrastructure;

namespace OrderFlow.Inventory.Api;

public class OrderSubmittedConsumer(InventoryDbContext db, ILogger<OrderSubmittedConsumer> logger) : IConsumer<OrderSubmitted>
{
    //OrderSubmittedConsumer implements IConsumer class which has message of type OrderSubmitted.
    //It has a constructor that takes an ILogger < OrderSubmittedConsumer > parameter and assigns it to a private readonly field called logger.
    //The Consume method is implemented to handle the OrderSubmitted message.
    //It logs the order details, including the order ID, submission time, item count, and a formatted string of the items in the order.
    //The method returns a completed task after logging the information.
    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var message = context.Message;


        var wanted = message.Items
            .GroupBy(i => i.Sku)
            .Select(g => new
            {
                Sku = g.Key,
                Quantity = g.Sum(i => i.Quantity)
            })
            .ToList();

        var skus = wanted.Select(w => w.Sku).ToList();

        var stock = await db.InventoryItems
            .Where(i => skus.Contains(i.Sku))
            .ToListAsync(context.CancellationToken);

        foreach (var line in wanted)
        {
            var item = stock.FirstOrDefault(
                i => i.Sku == line.Sku);

            if (item is null)
            {
                await Reject(
                    context,
                    $"SKU '{line.Sku}' does not exist.");

                return;
            }

            if (item.Quantity < line.Quantity)
            {
                await Reject(
                    context,
                    $"Not enough stock for '{line.Sku}': " +
                    $"requested {line.Quantity}, " +
                    $"available {item.Quantity}.");

                return;
            }
        }

        foreach (var line in wanted)
        {
            stock.First(i => i.Sku == line.Sku)
                .TryReserve(line.Quantity);
        }

        await db.SaveChangesAsync(context.CancellationToken);

        await context.Publish(
            new StockReserved(message.OrderId),
            context.CancellationToken);

        //if (message.Items.Any(i => i.Quantity == 13))
        //{
        //    throw new InvalidOperationException(
        //        "Simulated failure after save and publish (quantity 13).");
        //}

        logger.LogInformation(
            "Stock reserved for order {OrderId}",
            message.OrderId);
    }

    private async Task Reject(ConsumeContext<OrderSubmitted> context, string reason)
    {
        logger.LogWarning(
            "Stock reservation failed for order {OrderId}: {Reason}",
            context.Message.OrderId,
            reason);

        await context.Publish(
            new StockReservationFailed
            (
                context.Message.OrderId, reason
            ), context.CancellationToken);
    }
}
