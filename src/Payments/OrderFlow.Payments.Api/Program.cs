using OrderFlow.Payments.Application;
using OrderFlow.Payments.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("PaymentsDb")
    ?? throw new InvalidOperationException(
        "Connection string 'PaymentsDb' is missing. " +
        "Set it with dotnet user-secrets.");

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString);

var app = builder.Build();

app.MapGet("/ping", () =>
    Results.Ok(new { service = "payments", status = "up" }));

app.Run();