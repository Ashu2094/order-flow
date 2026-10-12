using OrderFlow.Orders.Application;
using OrderFlow.Orders.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.MapGet("/ping", () =>
    Results.Ok(new
    {
        service = "orders",
        status = "up"
    }));

app.Run();