namespace OrderFlow.Contracts;

public record OrderSubmittedItem(string Sku, int Quantity);

public record OrderSubmitted(
    Guid OrderId,
    DateTime SubmittedAtUtc,
    List<OrderSubmittedItem> Items
);