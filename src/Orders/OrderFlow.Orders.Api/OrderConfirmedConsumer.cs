using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts;
using OrderFlow.Orders.Infrastructure;

namespace OrderFlow.Orders.Api;

public class OrderConfirmedConsumer(OrdersDbContext db, ILogger<OrderConfirmedConsumer> logger)
    : IConsumer<OrderConfirmed>
{
    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var order = await db.Orders.FirstOrDefaultAsync(
            o => o.Id == context.Message.OrderId, context.CancellationToken);

        if (order is null)
        {
            logger.LogWarning("OrderConfirmed arrived for unknown order {OrderId}", context.Message.OrderId);
            return;
        }

        order.Confirm();
        await db.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Order {OrderId} is now {Status}", order.Id, order.Status);
    }
}