using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ContentRatings.Models;

public class TmdbMovieDetails
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("original_title")]
    public string OriginalTitle { get; set; } = string.Empty;

    [JsonPropertyName("overview")]
    public string Overview { get; set; } = string.Empty;

    [JsonPropertyName("budget")]
    public long Budget { get; set; }

    [JsonPropertyName("revenue")]
    public long Revenue { get; set; }

    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; set; } = string.Empty;

    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }

    [JsonPropertyName("vote_count")]
    public int VoteCount { get; set; }

    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("tagline")]
    public string Tagline { get; set; } = string.Empty;

    [JsonPropertyName("imdb_id")]
    public string ImdbId { get; set; } = string.Empty;

    [JsonPropertyName("genres")]
    public List<TmdbGenre> Genres { get; set; } = new();

    [JsonPropertyName("production_companies")]
    public List<TmdbProductionCompany> ProductionCompanies { get; set; } = new();

    [JsonPropertyName("production_countries")]
    public List<TmdbProductionCountry> ProductionCountries { get; set; } = new();

    [JsonPropertyName("spoken_languages")]
    public List<TmdbSpokenLanguage> SpokenLanguages { get; set; } = new();
}

public class TmdbGenre
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class TmdbProductionCompany
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("logo_path")]
    public string LogoPath { get; set; } = string.Empty;

    [JsonPropertyName("origin_country")]
    public string OriginCountry { get; set; } = string.Empty;
}

public class TmdbProductionCountry
{
    [JsonPropertyName("iso_3166_1")]
    public string Iso3166_1 { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class TmdbSpokenLanguage
{
    [JsonPropertyName("english_name")]
    public string EnglishName { get; set; } = string.Empty;

    [JsonPropertyName("iso_639_1")]
    public string Iso639_1 { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class TmdbReleaseDatesResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("results")]
    public List<TmdbReleaseDateResult> Results { get; set; } = new();
}

public class TmdbReleaseDateResult
{
    [JsonPropertyName("iso_3166_1")]
    public string Iso3166_1 { get; set; } = string.Empty;

    [JsonPropertyName("release_dates")]
    public List<TmdbReleaseDate> ReleaseDates { get; set; } = new();
}

public class TmdbReleaseDate
{
    [JsonPropertyName("certification")]
    public string Certification { get; set; } = string.Empty;

    [JsonPropertyName("descriptors")]
    public List<string> Descriptors { get; set; } = new();

    [JsonPropertyName("iso_639_1")]
    public string Iso639_1 { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string Note { get; set; } = string.Empty;

    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public int Type { get; set; }
}

public class TmdbContentRatingsResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("results")]
    public List<TmdbContentRating> Results { get; set; } = new();
}

public class TmdbContentRating
{
    [JsonPropertyName("iso_3166_1")]
    public string Iso3166_1 { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public string Rating { get; set; } = string.Empty;

    [JsonPropertyName("descriptors")]
    public List<string> Descriptors { get; set; } = new();
}

public class TmdbSearchResponse
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("results")]
    public List<TmdbSearchResult> Results { get; set; } = new();

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }
}

public class TmdbSearchResult
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("original_title")]
    public string OriginalTitle { get; set; } = string.Empty;

    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; set; } = string.Empty;

    [JsonPropertyName("overview")]
    public string Overview { get; set; } = string.Empty;

    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }

    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }

    [JsonPropertyName("vote_count")]
    public int VoteCount { get; set; }
}