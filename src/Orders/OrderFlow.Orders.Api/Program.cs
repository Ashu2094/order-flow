using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderFlow.Contracts;
using OrderFlow.Orders.Api;
using OrderFlow.Orders.Application;
using OrderFlow.Orders.Domain;
using OrderFlow.Orders.Infrastructure;
using Scalar.AspNetCore;
using System.Diagnostics;

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
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<OrdersDbContext>("database");

builder.Services
    .AddOptions<InventoryServiceOptions>()
    .Bind(builder.Configuration.GetSection(
        InventoryServiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart(); // it validates the configuration when the application starts, ensuring that any misconfigurations are caught early.


builder.Services.AddHttpClient<IInventoryClient, InventoryClient>((serviceProvider, client) =>
{
    //serviceProvider Gives access to services registered in DI
    //client The HttpClient instance being configured.
    var options = serviceProvider
        .GetRequiredService<IOptions<InventoryServiceOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddOpenApi();

var rabbitHost = builder.Configuration["RabbitMq:Host"]
    ?? throw new InvalidOperationException(
        "RabbitMq:Host is missing.");

var rabbitUser = builder.Configuration["RabbitMq:Username"]
    ?? throw new InvalidOperationException(
        "RabbitMq:Username is missing. Set it with dotnet user-secrets.");

var rabbitPassword = builder.Configuration["RabbitMq:Password"]
    ?? throw new InvalidOperationException(
        "RabbitMq:Password is missing. Set it with dotnet user-secrets.");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockReservedConsumer>();
    x.AddConsumer<StockReservationFailedConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPassword);
        });

        cfg.ConfigureEndpoints(context);
    });
});


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

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<OrdersDbContext>();

    await db.Database.MigrateAsync();
}

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
        IInventoryClient inventory,
        IPublishEndpoint publishEndpoint,
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
            DateTime.UtcNow
        );

        for (var i = 0; i < request.Items.Count; i++)
        {
            var requested = request.Items[i];
            var sku = requested.Sku.Trim().ToUpperInvariant();

            var product = await inventory.GetBySkuAsync(sku, ct);

            if (product is null)
            {
                errors[$"items[{i}].sku"] =
                    [$"SKU '{sku}' does not exist in Inventory."];

                continue;
            }

            order.AddItem(
                Guid.NewGuid(),
                product.Sku,
                requested.Quantity,
                product.Price);
        }

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        await publishEndpoint.Publish(
            new OrderSubmitted(
                order.Id,
                order.CreatedAtUtc,
                order.Items
                    .Select(i => new OrderSubmittedItem(i.Sku, i.Quantity))
                    .ToList()),
            ct
        );

        return Results.Created($"/api/orders/{order.Id}",OrderDto.From(order));

    }).WithSummary("Create an order")
        .Produces<OrderDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem(); 

//app.MapGet("/api/debug/boom", () =>
//{
//    throw new InvalidOperationException("Secret detail from Orders");
//});
app.MapHealthChecks("/health");


app.MapGet(
    "/api/debug/config",
    (IOptions<InventoryServiceOptions> options) =>
        Results.Ok(new
        {
            inventoryBaseUrl = options.Value.BaseUrl
        }));

app.MapOpenApi();
app.MapScalarApiReference();



app.Run();