using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Application;
using OrderFlow.Orders.Domain;
using OrderFlow.Orders.Infrastructure;
using OrderFlow.Orders.Api;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("OrdersDb")
    ?? throw new InvalidOperationException(
        "Connection string 'OrdersDb' is missing. Set it with dotnet user-secrets.");

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapGet("/ping", () =>
    Results.Ok(new { service = "orders", status = "up" }));

app.MapGet("/api/orders", async (
    int? page,
    int? pageSize,
    OrdersDbContext db,
    CancellationToken ct) =>
{
    var pageNumber = page ?? 1;
    var size = pageSize ?? 10;

    var errors = new Dictionary<string, string[]>();
    if (pageNumber < 1)
    {
        errors["page"] = ["Page must be 1 or greater."];
    }

    if (size < 1 || size > 100)
    {
        errors["pageSize"] =
            ["Page size must be between 1 and 100."];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var query = db.Orders.AsNoTracking();

    var total = await query.CountAsync(ct); //This obtains the total number of orders, not just the number on the current page.

    var items = await query
        .OrderByDescending(o => o.CreatedAtUtc)
        .ThenBy(o => o.Id)//ThenBy(o => o.Id) gives the results a consistent tie-breaker when creation timestamps match.
        .Skip((pageNumber - 1) * size)
        .Take(size)
        .Select(o => new OrderSummaryDto(
            o.Id,
            o.CustomerName,
            o.Status.ToString(),
            o.CreatedAtUtc,
            o.Items.Count,
            o.Items.Sum(i => i.Quantity * i.UnitPrice)))
        .ToListAsync(ct);

    return Results.Ok(
        new PagedResult<OrderSummaryDto>(
            items,
            pageNumber,
            size,
            total));
});

app.MapGet(
    "/api/orders/{id:guid}",
    async (
        Guid id,
        OrdersDbContext db,
        CancellationToken ct) =>
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)//It tells EF Core to load the order's related items as part of the query. Without it, the order might load successfully while its Items collection remains empty.
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        return order is null
            ? Results.NotFound()
            : Results.Ok(OrderDto.From(order));
    });

app.MapPost(
    "/api/orders",
    async (
        CreateOrderRequest request,
        OrdersDbContext db,
        CancellationToken ct) =>
    {
        var errors = OrderValidator.ValidateCreate(request);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var order = Order.Create(
            Guid.NewGuid(),
            request.CustomerName.Trim(),
            DateTime.UtcNow);

        foreach (var item in request.Items)
        {
            order.AddItem(
                Guid.NewGuid(),
                item.Sku.Trim().ToUpperInvariant(),
                item.Quantity,
                item.UnitPrice);
        }

        db.Orders.Add(order);

        await db.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/orders/{order.Id}",
            OrderDto.From(order));
    });

//app.MapGet("/api/debug/boom", () =>
//{
//    throw new InvalidOperationException("Secret detail from Orders");
//});

app.Run();