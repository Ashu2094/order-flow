namespace OrderFlow.Orders.Application;

public record CreateOrderItemRequest(
    string Sku,
    int Quantity
 );

public record CreateOrderRequest(
    string CustomerName,
    List<CreateOrderItemRequest> Items);