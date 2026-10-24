
using System.Net;
using System.Net.Http.Json;

namespace OrderFlow.Inventory.IntegrationTests;

public class InventoryApiTests(InventoryApiFixture fixture)
    : IClassFixture<InventoryApiFixture>
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    private record ItemResponse(
        Guid Id,
        string Sku,
        string Name,
        int Quantity,
        decimal Price);

    private static string NewSku() =>
        "IT-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task Create_then_get_returns_the_item_from_a_real_database()
    {
        var sku = NewSku();

        var create = await _client.PostAsJsonAsync(
            "/api/inventory",
            new
            {
                sku,
                name = "Integration Keyboard",
                quantity = 5,
                price = 12.5m
            });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(created);

        var get = await _client.GetAsync($"/api/inventory/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var fetched = await get.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(fetched);

        Assert.Equal(sku, fetched.Sku);
        Assert.Equal(5, fetched.Quantity);
    }

    [Fact]
    public async Task Creating_the_same_sku_twice_returns_409()
    {
        var sku = NewSku();

        var body = new
        {
            sku,
            name = "Duplicate",
            quantity = 1,
            price = 1m
        };

        var first = await _client.PostAsJsonAsync("/api/inventory", body);
        var second = await _client.PostAsJsonAsync("/api/inventory", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Getting_an_unknown_id_returns_404()
    {
        var response = await _client.GetAsync(
            $"/api/inventory/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
