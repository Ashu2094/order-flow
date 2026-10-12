namespace OrderFlow.Orders.Domain;

public class OrderItem
{
    private OrderItem() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string Sku { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public static OrderItem Create(
        Guid id,
        Guid orderId,
        string sku,
        int quantity,
        decimal unitPrice)
    {
        return new OrderItem
        {
            Id = id,
            OrderId = orderId,
            Sku = sku,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}