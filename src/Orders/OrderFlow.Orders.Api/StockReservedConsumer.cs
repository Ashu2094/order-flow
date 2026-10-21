
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts;
using OrderFlow.Orders.Infrastructure;

namespace OrderFlow.Orders.Api;

public class StockReservedConsumer(
    OrdersDbContext db,
    ILogger<StockReservedConsumer> logger)
    : IConsumer<StockReserved>
{
    public async Task Consume(
        ConsumeContext<StockReserved> context)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(
                o => o.Id == context.Message.OrderId,
                context.CancellationToken);

        if (order is null)
        {
            logger.LogWarning(
                "StockReserved arrived for unknown order {OrderId}",
                context.Message.OrderId);

            return;
        }

        order.Confirm();//If the order exists, calls Confirm().

        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Order {OrderId} is now {Status}",
            order.Id,
            order.Status);
    }
}
