using System.Text.Json;
using Jellyfin.Plugin.ContentRatings.Configuration;
using Jellyfin.Plugin.ContentRatings.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Plugin.ContentRatings.Services;

public interface ICacheService
{
    Task<MovieEnhancedData?> GetCachedDataAsync(string movieId, CancellationToken cancellationToken = default);
    Task SetCachedDataAsync(string movieId, MovieEnhancedData data, CancellationToken cancellationToken = default);
    Task ClearCacheAsync(CancellationToken cancellationToken = default);
}

public class CacheService : ICacheService
{
    private readonly string _cachePath;
    private readonly ILogger _logger;
    private readonly PluginConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public CacheService(
        ILogger<CacheService> logger,
        IOptions<PluginConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        var pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "ContentRatings");
        _cachePath = Path.Combine(pluginPath, "cache");
        
        if (!Directory.Exists(_cachePath))
        {
            Directory.CreateDirectory(_cachePath);
        }
    }

    public async Task<MovieEnhancedData?> GetCachedDataAsync(string movieId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetCacheFilePath(movieId);
            
            if (!File.Exists(filePath))
            {
                return null;
            }

            var fileInfo = new FileInfo(filePath);
            var age = DateTime.UtcNow - fileInfo.LastWriteTimeUtc;
            
            if (age.TotalHours > _config.CacheHours)
            {
                File.Delete(filePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<MovieEnhancedData>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading cache for movie {MovieId}", movieId);
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetCachedDataAsync(string movieId, MovieEnhancedData data, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetCacheFilePath(movieId);
            data.LastUpdated = DateTime.UtcNow;
            
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing cache for movie {MovieId}", movieId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (Directory.Exists(_cachePath))
            {
                foreach (var file in Directory.GetFiles(_cachePath, "*.json"))
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private string GetCacheFilePath(string movieId)
    {
        var safeId = movieId.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
        return Path.Combine(_cachePath, $"{safeId}.json");
    }
}