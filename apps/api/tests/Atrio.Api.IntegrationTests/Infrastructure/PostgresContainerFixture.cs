using Testcontainers.PostgreSql;
using Xunit;

namespace Atrio.Api.IntegrationTests.Infrastructure;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("atrio_integration")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public PostgreSqlContainer Container => _container;

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
