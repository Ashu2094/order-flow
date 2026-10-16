
using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Application;
using OrderFlow.Inventory.Domain;
using OrderFlow.Inventory.Infrastructure;
using OrderFlow.Inventory.Api;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("InventoryDb")
    ?? throw new InvalidOperationException(
        "Connection string 'InventoryDb' is missing. Set it with dotnet user-secrets.");

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<InventoryDbContext>("database");

var app = builder.Build();

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();

    await next();

    stopwatch.Stop();

    var status = context.Response.StatusCode;

    var level = status >= 500
        ? LogLevel.Error
        : status >= 400
            ? LogLevel.Warning
            : LogLevel.Information;

    app.Logger.Log(
        level,
        "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
        context.Request.Method,
        context.Request.Path,
        status,
        stopwatch.ElapsedMilliseconds);
});


app.UseExceptionHandler();
app.UseStatusCodePages();

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

app.MapGet("/ping", () =>
    Results.Ok(new { service = "inventory", status = "up" }));

app.MapGet("/api/inventory", async(InventoryDbContext db, CancellationToken ct) =>
   {
       var entities = await db.InventoryItems.AsNoTracking().OrderBy(i=>i.Name).ToListAsync(ct);

       return Results.Ok(entities.Select(InventoryItemDto.From).ToList());

   }
);

app.MapGet("/api/inventory/{id:guid}", async (Guid id, InventoryDbContext db, CancellationToken ct) =>
{
    var item = await db.InventoryItems.AsNoTracking()
        .FirstOrDefaultAsync(i => i.Id == id, ct);

    return item is null
        ? Results.NotFound()
        : Results.Ok(InventoryItemDto.From(item));
});


app.MapPost("/api/inventory", async (CreateInventoryItemRequest request, InventoryDbContext db, CancellationToken ct) =>
{
    var errors = InventoryValidator.ValidateCreate(request);

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var sku = request.Sku.Trim().ToUpperInvariant();

    var exists = await db.InventoryItems.AnyAsync(i => i.Sku == sku, ct);
    if (exists)
    {
        return Results.Problem(
            title: "SKU already exists",
            detail: $"An item with SKU '{sku}' already exists",
            statusCode: StatusCodes.Status409Conflict);

    }

    var item = InventoryItem.Create(
        Guid.NewGuid(),
        sku,
        request.Name.Trim(),
        request.Quantity,
        request.Price,
        request.Description
        );
    db.InventoryItems.Add(item);

        await db.SaveChangesAsync(ct);

    return Results.Created(
        $"/api/inventory/{item.Id}",
        InventoryItemDto.From(item));
});



app.MapPut(
    "/api/inventory/{id:guid}",
    async (
        Guid id,
        UpdateInventoryItemRequest request,
        InventoryDbContext db,
        CancellationToken ct) =>
    {
        var errors = InventoryValidator.ValidateUpdate(request);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var item = await db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (item is null)
        {
            return Results.NotFound();
        }

        item.Update(
            request.Name.Trim(),
            request.Description,
            request.Quantity,
            request.Price);

        app.Logger.LogInformation(
    "{View}",
    db.ChangeTracker.DebugView.LongView);

        await db.SaveChangesAsync(ct);

        return Results.Ok(InventoryItemDto.From(item));
    });


app.MapDelete(
    "/api/inventory/{id:guid}",
    async (
        Guid id,
        InventoryDbContext db,
        CancellationToken ct) =>
    {
        var item = await db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (item is null)
        {
            return Results.NotFound();
        }

        db.InventoryItems.Remove(item);

        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    });
app.MapGet("/api/debug/boom", () =>
{
    throw new InvalidOperationException(
        "Secret detail: Host=localhost;Password=hunter2");
});

app.MapHealthChecks("/health");

app.MapGet("/api/inventory/by-sku/{sku}", async (string sku, InventoryDbContext db, CancellationToken ct) =>
{
    var normalized = sku.Trim().ToUpperInvariant();

    var item = await db.InventoryItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Sku == normalized, ct);

    return item is null
        ? Results.NotFound()
        : Results.Ok(InventoryItemDto.From(item));
});

app.Run();
