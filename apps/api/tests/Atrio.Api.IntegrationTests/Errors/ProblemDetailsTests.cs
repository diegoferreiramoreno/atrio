using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Atrio.Api.IntegrationTests.Errors;

public class ProblemDetailsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ProblemDetailsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Unexpected_exception_returns_problem_details_with_code_and_traceId_without_stack_trace()
    {
        var response = await _client.GetAsync("/api/v1/test/fail");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var rawBody = await response.Content.ReadAsStringAsync();
        rawBody.Should().NotContain("InvalidOperationException");
        rawBody.Should().NotContain("Boom! Simulating unhandled internal failure.");
        rawBody.Should().NotContain("at Atrio");
        rawBody.Should().NotContain("C:\\");

        var json = JsonSerializer.Deserialize<JsonElement>(rawBody);
        json.GetProperty("status").GetInt32().Should().Be(500);
        json.GetProperty("code").GetString().Should().Be("internal_error");

        var traceId = json.GetProperty("traceId").GetString();
        traceId.Should().NotBeNullOrWhiteSpace();

        response.Headers.GetValues("X-Trace-Id").Should().ContainSingle()
            .Which.Should().Be(traceId);
    }
}
