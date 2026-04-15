using Microsoft.Extensions.Options;

namespace TonerTrack.Api.Middleware;

public sealed class ApiKeyMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-API-Key";

    public async Task InvokeAsync(HttpContext ctx)
    {
        // Skip Health checks
        if (ctx.Request.Path.StartsWithSegments("/health"))
        {
            await next(ctx); return;
        }

        var opts = ctx.RequestServices.GetRequiredService<IOptions<ApiKeyOptions>>().Value;

        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            await next(ctx); return; // not configured - allow all (dev mode)
        }

        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var key) || key != opts.ApiKey)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        await next(ctx);
    }
}

public sealed class ApiKeyOptions
{
    public const string Section = "ApiKey";
    public string ApiKey { get; set; } = "";
}