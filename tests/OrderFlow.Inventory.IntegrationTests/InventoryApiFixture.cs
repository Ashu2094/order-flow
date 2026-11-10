
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace OrderFlow.Inventory.IntegrationTests;

public class InventoryApiFixture : IAsyncLifetime
{
    public const string TestSigningKey = "integration-tests-only-signing-key-0123456789";
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

        Environment.SetEnvironmentVariable("Jwt__SigningKey", TestSigningKey);

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


    public HttpClient CreateClient(params string[] roles)
    {
        var client = Factory.CreateClient();
        if (roles.Length > 0)
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", CreateToken(roles));
        return client;
    }

    private static string CreateToken(string[] roles)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, "test-user") };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "orderflow-dev",
            Audience = "orderflow-api",
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey)),
                SecurityAlgorithms.HmacSha256)
        });
    }
}
