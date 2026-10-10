
using OrderFlow.Inventory.Application;
using OrderFlow.Inventory.Domain;
using OrderFlow.Inventory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

var items = new List<InventoryItem>
{
    InventoryItem.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"),"KB-001", "Keyboard", 25, 49.99m),

    InventoryItem.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"),"MS-002", "Mouse", 40, 19.50m, "Wireless"),

    InventoryItem.Create(Guid.Parse("33333333-3333-3333-3333-333333333333"),"MN-003", "Monitor", 8, 189.00m)
};

app.MapGet("/ping", () =>
    Results.Ok(new { service = "inventory", status = "up" }));

app.MapGet("/api/inventory", () =>
    Results.Ok(items.Select(InventoryItemDto.From).ToList()));

app.MapGet("/api/inventory/{id:guid}", (Guid id) =>
{
    var item = items.FirstOrDefault(i => i.Id == id);

    return item is null
        ? Results.NotFound()
        : Results.Ok(InventoryItemDto.From(item));
});

app.Run();
