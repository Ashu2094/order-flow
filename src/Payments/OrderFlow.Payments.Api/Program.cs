using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts;
using OrderFlow.Payments.Api;
using OrderFlow.Payments.Application;
using OrderFlow.Payments.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("PaymentsDb")
    ?? throw new InvalidOperationException(
        "Connection string 'PaymentsDb' is missing. " +
        "Set it with dotnet user-secrets.");

var rabbitHost = builder.Configuration["RabbitMq:Host"]
    ?? throw new InvalidOperationException("RabbitMq:Host is missing.");

var rabbitUser = builder.Configuration["RabbitMq:Username"]
    ?? throw new InvalidOperationException(
        "RabbitMq:Username is missing. Set it with dotnet user-secrets.");

var rabbitPassword = builder.Configuration["RabbitMq:Password"]
    ?? throw new InvalidOperationException(
        "RabbitMq:Password is missing. Set it with dotnet user-secrets.");

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString);

builder.Services
    .AddOptions<PaymentSimulationOptions>()
    .Bind(builder.Configuration.GetSection(
        PaymentSimulationOptions.SectionName));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ChargePaymentConsumer>();

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
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider
        .GetRequiredService<PaymentsDbContext>();

    await db.Database.MigrateAsync();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<PaymentsDbContext>();

    await db.Database.MigrateAsync();
}

app.MapGet("/ping", () =>
    Results.Ok(new { service = "payments", status = "up" }));



app.Run();