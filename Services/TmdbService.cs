using System.Text.Json;
using Jellyfin.Plugin.ContentRatings.Configuration;
using Jellyfin.Plugin.ContentRatings.Models;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Plugin.ContentRatings.Services;

public interface ITmdbService
{
    Task<TmdbMovieDetails?> GetMovieDetailsAsync(int tmdbId, CancellationToken cancellationToken = default);
    Task<TmdbMovieDetails?> GetMovieDetailsByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default);
    Task<TmdbReleaseDatesResponse?> GetReleaseDatesAsync(int tmdbId, CancellationToken cancellationToken = default);
    Task<TmdbContentRatingsResponse?> GetContentRatingsAsync(int tmdbId, CancellationToken cancellationToken = default);
    Task<int?> SearchMovieAsync(string title, int? year, CancellationToken cancellationToken = default);
}

public class TmdbService : ITmdbService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TmdbService> _logger;
    private readonly PluginConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public TmdbService(
        HttpClient httpClient,
        ILogger<TmdbService> logger,
        IOptions<PluginConfiguration> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _httpClient.BaseAddress = new Uri("https://api.themoviedb.org/3/");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.TmdbApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<TmdbMovieDetails?> GetMovieDetailsAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"movie/{tmdbId}?append_to_response=release_dates,content_ratings", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB API error: {StatusCode} for movie {TmdbId}", response.StatusCode, tmdbId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TmdbMovieDetails>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching TMDB movie details for {TmdbId}", tmdbId);
            return null;
        }
    }

    public async Task<TmdbMovieDetails?> GetMovieDetailsByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"find/{imdbId}?external_source=imdb_id", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB API error: {StatusCode} for IMDb ID {ImdbId}", response.StatusCode, imdbId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var findResponse = JsonSerializer.Deserialize<TmdbFindResponse>(content, _jsonOptions);
            
            if (findResponse?.MovieResults?.Count > 0)
            {
                return await GetMovieDetailsAsync(findResponse.MovieResults[0].Id, cancellationToken);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding TMDB movie by IMDb ID {ImdbId}", imdbId);
            return null;
        }
    }

    public async Task<TmdbReleaseDatesResponse?> GetReleaseDatesAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"movie/{tmdbId}/release_dates", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB API error: {StatusCode} for release dates {TmdbId}", response.StatusCode, tmdbId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TmdbReleaseDatesResponse>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching release dates for {TmdbId}", tmdbId);
            return null;
        }
    }

    public async Task<TmdbContentRatingsResponse?> GetContentRatingsAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"movie/{tmdbId}/content_ratings", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB API error: {StatusCode} for content ratings {TmdbId}", response.StatusCode, tmdbId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TmdbContentRatingsResponse>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content ratings for {TmdbId}", tmdbId);
            return null;
        }
    }

    public async Task<int?> SearchMovieAsync(string title, int? year, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Uri.EscapeDataString(title);
            var url = $"search/movie?query={query}&include_adult=false";
            
            if (year.HasValue)
            {
                url += $"&year={year.Value}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB search error: {StatusCode} for {Title}", response.StatusCode, title);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResponse = JsonSerializer.Deserialize<TmdbSearchResponse>(content, _jsonOptions);
            
            if (searchResponse?.Results?.Count > 0)
            {
                return searchResponse.Results[0].Id;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching TMDB for {Title}", title);
            return null;
        }
    }

    private class TmdbFindResponse
    {
        [JsonPropertyName("movie_results")]
        public List<TmdbSearchResult> MovieResults { get; set; } = new();
    }
}