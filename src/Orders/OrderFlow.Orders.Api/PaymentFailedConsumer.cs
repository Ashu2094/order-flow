
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts;
using OrderFlow.Orders.Infrastructure;

namespace OrderFlow.Orders.Api;

public class PaymentFailedConsumer(
    OrdersDbContext db,
    ISendEndpointProvider sendEndpointProvider,
    ILogger<PaymentFailedConsumer> logger)
    : IConsumer<PaymentFailed>
{
    public async Task Consume(ConsumeContext<PaymentFailed> context)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(
                o => o.Id == context.Message.OrderId,
                context.CancellationToken);

        if (order is null)
        {
            logger.LogWarning(
                "PaymentFailed arrived for unknown order {OrderId}",
                context.Message.OrderId);
            return;
        }

        if (!order.Cancel())
        {
            logger.LogInformation(
                "Order {OrderId} is already {Status}; nothing to cancel",
                order.Id, order.Status);
            return;
        }

        await db.SaveChangesAsync(context.CancellationToken);

        var items = order.Items
            .GroupBy(i => i.Sku)
            .Select(g => new OrderSubmittedItem(
                g.Key, g.Sum(i => i.Quantity)))
            .ToList();

        var endpoint = await sendEndpointProvider
            .GetSendEndpoint(new Uri("queue:ReleaseStock"));

        await endpoint.Send(
            new ReleaseStock(order.Id, items),
            context.CancellationToken);

        logger.LogWarning(
            "Payment failed; order {OrderId} is now {Status}; asked Inventory to release the stock. Reason: {Reason}",
            order.Id, order.Status, context.Message.Reason);
    }
}
