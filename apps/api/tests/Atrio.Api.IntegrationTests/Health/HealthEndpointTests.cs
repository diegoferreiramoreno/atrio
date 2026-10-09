using System.Net;
using Atrio.Api.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Atrio.Api.IntegrationTests.Health;

public class HealthEndpointTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public HealthEndpointTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Liveness_returns_200_and_readiness_returns_200_when_database_is_reachable()
    {
        using var factory = new ApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Atrio"] = _fixture.ConnectionString
                });
            });
        });

        var client = factory.CreateClient();

        var liveResponse = await client.GetAsync("/health/live");
        liveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var readyResponse = await client.GetAsync("/health/ready");
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Liveness_returns_200_and_readiness_returns_503_when_database_is_unreachable()
    {
        const string unreachableConnStr = "Host=127.0.0.1;Port=54329;Database=unreachable;Username=postgres;Password=postgres;Timeout=1;Command Timeout=1";

        using var factory = new ApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Atrio"] = unreachableConnStr
                });
            });
        });

        var client = factory.CreateClient();

        // Liveness deve continuar 200 mesmo sem PostgreSQL
        var liveResponse = await client.GetAsync("/health/live");
        liveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Readiness deve indicar 503
        var readyResponse = await client.GetAsync("/health/ready");
        readyResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        // Garantir que nenhum detalhe de infraestrutura sensível vaze
        var body = await readyResponse.Content.ReadAsStringAsync();
        body.Should().NotContain("Password");
        body.Should().NotContain("54329");
        body.Should().NotContain("unreachable");
    }
}
