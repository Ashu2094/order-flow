using OrderFlow.Orders.Application;
using OrderFlow.Orders.Infrastructure;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Domain;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("OrdersDb")
    ?? throw new InvalidOperationException(
        "Connection string 'OrdersDb' is missing. Set it with dotnet user-secrets.");

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString);

var app = builder.Build();

app.MapGet("/ping", () =>
    Results.Ok(new { service = "orders", status = "up" }));

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

app.Run();