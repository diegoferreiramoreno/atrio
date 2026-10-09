using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Atrio.Api.IntegrationTests.System;

public class SystemEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public SystemEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_system_version_returns_200_and_build_info()
    {
        var response = await _client.GetAsync("/api/v1/system/version");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("version").GetString().Should().Be(ApiFactory.TestVersion);
        content.GetProperty("commit").GetString().Should().Be(ApiFactory.TestCommit);
    }
}
