using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Atrio.Api.IntegrationTests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestVersion = "1.0.0-test";
    public const string TestCommit = "abcdef123456";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production"); // Garante que detalhes de exceção (stack trace) não vazem

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Build:Version"] = TestVersion,
                ["Build:Commit"] = TestCommit,
                ["EnableTestEndpoints"] = "true",
                ["ConnectionStrings:Atrio"] = "Host=localhost;Database=atrio_test;Username=postgres;Password=postgres"
            });
        });
    }
}
