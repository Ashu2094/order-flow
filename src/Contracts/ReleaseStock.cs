namespace OrderFlow.Contracts;

public record ReleaseStock(
    Guid OrderId,
    IReadOnlyList<OrderSubmittedItem> Items);