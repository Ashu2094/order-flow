using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts;
using OrderFlow.Orders.Domain;
using OrderFlow.Orders.Infrastructure;

namespace OrderFlow.Orders.Api;

public class OrderStatusConsumer(OrdersDbContext db, ILogger<OrderStatusConsumer> logger)
    : IConsumer<OrderConfirmed>, IConsumer<OrderCancelled>, IConsumer<OrderFailed>
{
    public Task Consume(ConsumeContext<OrderConfirmed> context) =>
        Apply(context.Message.OrderId, order => order.Confirm(), context.CancellationToken);

    public Task Consume(ConsumeContext<OrderCancelled> context) =>
        Apply(context.Message.OrderId, order => order.Cancel(), context.CancellationToken);

    public Task Consume(ConsumeContext<OrderFailed> context) =>
        Apply(context.Message.OrderId, order => order.MarkFailed(), context.CancellationToken);

    private async Task Apply(Guid orderId, Action<Order> change, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            logger.LogWarning("Status event arrived for unknown order {OrderId}", orderId);
            return;
        }

        change(order);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Order {OrderId} is now {Status}", order.Id, order.Status);
    }
}