using System.Net;
using System.Text.Json;
using FluentValidation;
using TonerTrack.Domain.Exceptions;

namespace TonerTrack.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions and maps them to RFC 7807 problem-detail
/// JSON responses. Stack traces are never exposed to callers.
/// </summary>
public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Invokes the middleware, catching any exceptions thrown by downstream components.</summary>
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            await HandleAsync(ctx, ex);
        }
    }

    /// <summary>Handles exceptions thrown during request processing.</summary>
    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (statusCode, title, errors) = ex switch
        {
            ValidationException ve => (
                HttpStatusCode.UnprocessableEntity,
                "One or more validation errors occurred.",
                (object)ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

            PrinterNotFoundException nf => (
                HttpStatusCode.NotFound,
                nf.Message,
                (object)new { }),

            PrinterDomainException de => (
                HttpStatusCode.BadRequest,
                de.Message,
                (object)new { }),

            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                (object)new { })
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            logger.LogError(ex, "Unhandled exception");

        ctx.Response.StatusCode  = (int)statusCode;
        ctx.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            traceId = ctx.TraceIdentifier,
            errors,
        };

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOpts));
    }
}
