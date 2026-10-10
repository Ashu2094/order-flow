
namespace OrderFlow.Inventory.Application;

public record CreateInventoryItemRequest(
    string Sku,
    string Name,
    string? Description,
    int Quantity,
    decimal Price
);

public record UpdateInventoryItemRequest(
    string Name,
    string? Description,
    int Quantity,
    decimal Price
);
