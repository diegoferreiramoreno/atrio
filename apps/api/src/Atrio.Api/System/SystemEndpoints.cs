using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace Atrio.Api.System;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/system/version", (IConfiguration configuration) =>
        {
            var version = configuration["Build:Version"]
                ?? configuration["BUILD_VERSION"]
                ?? "0.1.0-dev";

            var commit = configuration["Build:Commit"]
                ?? configuration["BUILD_COMMIT"]
                ?? "local";

            return Results.Ok(new BuildInfo(version, commit));
        })
        .WithName("GetSystemVersion")
        .WithTags("System");

        return endpoints;
    }
}
