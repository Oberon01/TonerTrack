using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TonerTrack.Application.Common.Interfaces;

namespace TonerTrack.Infrastructure.NinjaRmm;

/// <summary>
/// NinjaRMM HTTP client.
/// - OAuth2 client-credentials token acquisition with in-memory caching
///   (same 30-second expiry buffer as the original Python implementation)
/// - Thread-safe via SemaphoreSlim during token refresh
/// </summary>
public sealed class NinjaRmmService(
    HttpClient http,
    IOptions<NinjaRmmOptions> opts,
    ILogger<NinjaRmmService> logger)
    : INinjaRmmService
{
    private readonly NinjaRmmOptions _opts = opts.Value;

    private string? _cachedToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    // INinjaRmmService implementation
    public async Task<string> CreateTonerTicketAsync(
        int clientId, int ticketFormId, int locationId, int nodeId,
        string subject, string body, CancellationToken ct = default)
    {
        var token = await GetAccessTokenAsync(ct);

        var payload = new
        {
            clientId,
            ticketFormId,
            locationId,
            nodeId,
            summary = subject,
            description = new
            {
                @public  = true,
                body,
                htmlBody = $"<p>{System.Net.WebUtility.HtmlEncode(body).Replace("\n", "<br/>")}</p>",
            },
            status = "NEW",
            type = "PROBLEM",
            severity = "NONE",
            priority = "NONE",
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v2/ticketing/ticket")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("Accept", "application/json");

        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var ticketId = json.TryGetProperty("id", out var id) ? id.ToString() : "unknown";

        logger.LogInformation("NinjaRMM ticket created: {TicketId}", ticketId);
        return ticketId;
    }

    // Token Management with in-memory caching and thread safety
    /// <summary>Gets a valid access token, using a cached token if available.</summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        // Fast path — valid cached token
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiresAt)
            return _cachedToken;

        await _tokenLock.WaitAsync(ct);
        try
        {
            // Double-check inside the lock
            if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiresAt)
                return _cachedToken;

            var tokenResp = await FetchTokenAsync(ct);
            _cachedToken = tokenResp.AccessToken;
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResp.ExpiresIn - 30);
            return _cachedToken;
        }
        finally { _tokenLock.Release(); }
    }

    /// <summary>Fetches a new access token from NinjaRMM.</summary>
    private async Task<TokenResponse> FetchTokenAsync(CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _opts.ClientId,
            ["client_secret"] = _opts.ClientSecret,
            ["scope"] = _opts.Scope,
        };

        using var req  = new HttpRequestMessage(HttpMethod.Post, "oauth/token")
        {
            Content = new FormUrlEncodedContent(form),
        };

        var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        return await resp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Empty token response from NinjaRMM.");
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

// @TODO: Consider validating these options (e.g. non-empty ClientId/ClientSecret) using IValidateOptions.
/// <summary>Configuration options for NinjaRMM API access, bound from appsettings.json.</summary>
public sealed class NinjaRmmOptions
{
    public const string Section = "NinjaRmm";

    public string BaseUrl { get; set; } = "https://app.ninjarmm.com/";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scope { get; set; } = "monitoring management control offline_access";
}
