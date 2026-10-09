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

    [Fact]
    public async Task Same_api_instance_recovers_readiness_when_database_becomes_available_again()
    {
        // Aloca porta livre para vincular de forma fixa ao container, permitindo stop/start sem alterar a porta
        var tcpListener = new global::System.Net.Sockets.TcpListener(global::System.Net.IPAddress.Loopback, 0);
        tcpListener.Start();
        var hostPort = ((global::System.Net.IPEndPoint)tcpListener.LocalEndpoint).Port;
        tcpListener.Stop();

        var dedicatedPostgres = new Testcontainers.PostgreSql.PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("atrio_recovery")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(hostPort, 5432)
            .Build();

        await dedicatedPostgres.StartAsync();

        try
        {
            using var factory = new ApiFactory().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Atrio"] = dedicatedPostgres.GetConnectionString()
                    });
                });
            });

            var client = factory.CreateClient();

            // 1. Estado inicial: banco online -> readiness 200, liveness 200
            var initialReady = await client.GetAsync("/health/ready");
            initialReady.StatusCode.Should().Be(HttpStatusCode.OK);
            var initialLive = await client.GetAsync("/health/live");
            initialLive.StatusCode.Should().Be(HttpStatusCode.OK);

            // 2. Para o PostgreSQL: banco offline
            await dedicatedPostgres.StopAsync();

            // Bounded polling para verificar transição para 503
            var maxWait = TimeSpan.FromSeconds(15);
            var pollInterval = TimeSpan.FromMilliseconds(250);
            var start = DateTime.UtcNow;
            HttpStatusCode degradedStatus = HttpStatusCode.OK;

            while (DateTime.UtcNow - start < maxWait)
            {
                var degradedReady = await client.GetAsync("/health/ready");
                degradedStatus = degradedReady.StatusCode;
                if (degradedStatus == HttpStatusCode.ServiceUnavailable)
                {
                    break;
                }
                await Task.Delay(pollInterval);
            }

            degradedStatus.Should().Be(HttpStatusCode.ServiceUnavailable, "Readiness deve refletir a indisponibilidade do banco");

            // Liveness deve permanecer 200 durante a falha do banco
            var degradedLive = await client.GetAsync("/health/live");
            degradedLive.StatusCode.Should().Be(HttpStatusCode.OK, "Liveness não deve depender da disponibilidade do PostgreSQL");

            // 3. Reinicia o PostgreSQL: banco recuperado
            await dedicatedPostgres.StartAsync();

            // Bounded polling para verificar recuperação para 200 na MESMA instância da API
            start = DateTime.UtcNow;
            HttpStatusCode recoveredStatus = HttpStatusCode.ServiceUnavailable;

            while (DateTime.UtcNow - start < maxWait)
            {
                var recoveredReady = await client.GetAsync("/health/ready");
                recoveredStatus = recoveredReady.StatusCode;
                if (recoveredStatus == HttpStatusCode.OK)
                {
                    break;
                }
                await Task.Delay(pollInterval);
            }

            recoveredStatus.Should().Be(HttpStatusCode.OK, "Readiness deve se recuperar automaticamente quando o PostgreSQL voltar");

            var finalLive = await client.GetAsync("/health/live");
            finalLive.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await dedicatedPostgres.DisposeAsync();
        }
    }
}
