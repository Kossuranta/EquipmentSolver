using System.Net.Http.Headers;
using System.Text.Json;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EquipmentSolver.Infrastructure.Services;

/// <summary>
/// IGDB API client with Twitch OAuth2 authentication and stale-while-revalidate caching.
/// Results are refreshed after 24h but remain usable for 72h, providing resilience if IGDB is down.
/// </summary>
public class IgdbService : IIgdbService
{
    private readonly HttpClient _httpClient;
    private readonly IgdbSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IgdbService> _logger;

    /// <summary>
    /// After this period, a background refresh is attempted on the next request.
    /// </summary>
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(24);

    /// <summary>
    /// After this period, cached data is evicted entirely and a fresh fetch is required.
    /// </summary>
    private static readonly TimeSpan ExpireAfter = TimeSpan.FromHours(72);

    private const string TokenCacheKey = "igdb_access_token";

    public IgdbService(
        HttpClient httpClient,
        IOptions<IgdbSettings> settings,
        IMemoryCache cache,
        ILogger<IgdbService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<GameSearchResult>> SearchGamesAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        limit = Math.Clamp(limit, 1, 50);

        var cacheKey = $"igdb_search:{query.ToLowerInvariant().Trim()}:{limit}";

        if (_cache.TryGetValue(cacheKey, out CachedSearchResult? cached) && cached is not null)
        {
            var age = DateTime.UtcNow - cached.FetchedAt;

            if (age < StaleAfter)
            {
                // Fresh — return immediately
                return cached.Results;
            }

            // Stale but still valid — return cached data, refresh in background
            _ = Task.Run(async () =>
            {
                try
                {
                    var fresh = await FetchAndCacheAsync(cacheKey, query, limit);
                    _logger.LogDebug("Background refresh for '{Query}' returned {Count} results", query, fresh.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Background refresh for IGDB query '{Query}' failed, serving stale cache", query);
                }
            });

            return cached.Results;
        }

        // No cache entry — must fetch synchronously
        return await FetchAndCacheAsync(cacheKey, query, limit);
    }

    /// <summary>
    /// Fetches from IGDB and stores in cache with a 72h absolute expiration.
    /// </summary>
    private async Task<List<GameSearchResult>> FetchAndCacheAsync(string cacheKey, string query, int limit)
    {
        var token = await GetAccessTokenAsync();
        var results = await QueryIgdbAsync(query, limit, token);

        var entry = new CachedSearchResult(results, DateTime.UtcNow);
        _cache.Set(cacheKey, entry, ExpireAfter);

        return results;
    }

    /// <summary>
    /// Gets a Twitch OAuth2 access token, caching it until near expiration.
    /// </summary>
    private async Task<string> GetAccessTokenAsync()
    {
        if (_cache.TryGetValue(TokenCacheKey, out string? token) && token is not null)
            return token;

        var url = $"https://id.twitch.tv/oauth2/token" +
                  $"?client_id={_settings.ClientId}" +
                  $"&client_secret={_settings.ClientSecret}" +
                  $"&grant_type=client_credentials";

        var response = await _httpClient.PostAsync(url, null);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        token = root.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("IGDB token response missing access_token.");

        var expiresIn = root.GetProperty("expires_in").GetInt32();

        // Cache token with a buffer before expiration
        var cacheDuration = TimeSpan.FromSeconds(Math.Max(expiresIn - 300, 60));
        _cache.Set(TokenCacheKey, token, cacheDuration);

        _logger.LogInformation("Obtained new IGDB access token, expires in {ExpiresIn}s", expiresIn);
        return token;
    }

    /// <summary>
    /// Queries the IGDB games endpoint.
    /// </summary>
    private async Task<List<GameSearchResult>> QueryIgdbAsync(string query, int limit, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games");
        request.Headers.Add("Client-ID", _settings.ClientId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // IGDB uses Apicalypse query language
        var body = $"search \"{EscapeQuery(query)}\"; fields name,cover.url; limit {limit};";
        request.Content = new StringContent(body);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var results = new List<GameSearchResult>();
        foreach (var game in doc.RootElement.EnumerateArray())
        {
            var result = new GameSearchResult
            {
                IgdbId = game.GetProperty("id").GetInt32(),
                Name = game.GetProperty("name").GetString() ?? "Unknown"
            };

            if (game.TryGetProperty("cover", out var cover) &&
                cover.TryGetProperty("url", out var coverUrl))
            {
                var url = coverUrl.GetString();
                if (url is not null)
                {
                    // IGDB returns protocol-relative URLs; ensure https and use cover_big size
                    url = url.Replace("t_thumb", "t_cover_big");
                    if (url.StartsWith("//"))
                        url = "https:" + url;
                    result.CoverUrl = url;
                }
            }

            results.Add(result);
        }

        _logger.LogDebug("IGDB search for '{Query}' returned {Count} results", query, results.Count);
        return results;
    }

    /// <summary>
    /// Escapes double quotes in the query for IGDB's Apicalypse syntax.
    /// </summary>
    private static string EscapeQuery(string query) => query.Replace("\"", "\\\"");

    /// <summary>
    /// Cache entry that tracks when the data was fetched for stale-while-revalidate logic.
    /// </summary>
    private sealed record CachedSearchResult(List<GameSearchResult> Results, DateTime FetchedAt);
}
