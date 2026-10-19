
using System.Net;
using System.Net.Http.Json;
using OrderFlow.Orders.Application;

namespace OrderFlow.Orders.Infrastructure;

public class InventoryClient(HttpClient httpClient) : IInventoryClient
{
    public async Task<InventoryItemInfo?> GetBySkuAsync(string sku, CancellationToken ct)
    {
        var response = await httpClient.GetAsync(
            $"api/inventory/by-sku/{Uri.EscapeDataString(sku)}",
            ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();//Other unsuccessful status	EnsureSuccessStatusCode() throws an HTTP exception.

        return await response.Content
            .ReadFromJsonAsync<InventoryItemInfo>(ct);
    }
}
