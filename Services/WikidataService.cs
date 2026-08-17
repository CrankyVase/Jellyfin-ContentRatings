using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.ContentRatings.Configuration;
using Jellyfin.Plugin.ContentRatings.Models;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Plugin.ContentRatings.Services;

public interface IWikidataService
{
    Task<WikidataMovieData?> GetMovieDataAsync(string imdbId, CancellationToken cancellationToken = default);
    Task<WikidataMovieData?> GetMovieDataByTitleAsync(string title, int? year, CancellationToken cancellationToken = default);
    Task<string?> SearchMovieAsync(string title, int? year, CancellationToken cancellationToken = default);
}

public class WikidataService : IWikidataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WikidataService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public WikidataService(
        HttpClient httpClient,
        ILogger<WikidataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _httpClient.BaseAddress = new Uri("https://www.wikidata.org/w/api.php");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin.Plugin.ContentRatings/1.0");
    }

    public async Task<WikidataMovieData?> GetMovieDataAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Search by IMDb ID (P345)
            var entityId = await SearchByImdbIdAsync(imdbId, cancellationToken);
            if (string.IsNullOrEmpty(entityId))
            {
                return null;
            }

            return await GetEntityDataAsync(entityId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Wikidata for IMDb ID {ImdbId}", imdbId);
            return null;
        }
    }

    public async Task<WikidataMovieData?> GetMovieDataByTitleAsync(string title, int? year, CancellationToken cancellationToken = default)
    {
        try
        {
            var entityId = await SearchMovieAsync(title, year, cancellationToken);
            if (string.IsNullOrEmpty(entityId))
            {
                return null;
            }

            return await GetEntityDataAsync(entityId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Wikidata for title {Title}", title);
            return null;
        }
    }

    public async Task<string?> SearchMovieAsync(string title, int? year, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = BuildSearchQuery(title, year);
            var url = $"?action=wbsearchentities&search={Uri.EscapeDataString(query)}&language=en&format=json&type=item&limit=5";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResponse = JsonSerializer.Deserialize<WikidataSearchResponse>(content, _jsonOptions);

            if (searchResponse?.Search?.Count > 0)
            {
                // Filter for films (Q11424)
                var film = searchResponse.Search.FirstOrDefault(s => 
                    s.Description?.Contains("film", StringComparison.OrdinalIgnoreCase) == true ||
                    s.Label?.Contains("film", StringComparison.OrdinalIgnoreCase) == true);
                
                return film?.Id ?? searchResponse.Search[0].Id;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Wikidata for {Title}", title);
            return null;
        }
    }

    private async Task<string?> SearchByImdbIdAsync(string imdbId, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"?action=wbsearchentities&search={Uri.EscapeDataString(imdbId)}&language=en&format=json&type=item&limit=5";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResponse = JsonSerializer.Deserialize<WikidataSearchResponse>(content, _jsonOptions);

            if (searchResponse?.Search?.Count > 0)
            {
                // Verify it has the IMDb ID property
                foreach (var result in searchResponse.Search)
                {
                    var entity = await GetEntityDataAsync(result.Id, cancellationToken);
                    if (entity?.ImdbId == imdbId)
                    {
                        return result.Id;
                    }
                }
                return searchResponse.Search[0].Id;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Wikidata by IMDb ID {ImdbId}", imdbId);
            return null;
        }
    }

    private async Task<WikidataMovieData?> GetEntityDataAsync(string entityId, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"?action=wbgetentities&ids={entityId}&format=json&props=claims|labels|descriptions&languages=en";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var entityResponse = JsonSerializer.Deserialize<WikidataEntityResponse>(content, _jsonOptions);

            if (entityResponse?.Entities?.TryGetValue(entityId, out var entity) == true)
            {
                return ParseEntityData(entity, entityId);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity data for {EntityId}", entityId);
            return null;
        }
    }

    private WikidataMovieData ParseEntityData(WikidataEntity entity, string entityId)
    {
        var data = new WikidataMovieData
        {
            EntityId = entityId,
            Title = entity.Labels?.En?.Value ?? string.Empty,
            Description = entity.Descriptions?.En?.Value ?? string.Empty
        };

        if (entity.Claims == null) return data;

        // IMDb ID (P345)
        if (entity.Claims.TryGetValue("P345", out var imdbClaims))
        {
            data.ImdbId = imdbClaims.FirstOrDefault()?.Mainsnak?.Datavalue?.Value?.ToString() ?? string.Empty;
        }

        // Title (P1476)
        if (entity.Claims.TryGetValue("P1476", out var titleClaims))
        {
            data.OriginalTitle = titleClaims.FirstOrDefault()?.Mainsnak?.Datavalue?.Value?.ToString() ?? string.Empty;
        }

        // Publication date / Release date (P577)
        if (entity.Claims.TryGetValue("P577", out var dateClaims))
        {
            var dateVal = dateClaims.FirstOrDefault()?.Mainsnak?.Datavalue?.Value?.ToString();
            if (!string.IsNullOrEmpty(dateVal))
            {
                data.ReleaseDate = ParseWikidataDate(dateVal);
            }
        }

        // Duration (P2047)
        if (entity.Claims.TryGetValue("P2047", out var durationClaims))
        {
            var durationVal = durationClaims.FirstOrDefault()?.Mainsnak?.Datavalue?.Value;
            if (durationVal is JsonElement elem && elem.TryGetProperty("amount", out var amount))
            {
                data.RuntimeMinutes = (int)Math.Round(amount.GetDouble());
            }
        }

        // Budget (P2130)
        if (entity.Claims.TryGetValue("P2130", out var budgetClaims))
        {
            var budgetVal = budgetClaims.FirstOrDefault()?.Mainsnak?.Datavalue?.Value;
            if (budgetVal is JsonElement bElem && bElem.TryGetProperty("amount", out var bAmount))
            {
                data.Budget = (long)bAmount.GetDouble();
            }
        }

        // Box office / Revenue (P2142)
        if (entity.Claims.TryGetValue("P2142", out var revenueClaims))
        {
            var revenueVal = revenueClaims.FirstOrDefault()?.Mainsnak?.Datavalue?.Value;
            if (revenueVal is JsonElement rElem && rElem.TryGetProperty("amount", out var rAmount))
            {
                data.Revenue = (long)rAmount.GetDouble();
            }
        }

        // MPAA rating (P1658) - Content rating
        if (entity.Claims.TryGetValue("P1658", out var mpaaClaims))
        {
            foreach (var claim in mpaaClaims)
            {
                var rating = claim.Mainsnak?.Datavalue?.Value?.ToString();
                if (!string.IsNullOrEmpty(rating))
                {
                    data.MpaaRating = rating;
                    break;
                }
            }
        }

        // Rating system (P1659) - for MPAA
        // Country-specific ratings would be more complex, but we can get the main one

        // Genre (P136)
        if (entity.Claims.TryGetValue("P136", out var genreClaims))
        {
            foreach (var claim in genreClaims)
            {
                var genreId = claim.Mainsnak?.Datavalue?.Value?.ToString();
                if (!string.IsNullOrEmpty(genreId))
                {
                    data.Genres.Add(genreId);
                }
            }
        }

        // Country of origin (P495)
        if (entity.Claims.TryGetValue("P495", out var countryClaims))
        {
            foreach (var claim in countryClaims)
            {
                var countryId = claim.Mainsnak?.Datavalue?.Value?.ToString();
                if (!string.IsNullOrEmpty(countryId))
                {
                    data.Countries.Add(countryId);
                }
            }
        }

        return data;
    }

    private string BuildSearchQuery(string title, int? year)
    {
        var query = title;
        if (year.HasValue)
        {
            query += $" {year.Value}";
        }
        return query;
    }

    private string ParseWikidataDate(string dateStr)
    {
        // Wikidata dates are like "+2023-01-15T00:00:00Z"
        if (dateStr.StartsWith("+") && dateStr.Length >= 11)
        {
            return dateStr.Substring(1, 10); // YYYY-MM-DD
        }
        return dateStr;
    }

    // Response models
    private class WikidataSearchResponse
    {
        [JsonPropertyName("search")]
        public List<WikidataSearchResult> Search { get; set; } = new();
    }

    private class WikidataSearchResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    private class WikidataEntityResponse
    {
        [JsonPropertyName("entities")]
        public Dictionary<string, WikidataEntity> Entities { get; set; } = new();
    }

    private class WikidataEntity
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("labels")]
        public Dictionary<string, WikidataLabel> Labels { get; set; } = new();

        [JsonPropertyName("descriptions")]
        public Dictionary<string, WikidataLabel> Descriptions { get; set; } = new();

        [JsonPropertyName("claims")]
        public Dictionary<string, List<WikidataClaim>> Claims { get; set; } = new();
    }

    private class WikidataLabel
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    private class WikidataClaim
    {
        [JsonPropertyName("mainsnak")]
        public WikidataSnak Mainsnak { get; set; } = new();
    }

    private class WikidataSnak
    {
        [JsonPropertyName("datavalue")]
        public WikidataValue Datavalue { get; set; } = new();
    }

    private class WikidataValue
    {
        [JsonPropertyName("value")]
        public object Value { get; set; } = new();
    }
}

public class WikidataMovieData
{
    public string EntityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string OriginalTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImdbId { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public int? RuntimeMinutes { get; set; }
    public long Budget { get; set; }
    public long Revenue { get; set; }
    public string MpaaRating { get; set; } = string.Empty;
    public List<string> Genres { get; set; } = new();
    public List<string> Countries { get; set; } = new();
}