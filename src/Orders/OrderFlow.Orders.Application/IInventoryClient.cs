
namespace OrderFlow.Orders.Application;

public interface IInventoryClient
{
    Task<InventoryItemInfo?> GetBySkuAsync(string sku, CancellationToken ct);
}

public record InventoryItemInfo(
    string Sku,
    string Name,
    decimal Price
);
