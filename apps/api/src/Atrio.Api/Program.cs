using Atrio.Api.Diagnostics;
using Atrio.Api.Errors;
using Atrio.Api.System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ProblemDetailsExtensions.EnrichProblemDetails(ctx.ProblemDetails, ctx.HttpContext);
    };
});

var app = builder.Build();

app.UseMiddleware<TraceIdMiddleware>();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapSystemEndpoints();

if (app.Configuration.GetValue<bool>("EnableTestEndpoints"))
{
    app.MapGet("/api/v1/test/fail", () =>
    {
        throw new InvalidOperationException("Boom! Simulating unhandled internal failure.");
    });
}

app.Run();

public partial class Program;
