using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Infrastructure;
using Testcontainers.PostgreSql;

namespace OrderFlow.Inventory.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public string ConnectionString => _database.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new InventoryDbContext(options);
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() =>
        _database.DisposeAsync().AsTask();
}