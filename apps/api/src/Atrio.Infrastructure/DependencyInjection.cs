using Atrio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atrio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AtrioDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connStr = config.GetConnectionString("Atrio")
                ?? config["ConnectionStrings:Atrio"]
                ?? config["CONNECTIONSTRINGS__ATRIO"];

            if (!string.IsNullOrWhiteSpace(connStr))
            {
                options.UseNpgsql(connStr);
            }
        });

        services.AddHealthChecks()
            .AddNpgSql(
                connectionStringFactory: sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    return config.GetConnectionString("Atrio")
                        ?? config["ConnectionStrings:Atrio"]
                        ?? config["CONNECTIONSTRINGS__ATRIO"]
                        ?? string.Empty;
                },
                name: "postgres",
                tags: ["ready"]);

        return services;
    }
}
