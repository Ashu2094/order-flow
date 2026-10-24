
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace OrderFlow.Inventory.IntegrationTests;

public class InventoryApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database =
       new PostgreSqlBuilder("postgres:16-alpine").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__InventoryDb",
            _database.GetConnectionString());

        Environment.SetEnvironmentVariable(
            "Database__MigrateOnStartup",
            "true");

        Environment.SetEnvironmentVariable(
            "RabbitMq__Host",
            "localhost");

        Environment.SetEnvironmentVariable(
            "RabbitMq__Username",
            "test");

        Environment.SetEnvironmentVariable(
            "RabbitMq__Password",
            "test");

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                    services.AddMassTransitTestHarness()));
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _database.DisposeAsync();
    }
}
