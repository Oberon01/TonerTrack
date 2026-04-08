using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TonerTrack.Application.Common.Interfaces;

namespace TonerTrack.Infrastructure.NinjaRmm;

/// <summary>
/// NinjaRMM HTTP client.
/// - OAuth2 refresh_token flow — requires a refresh token obtained once via
///   the browser authorization code flow (e.g. via Postman).
/// - In-memory access token caching with 30-second expiry buffer.
/// - Thread-safe token refresh via SemaphoreSlim.
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
        int clientId, int ticketFormId, int locationId,
        string subject, string body, CancellationToken ct = default)
    {
        var token = await GetAccessTokenAsync(ct);

        var payload = new
        {
            clientId,
            ticketFormId,
            locationId,
            subject,
            description = new
            {
                @public = true,
                body,
                htmlBody = $"<p>{System.Net.WebUtility.HtmlEncode(body).Replace("\n", "<br/>")}</p>",
            },
            status = "NEW",
            type = "PROBLEM",
            severity = "NONE",
            priority = "NONE",
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        logger.LogDebug("NinjaRMM ticket payload: {Payload}", payloadJson);

        using var request = new HttpRequestMessage(HttpMethod.Post, "v2/ticketing/ticket")
        {
            Content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("Accept", "application/json");

        var response = await http.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        logger.LogDebug("NinjaRMM API response: {StatusCode} - {ResponseBody}",
            (int)response.StatusCode, responseBody);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Failed to create NinjaRMM ticket. Status: {StatusCode}, Response: {ResponseBody}",
                (int)response.StatusCode, responseBody);
            throw new InvalidOperationException(
                $"NinjaRMM API error: {(int)response.StatusCode} - {responseBody}");
        }

        var json = JsonSerializer.Deserialize<JsonElement>(responseBody);
        var ticketId = json.TryGetProperty("id", out var id) ? id.ToString() : "unknown";

        logger.LogInformation("NinjaRMM ticket created: {TicketId}", ticketId);
        return ticketId;
    }

    // Token management with in-memory caching and thread-safe refresh
    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiresAt)
            return _cachedToken;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiresAt)
                return _cachedToken;

            var tokenResp = await FetchTokenAsync(ct);
            _cachedToken = tokenResp.AccessToken;
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResp.ExpiresIn - 30);

            logger.LogDebug("NinjaRMM access token refreshed, expires in {ExpiresIn}s",
                tokenResp.ExpiresIn);

            return _cachedToken;
        }
        finally { _tokenLock.Release(); }
    }

    private async Task<TokenResponse> FetchTokenAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opts.RefreshToken))
            throw new InvalidOperationException(
                "NinjaRmm:RefreshToken is not configured. " +
                "Obtain a refresh token via Postman and store it with: " +
                "dotnet user-secrets set \"NinjaRmm:RefreshToken\" \"<token>\"");

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = _opts.RefreshToken,
            ["scope"] = _opts.Scope,
        };

        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{_opts.ClientId}:{_opts.ClientSecret}"));

        using var req = new HttpRequestMessage(HttpMethod.Post, "oauth/token")
        {
            Content = new FormUrlEncodedContent(form),
        };
        req.Headers.Add("Authorization", $"Basic {credentials}");

        var resp = await http.SendAsync(req, ct);
        var responseBody = await resp.Content.ReadAsStringAsync(ct);

        logger.LogDebug("Token response: {StatusCode} - {ResponseBody}",
            (int)resp.StatusCode, responseBody);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"NinjaRMM token refresh failed: {(int)resp.StatusCode} - {responseBody}");

        return JsonSerializer.Deserialize<TokenResponse>(responseBody)
               ?? throw new InvalidOperationException("Empty token response from NinjaRMM.");
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

// Options class for configuring NinjaRMM API credentials and defaults.
public sealed class NinjaRmmOptions
{
    public const string Section = "NinjaRmm";

    public string BaseUrl { get; set; } = "https://app.ninjarmm.com/";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string Scope { get; set; } = "monitoring management offline_access";
}