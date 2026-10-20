using MassTransit;
using OrderFlow.Contracts;

namespace OrderFlow.Inventory.Api;

public class OrderSubmittedConsumer(ILogger<OrderSubmittedConsumer> logger): IConsumer<OrderSubmitted>
//OrderSubmittedConsumer implements IConsumer class which has message of type OrderSubmitted.
//It has a constructor that takes an ILogger < OrderSubmittedConsumer > parameter and assigns it to a private readonly field called logger.
//The Consume method is implemented to handle the OrderSubmitted message.
//It logs the order details, including the order ID, submission time, item count, and a formatted string of the items in the order.
//The method returns a completed task after logging the information.
{
    public Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var message = context.Message;

        var items = string.Join(
            ", ",
            message.Items.Select(i => $"{i.Quantity} x {i.Sku}"));

        logger.LogInformation(
            "Order {OrderId} submitted at {SubmittedAtUtc} with {ItemCount} item(s): {Items}",
            message.OrderId,
            message.SubmittedAtUtc,
            message.Items.Count,
            items);

        return Task.CompletedTask;
    }
}