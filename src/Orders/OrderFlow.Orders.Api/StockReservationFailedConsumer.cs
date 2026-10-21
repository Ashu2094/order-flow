
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts;
using OrderFlow.Orders.Infrastructure;

namespace OrderFlow.Orders.Api;

public class StockReservationFailedConsumer(
    OrdersDbContext db,
    ILogger<StockReservationFailedConsumer> logger)
    : IConsumer<StockReservationFailed>
{
    public async Task Consume(
        ConsumeContext<StockReservationFailed> context)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(
                o => o.Id == context.Message.OrderId,
                context.CancellationToken);

        if (order is null)
        {
            logger.LogWarning(
                "StockReservationFailed arrived for unknown order {OrderId}",
                context.Message.OrderId);

            return;
        }

        order.MarkFailed();

        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogWarning(
            "Order {OrderId} is now {Status}. Reason: {Reason}",
            order.Id,
            order.Status,
            context.Message.Reason);
    }
}
