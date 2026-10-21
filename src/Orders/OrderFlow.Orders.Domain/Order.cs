using System.Security.Principal;

namespace OrderFlow.Orders.Domain;

public class Order
{
    private Order() { }

    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public List<OrderItem> Items { get; private set; } = [];

    public static Order Create(
        Guid id,
        string customerName,
        DateTime createdAtUtc)
    {
        return new Order
        {
            Id = id,
            CustomerName = customerName,
            Status = OrderStatus.Pending,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void AddItem(
        Guid itemId,
        string sku,
        int quantity,
        decimal unitPrice)
    {
        Items.Add(
            OrderItem.Create(itemId, Id, sku, quantity, unitPrice));
    }

    public void Confirm()
    {
        if (Status == OrderStatus.Pending)
            Status = OrderStatus.Confirmed;
        //Why check for Pending first?
        //Suppose the same StockReserved message is delivered twice.
        //RabbitMQ - based systems can redeliver messages, so consumers and domain logic should account for duplicates.
    }

    public void MarkFailed()
    {
        if (Status == OrderStatus.Pending)
            Status = OrderStatus.Failed;
    }
}