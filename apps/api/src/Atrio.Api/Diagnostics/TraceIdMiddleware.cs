using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Atrio.Api.Diagnostics;

public sealed class TraceIdMiddleware
{
    public const string HeaderName = "X-Trace-Id";
    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate _next)
    {
        this._next = _next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(traceId))
        {
            traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            if (string.IsNullOrWhiteSpace(traceId))
            {
                traceId = Guid.NewGuid().ToString("n");
            }
        }

        context.TraceIdentifier = traceId;
        context.Response.Headers[HeaderName] = traceId;

        await _next(context);
    }
}
