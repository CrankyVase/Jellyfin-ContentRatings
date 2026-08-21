using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Jellyfin.Plugin.ContentRatings.Services;

/// <summary>
/// Injects the ContentRatings client script into the web client's index.html response
/// on the fly. The container's index.html isn't writable at runtime, so instead of
/// patching the file on disk this rewrites the HTML response body as it's served -
/// which also means it survives web client updates without re-patching anything.
/// </summary>
public class ClientScriptStartupFilter : IStartupFilter
{
    private const string MarkerComment = "<!-- ContentRatings-injected -->";
    private const string ScriptTag = "<script plugin=\"ContentRatings\" src=\"/ContentRatings/ClientScript\" defer></script>";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                var path = context.Request.Path.Value ?? string.Empty;
                var looksLikeWebClient = path.Length == 0 || path == "/" || path.StartsWith("/web", StringComparison.OrdinalIgnoreCase);

                if (!looksLikeWebClient)
                {
                    await nextMiddleware();
                    return;
                }

                // The static file middleware may serve index.html via SendFileAsync, which
                // bypasses Response.Body entirely - swapping the IHttpResponseBodyFeature
                // (rather than just Response.Body) catches that path too.
                var originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>();
                using var buffer = new MemoryStream();
                context.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(buffer));

                try
                {
                    await nextMiddleware();
                }
                finally
                {
                    context.Features.Set(originalBodyFeature);
                }

                buffer.Seek(0, SeekOrigin.Begin);

                if (originalBodyFeature != null &&
                    context.Response.ContentType != null &&
                    context.Response.ContentType.Contains("text/html", StringComparison.OrdinalIgnoreCase) &&
                    context.Response.StatusCode == StatusCodes.Status200OK)
                {
                    var html = await new StreamReader(buffer).ReadToEndAsync();
                    var bodyCloseIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

                    if (!html.Contains(MarkerComment, StringComparison.Ordinal) && bodyCloseIndex >= 0)
                    {
                        html = html[..bodyCloseIndex] + MarkerComment + ScriptTag + html[bodyCloseIndex..];
                    }

                    var bytes = Encoding.UTF8.GetBytes(html);
                    context.Response.ContentLength = bytes.Length;
                    await originalBodyFeature.Stream.WriteAsync(bytes);
                }
                else if (originalBodyFeature != null)
                {
                    await buffer.CopyToAsync(originalBodyFeature.Stream);
                }
            });

            next(app);
        };
    }
}
