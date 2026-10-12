using OrderFlow.Orders.Domain;

namespace OrderFlow.Orders.Application;

public record OrderItemDto(
    Guid Id,
    string Sku,
    int Quantity,
    decimal UnitPrice
);

public record OrderDto(
    Guid Id,
    string CustomerName,
    string Status,
    DateTime CreatedAtUtc,
    List<OrderItemDto> Items,
    decimal Total
)
{
    public static OrderDto From(Order order)
    {
        var items = order.Items
            .Select(i => new OrderItemDto(
                i.Id,
                i.Sku,
                i.Quantity,
                i.UnitPrice))
            .ToList();

        return new OrderDto(
            order.Id,
            order.CustomerName,
            order.Status.ToString(),
            order.CreatedAtUtc,
            items,
            order.Items.Sum(i => i.Quantity * i.UnitPrice));
    }
}