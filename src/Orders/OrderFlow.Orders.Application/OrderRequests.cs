namespace OrderFlow.Orders.Application;

public record CreateOrderItemRequest(
    string Sku,
    int Quantity,
    decimal UnitPrice);

public record CreateOrderRequest(
    string CustomerName,
    List<CreateOrderItemRequest> Items);