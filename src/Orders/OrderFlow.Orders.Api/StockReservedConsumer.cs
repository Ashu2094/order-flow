
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts;
using OrderFlow.Orders.Infrastructure;

namespace OrderFlow.Orders.Api;

public class StockReservedConsumer(
    OrdersDbContext db,
    ISendEndpointProvider sendEndpointProvider,
    ILogger<StockReservedConsumer> logger)
    : IConsumer<StockReserved>
{
    public async Task Consume(ConsumeContext<StockReserved> context)
    {
        var order = await db.Orders
            .Include(o => o.Items)
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

        var amount = order.Items.Sum(
            i => i.Quantity * i.UnitPrice);

        var endpoint = await sendEndpointProvider
            .GetSendEndpoint(new Uri("queue:ChargePayment"));

        await endpoint.Send(
            new ChargePayment(order.Id, amount),
            context.CancellationToken);

        logger.LogInformation(
            "Stock reserved for order {OrderId}; asked Payments to charge {Amount}",
            order.Id, amount);
    }
}
