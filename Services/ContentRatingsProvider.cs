using Jellyfin.Plugin.ContentRatings.Configuration;
using Jellyfin.Plugin.ContentRatings.Models;
using Jellyfin.Plugin.ContentRatings.Services;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Plugin.ContentRatings.Services;

public interface IContentRatingsProvider
{
    Task<MovieEnhancedData?> GetEnhancedDataAsync(Movie movie, CancellationToken cancellationToken = default);
    Task<MovieEnhancedData?> RefreshMovieDataAsync(Movie movie, CancellationToken cancellationToken = default);
}

public class ContentRatingsProvider : IContentRatingsProvider
{
    private readonly IWikidataService _wikidataService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ContentRatingsProvider> _logger;
    private readonly PluginConfiguration _config;

    public ContentRatingsProvider(
        IWikidataService wikidataService,
        ICacheService cacheService,
        ILogger<ContentRatingsProvider> logger,
        IOptions<PluginConfiguration> config)
    {
        _wikidataService = wikidataService;
        _cacheService = cacheService;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<MovieEnhancedData?> GetEnhancedDataAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        var imdbId = movie.GetProviderId(MetadataProvider.Imdb);
        var tmdbId = movie.GetProviderId(MetadataProvider.Tmdb);
        
        var cacheKey = !string.IsNullOrEmpty(imdbId) ? imdbId : (tmdbId ?? movie.Id.ToString());

        var cachedData = await _cacheService.GetCachedDataAsync(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            return cachedData;
        }

        return await RefreshMovieDataAsync(movie, cancellationToken);
    }

    public async Task<MovieEnhancedData?> RefreshMovieDataAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        var imdbId = movie.GetProviderId(MetadataProvider.Imdb);
        var tmdbId = movie.GetProviderId(MetadataProvider.Tmdb);
        
        var cacheKey = !string.IsNullOrEmpty(imdbId) ? imdbId : (tmdbId ?? movie.Id.ToString());

        WikidataMovieData? wikidataMovie = null;

        if (!string.IsNullOrEmpty(imdbId))
        {
            wikidataMovie = await _wikidataService.GetMovieDataAsync(imdbId, cancellationToken);
        }

        if (wikidataMovie == null && !string.IsNullOrEmpty(tmdbId))
        {
            // Try to find via TMDB ID by searching title+year
            wikidataMovie = await _wikidataService.GetMovieDataByTitleAsync(movie.Name, movie.ProductionYear, cancellationToken);
        }

        if (wikidataMovie == null)
        {
            // Last resort: search by title and year
            wikidataMovie = await _wikidataService.GetMovieDataByTitleAsync(movie.Name, movie.ProductionYear, cancellationToken);
        }

        if (wikidataMovie == null)
        {
            _logger.LogDebug("No Wikidata found for movie {MovieName}", movie.Name);
            return null;
        }

        var enhancedData = new MovieEnhancedData
        {
            MovieId = cacheKey,
            ImdbId = wikidataMovie.ImdbId ?? imdbId ?? string.Empty
        };

        if (_config.EnableContentRatings)
        {
            enhancedData.ContentRatings = BuildContentRatings(wikidataMovie);
        }

        if (_config.EnableBudgetRevenue)
        {
            enhancedData.FinancialData = BuildFinancialData(wikidataMovie);
        }

        if (_config.EnableAgeRatings)
        {
            enhancedData.AgeRatings = BuildAgeRatings(wikidataMovie);
        }

        await _cacheService.SetCachedDataAsync(cacheKey, enhancedData, cancellationToken);
        
        return enhancedData;
    }

    private List<ContentRatingData> BuildContentRatings(WikidataMovieData wikidata)
    {
        var ratings = new List<ContentRatingData>();

        if (!string.IsNullOrEmpty(wikidata.MpaaRating) || wikidata.ContentDescriptors.Count > 0)
        {
            ratings.Add(new ContentRatingData
            {
                Source = "Wikidata (MPA)",
                Rating = wikidata.MpaaRating,
                Description = GetMpaaDescription(wikidata.MpaaRating),
                Region = "US",
                AgeRating = wikidata.MpaaRating,
                Descriptors = wikidata.ContentDescriptors
            });
        }

        return ratings;
    }

    private FinancialData? BuildFinancialData(WikidataMovieData wikidata)
    {
        if (wikidata.Budget <= 0 && wikidata.Revenue <= 0)
        {
            return null;
        }

        var profitLoss = wikidata.Revenue - wikidata.Budget;
        var roi = wikidata.Budget > 0 ? (double)profitLoss / wikidata.Budget * 100 : 0;

        return new FinancialData
        {
            Budget = wikidata.Budget,
            Revenue = wikidata.Revenue,
            BudgetFormatted = FormatCurrency(wikidata.Budget),
            RevenueFormatted = FormatCurrency(wikidata.Revenue),
            ProfitLoss = profitLoss.ToString(),
            ProfitLossFormatted = FormatCurrency(profitLoss),
            RoiPercentage = Math.Round(roi, 1),
            Source = "Wikidata"
        };
    }

    private List<AgeRatingData> BuildAgeRatings(WikidataMovieData wikidata)
    {
        var ratings = new List<AgeRatingData>();

        if (!string.IsNullOrEmpty(wikidata.MpaaRating))
        {
            ratings.Add(new AgeRatingData
            {
                Rating = wikidata.MpaaRating,
                Region = "US",
                Description = GetMpaaDescription(wikidata.MpaaRating),
                Source = "Wikidata"
            });
        }

        // Could add more country-specific ratings by querying additional Wikidata properties
        // P1659 (rating system) + country qualifiers

        return ratings;
    }

    private string FormatCurrency(long value)
    {
        if (value >= 1_000_000_000)
            return $"${value / 1_000_000_000.0:F1}B";
        if (value >= 1_000_000)
            return $"${value / 1_000_000.0:F1}M";
        if (value >= 1_000)
            return $"${value / 1_000.0:F1}K";
        return $"${value}";
    }

    private string GetMpaaDescription(string rating)
    {
        return rating switch
        {
            "G" => "General Audiences - All ages admitted",
            "PG" => "Parental Guidance Suggested - Some material may not be suitable for children",
            "PG-13" => "Parents Strongly Cautioned - Some material may be inappropriate for children under 13",
            "R" => "Restricted - Under 17 requires accompanying parent or adult guardian",
            "NC-17" => "Adults Only - No one 17 and under admitted",
            "Unrated" => "Not rated by MPAA",
            "Not Rated" => "Not rated by MPAA",
            _ => rating
        };
    }
}