using System.Diagnostics;
using Atrio.Api.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Atrio.Api.Errors;

public static class ProblemDetailsExtensions
{
    public static void EnrichProblemDetails(ProblemDetails problemDetails, HttpContext httpContext)
    {
        var traceId = httpContext.TraceIdentifier;
        if (string.IsNullOrWhiteSpace(traceId))
        {
            traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString("n");
        }

        problemDetails.Extensions["traceId"] = traceId;

        if (!problemDetails.Extensions.ContainsKey("code"))
        {
            problemDetails.Extensions["code"] = problemDetails.Status switch
            {
                StatusCodes.Status400BadRequest => "bad_request",
                StatusCodes.Status401Unauthorized => "unauthorized",
                StatusCodes.Status403Forbidden => "forbidden",
                StatusCodes.Status404NotFound => "not_found",
                StatusCodes.Status409Conflict => "conflict",
                StatusCodes.Status422UnprocessableEntity => "validation_failed",
                _ => ErrorCodes.InternalError
            };
        }

        if (problemDetails.Status is null or >= 500)
        {
            problemDetails.Title = "Ocorreu um erro interno no servidor.";
            problemDetails.Detail = "Por favor, entre em contato com o suporte informando o identificador do erro.";
        }
    }
}
