
using OrderFlow.Inventory.Domain;

namespace OrderFlow.Inventory.Application;

public record InventoryItemDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    int Quantity,
    decimal Price
){
    public static InventoryItemDto From(InventoryItem item)
    {
        return new InventoryItemDto(
            item.Id,
            item.Sku,
            item.Name,
            item.Description,
            item.Quantity,
            item.Price);
    }
}
