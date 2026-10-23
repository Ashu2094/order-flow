
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts;
using OrderFlow.Orders.Infrastructure;

namespace OrderFlow.Orders.Api;

public class PaymentCompletedConsumer(
    OrdersDbContext db,
    ILogger<PaymentCompletedConsumer> logger)
    : IConsumer<PaymentCompleted>
{
    public async Task Consume(ConsumeContext<PaymentCompleted> context)
    {
        var order = await db.Orders.FirstOrDefaultAsync(
            o => o.Id == context.Message.OrderId,
            context.CancellationToken);

        if (order is null)
        {
            logger.LogWarning(
                "PaymentCompleted arrived for unknown order {OrderId}",
                context.Message.OrderId);
            return;
        }

        order.Confirm();

        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Payment completed; order {OrderId} is now {Status}",
            order.Id, order.Status);
    }
}
