using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.ContentRatings.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public string TmdbApiKey { get; set; } = string.Empty;
    public string OmdbApiKey { get; set; } = string.Empty;
    public bool EnableBudgetRevenue { get; set; } = true;
    public bool EnableContentRatings { get; set; } = true;
    public bool EnableAgeRatings { get; set; } = true;
    public int CacheHours { get; set; } = 24;
    public string PreferredRegion { get; set; } = "US";
}