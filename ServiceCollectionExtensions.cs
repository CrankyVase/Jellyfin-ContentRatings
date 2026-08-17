using Jellyfin.Plugin.ContentRatings.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ContentRatings;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContentRatingsServices(this IServiceCollection services)
    {
        services.AddHttpClient<IWikidataService, WikidataService>(client =>
        {
            client.BaseAddress = new Uri("https://www.wikidata.org/w/api.php");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin.Plugin.ContentRatings/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IContentRatingsProvider, ContentRatingsProvider>();

        return services;
    }
}