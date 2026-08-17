using Jellyfin.Plugin.ContentRatings.Configuration;
using Jellyfin.Plugin.ContentRatings.Services;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ContentRatings;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "ContentRatings";
    public override Guid Id => Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    public override string Description => "Adds content ratings (MPAA, etc.), budget/revenue stats, and age ratings to movies using free APIs (Wikidata) - no API keys required";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "ContentRatings",
                EmbeddedResourcePath = "Jellyfin.Plugin.ContentRatings.Configuration.configPage.html"
            }
        };
    }
}