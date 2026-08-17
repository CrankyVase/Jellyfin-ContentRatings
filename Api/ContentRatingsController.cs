using Jellyfin.Plugin.ContentRatings.Models;
using Jellyfin.Plugin.ContentRatings.Services;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.WebDashboard.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.ContentRatings.Api;

[ApiController]
[Route("ContentRatings")]
[Authorize(Policy = "RequiresElevation")]
public class ContentRatingsController : ControllerBase
{
    private readonly IContentRatingsProvider _provider;
    private readonly ILibraryManager _libraryManager;

    public ContentRatingsController(
        IContentRatingsProvider provider,
        ILibraryManager libraryManager)
    {
        _provider = provider;
        _libraryManager = libraryManager;
    }

    [HttpGet("Movie/{itemId}")]
    public async Task<ActionResult<MovieEnhancedData>> GetMovieData(Guid itemId, CancellationToken cancellationToken)
    {
        var movie = _libraryManager.GetItemById(itemId) as Movie;
        
        if (movie == null)
        {
            return NotFound("Movie not found");
        }

        var data = await _provider.GetEnhancedDataAsync(movie, cancellationToken);
        
        if (data == null)
        {
            return NotFound("No enhanced data available for this movie");
        }

        return Ok(data);
    }

    [HttpPost("Movie/{itemId}/Refresh")]
    public async Task<ActionResult<MovieEnhancedData>> RefreshMovieData(Guid itemId, CancellationToken cancellationToken)
    {
        var movie = _libraryManager.GetItemById(itemId) as Movie;
        
        if (movie == null)
        {
            return NotFound("Movie not found");
        }

        var data = await _provider.RefreshMovieDataAsync(movie, cancellationToken);
        
        if (data == null)
        {
            return NotFound("Could not fetch enhanced data for this movie");
        }

        return Ok(data);
    }

    [HttpGet("Config")]
    public ActionResult<object> GetConfig()
    {
        return Ok(new
        {
            enabled = true
        });
    }
}