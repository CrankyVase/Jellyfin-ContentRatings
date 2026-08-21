using System.Globalization;
using Jellyfin.Plugin.ContentRatings.Api;
using Jellyfin.Plugin.ContentRatings.Configuration;
using Jellyfin.Plugin.ContentRatings.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Hosting;
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

    public override string Description => "Adds content advisory descriptors (nudity, violence, language), MPA ratings, and budget/revenue stats to movies using Wikidata - no API keys required";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        ];
    }

    public class ServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, MediaBrowser.Controller.IServerApplicationHost applicationHost)
        {
            serviceCollection.AddHttpClient<IWikidataService, WikidataService>(client =>
            {
                client.BaseAddress = new Uri("https://www.wikidata.org/w/api.php");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin.Plugin.ContentRatings/1.0");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            serviceCollection.AddSingleton<ICacheService, CacheService>();
            serviceCollection.AddSingleton<IContentRatingsProvider, ContentRatingsProvider>();
            serviceCollection.AddSingleton<IStartupFilter, ClientScriptStartupFilter>();

            serviceCollection.AddControllers()
                .AddApplicationPart(typeof(ContentRatingsController).Assembly);
        }
    }
}