using Jellyfin.Plugin.ContentRatings.Api;
using Jellyfin.Plugin.ContentRatings.Services;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ContentRatings;

public class ServiceRegistrator : IPluginServiceRegistrator
{
    public ServiceRegistrator() { }

    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient<IWikidataService, WikidataService>(client =>
        {
            client.BaseAddress = new Uri("https://www.wikidata.org/w/api.php");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin.Plugin.ContentRatings/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        serviceCollection.AddSingleton<ICacheService, CacheService>();
        serviceCollection.AddSingleton<IContentRatingsProvider, ContentRatingsProvider>();

        // Register API controllers
        serviceCollection.AddControllers()
            .AddApplicationPart(typeof(ContentRatingsController).Assembly);
    }
}