namespace OrderFlow.Orders.Application;

public record OrderSummaryDto(
    Guid Id,
    string CustomerName,
    string Status,
    DateTime CreatedAtUtc,
    int ItemCount,
    decimal Total
);