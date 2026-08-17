using System.Text.Json;
using Jellyfin.Plugin.ContentRatings.Configuration;
using Jellyfin.Plugin.ContentRatings.Models;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Plugin.ContentRatings.Services;

public interface IOmdbService
{
    Task<OmdbMovieResponse?> GetMovieByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default);
    Task<OmdbMovieResponse?> GetMovieByTitleAsync(string title, int? year, CancellationToken cancellationToken = default);
}

public class OmdbService : IOmdbService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OmdbService> _logger;
    private readonly PluginConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public OmdbService(
        HttpClient httpClient,
        ILogger<OmdbService> logger,
        IOptions<PluginConfiguration> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _httpClient.BaseAddress = new Uri("https://www.omdbapi.com/");
    }

    public async Task<OmdbMovieResponse?> GetMovieByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"?apikey={_config.OmdbApiKey}&i={imdbId}&plot=full";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OMDb API error: {StatusCode} for IMDb ID {ImdbId}", response.StatusCode, imdbId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OmdbMovieResponse>(content, _jsonOptions);
            
            if (result?.Response == "False")
            {
                _logger.LogWarning("OMDb API error: {Error} for IMDb ID {ImdbId}", result.Error, imdbId);
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching OMDb movie by IMDb ID {ImdbId}", imdbId);
            return null;
        }
    }

    public async Task<OmdbMovieResponse?> GetMovieByTitleAsync(string title, int? year, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Uri.EscapeDataString(title);
            var url = $"?apikey={_config.OmdbApiKey}&t={query}&plot=full";
            
            if (year.HasValue)
            {
                url += $"&y={year.Value}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OMDb API error: {StatusCode} for title {Title}", response.StatusCode, title);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OmdbMovieResponse>(content, _jsonOptions);
            
            if (result?.Response == "False")
            {
                _logger.LogWarning("OMDb API error: {Error} for title {Title}", result.Error, title);
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching OMDb movie by title {Title}", title);
            return null;
        }
    }
}