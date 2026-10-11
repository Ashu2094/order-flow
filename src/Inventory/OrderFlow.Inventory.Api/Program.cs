
using OrderFlow.Inventory.Application;
using OrderFlow.Inventory.Domain;
using OrderFlow.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("InventoryDb")
    ?? throw new InvalidOperationException(
        "Connection string 'InventoryDb' is missing. Set it with dotnet user-secrets.");

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<InventoryDbContext>();

    try
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.CloseConnectionAsync();

        app.Logger.LogInformation(
            "Connected to the inventory database.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Cannot reach the inventory database.");

        throw;
    }
}


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


app.MapPost("/api/inventory", (CreateInventoryItemRequest request) =>
{
    var errors = InventoryValidator.ValidateCreate(request);

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var item = InventoryItem.Create(
        Guid.NewGuid(),
        request.Sku.Trim().ToUpperInvariant(),
        request.Name.Trim(),
        request.Quantity,
        request.Price,
        request.Description);

    items.Add(item);

    return Results.Created(
        $"/api/inventory/{item.Id}",
        InventoryItemDto.From(item));
});



app.MapPut("/api/inventory/{id:guid}",
    (Guid id, UpdateInventoryItemRequest request) =>
    {
        var errors = InventoryValidator.ValidateUpdate(request);

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var item = items.FirstOrDefault(i => i.Id == id);

        if (item is null)
            return Results.NotFound();

        item.Update(
            request.Name.Trim(),
            request.Description,
            request.Quantity,
            request.Price);

        return Results.Ok(InventoryItemDto.From(item));
    });


app.MapDelete("/api/inventory/{id:guid}",
    (Guid id) =>
    {
        var item = items.FirstOrDefault(i => i.Id == id);

        if (item is null)
            return Results.NotFound();

        items.Remove(item);

        return Results.NoContent();
    });

app.Run();
