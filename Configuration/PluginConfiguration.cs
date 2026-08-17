using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ContentRatings.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public PluginConfiguration()
    {
        EnableContentRatings = true;
        EnableBudgetRevenue = true;
        EnableAgeRatings = true;
        CacheHours = 24;
        PreferredRegion = "US";
    }

    public bool EnableContentRatings { get; set; }

    public bool EnableBudgetRevenue { get; set; }

    public bool EnableAgeRatings { get; set; }

    public int CacheHours { get; set; }

    public string PreferredRegion { get; set; }
}