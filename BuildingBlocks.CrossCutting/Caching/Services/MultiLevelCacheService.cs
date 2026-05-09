using System.Diagnostics;
using BuildingBlocks.CrossCutting.Caching.Configuration;
using BuildingBlocks.CrossCutting.Caching.Core;
using BuildingBlocks.CrossCutting.Caching.Interfaces;
using BuildingBlocks.CrossCutting.Caching.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.CrossCutting.Caching.Services;

/// <summary>
/// Multi-level cache service with L1 (Memory) and L2 (Redis) providers.
/// </summary>
public class MultiLevelCacheService : IDistributedCacheService
{
    private readonly CacheSettings _settings;
    private readonly ICacheProvider? _l1Provider;
    private readonly ICacheProvider? _l2Provider;
    private readonly ILogger<MultiLevelCacheService> _logger;
    private readonly CacheKeyGenerator _keyGenerator;
    private readonly JsonCacheSerializer _serializer;
    private readonly CacheStatistics _statistics;

    public MultiLevelCacheService(
        IOptions<CacheSettings> settings,
        IEnumerable<ICacheProvider> providers,
        ILogger<MultiLevelCacheService> logger,
        CacheKeyGenerator keyGenerator,
        JsonCacheSerializer serializer)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _statistics = new CacheStatistics();

        var providerList = providers?.ToList() ?? throw new ArgumentNullException(nameof(providers));

        _l1Provider = _settings.EnableL1Cache
            ? providerList.FirstOrDefault(p => p.ProviderName == "Memory")
                ?? throw new InvalidOperationException("Memory cache provider not found")
            : null;

        _l2Provider = _settings.EnableL2Cache
            ? providerList.FirstOrDefault(p => p.ProviderName == "Redis")
                ?? throw new InvalidOperationException("Redis cache provider not found")
            : null;

        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        if (_settings.DefaultTtlSeconds < 0)
            throw new Exceptions.ConfigurationException(nameof(_settings.DefaultTtlSeconds), "Must be >= 0");

        if (_settings.OperationTimeoutSeconds <= 0)
            throw new Exceptions.ConfigurationException(nameof(_settings.OperationTimeoutSeconds), "Must be > 0");

        if (_settings.CompressionThresholdBytes < 0)
            throw new Exceptions.ConfigurationException(nameof(_settings.CompressionThresholdBytes), "Must be >= 0");
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // Check L1 cache first
            var l1Data = _l1Provider != null
                ? await _l1Provider.GetAsync(key, cancellationToken)
                : null;
            if (l1Data != null)
            {
                _statistics.RecordHit();
                _statistics.RecordL1Hit();
                sw.Stop();
                _statistics.RecordOperationDuration("Get", sw.Elapsed.TotalMilliseconds);

                if (_settings.EnableVerboseLogging)
                {
                    _logger.LogInformation("Cache hit (L1) for key '{Key}' in {Duration}ms", key, sw.Elapsed.TotalMilliseconds);
                }

                return _serializer.Deserialize<T>(CompressionHelper.Decompress(l1Data));
            }

            // Check L2 cache
            var l2Data = _l2Provider != null
                ? await _l2Provider.GetAsync(key, cancellationToken)
                : null;
            if (l2Data != null)
            {
                _statistics.RecordHit();
                _statistics.RecordL2Hit();

                // Promote to L1
                if (_l1Provider != null)
                {
                    await _l1Provider.SetAsync(key, l2Data, TimeSpan.FromSeconds(_settings.DefaultTtlSeconds), cancellationToken);
                }

                sw.Stop();
                _statistics.RecordOperationDuration("Get", sw.Elapsed.TotalMilliseconds);

                if (_settings.EnableVerboseLogging)
                {
                    _logger.LogInformation("Cache hit (L2) for key '{Key}' in {Duration}ms, promoted to L1", key, sw.Elapsed.TotalMilliseconds);
                }

                return _serializer.Deserialize<T>(CompressionHelper.Decompress(l2Data));
            }

            // Cache miss
            _statistics.RecordMiss();
            sw.Stop();
            _statistics.RecordOperationDuration("Get", sw.Elapsed.TotalMilliseconds);

            _logger.LogDebug("Cache miss for key '{Key}' in {Duration}ms", key, sw.Elapsed.TotalMilliseconds);

            return default;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error getting cache key '{Key}' after {Duration}ms", key, sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // Serialize
            var data = _serializer.Serialize(value);
            var originalSize = data.Length;

            // Compress if needed
            var compressed = false;
            if (_settings.EnableCompression && data.Length > _settings.CompressionThresholdBytes)
            {
                data = CompressionHelper.Compress(data);
                compressed = CompressionHelper.IsCompressed(data);
            }

            // Get expiration
            var expiration = options?.GetExpiration() ?? TimeSpan.FromSeconds(_settings.DefaultTtlSeconds);
            TimeSpan? finalExpiration = expiration == TimeSpan.Zero ? null : expiration;

            // Write to both levels in parallel
            var tasks = new List<Task>();

            if (_l1Provider != null)
            {
                tasks.Add(_l1Provider.SetAsync(key, data, finalExpiration, cancellationToken));
            }

            if (_l2Provider != null)
            {
                tasks.Add(_l2Provider.SetAsync(key, data, finalExpiration, cancellationToken));
            }

            await Task.WhenAll(tasks);

            // Update tag mappings
            if (options?.Tags != null && options.Tags.Count > 0)
            {
                await UpdateTagMappingsAsync(key, options.Tags, cancellationToken);
            }

            sw.Stop();
            _statistics.RecordOperationDuration("Set", sw.Elapsed.TotalMilliseconds);

            if (_settings.EnableVerboseLogging)
            {
                _logger.LogInformation(
                    "Cache set for key '{Key}' in {Duration}ms (Size: {Size} bytes, Compressed: {Compressed}, Tags: {Tags})",
                    key, sw.Elapsed.TotalMilliseconds, data.Length, compressed, options?.Tags.Count ?? 0);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error setting cache key '{Key}' after {Duration}ms", key, sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    private async Task UpdateTagMappingsAsync(string key, HashSet<string> tags, CancellationToken cancellationToken)
    {
        foreach (var tag in tags)
        {
            var tagKey = $"tag:{tag}";
            if (_l2Provider == null)
            {
                return;
            }

            var existingData = await _l2Provider.GetAsync(tagKey, cancellationToken);

            HashSet<string> keys;
            if (existingData != null)
            {
                keys = _serializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>();
            }
            else
            {
                keys = new HashSet<string>();
            }

            keys.Add(key);

            var tagData = _serializer.Serialize(keys);
            await _l2Provider.SetAsync(tagKey, tagData, null, cancellationToken);
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var tasks = new List<Task<bool>>();

            if (_l1Provider != null)
            {
                tasks.Add(_l1Provider.RemoveAsync(key, cancellationToken));
            }

            if (_l2Provider != null)
            {
                tasks.Add(_l2Provider.RemoveAsync(key, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);

            sw.Stop();
            _statistics.RecordOperationDuration("Remove", sw.Elapsed.TotalMilliseconds);

            var removed = results.Any(r => r);

            if (_settings.EnableVerboseLogging)
            {
                _logger.LogInformation("Cache remove for key '{Key}' in {Duration}ms (Removed: {Removed})",
                    key, sw.Elapsed.TotalMilliseconds, removed);
            }

            return removed;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error removing cache key '{Key}' after {Duration}ms", key, sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var l1Exists = _l1Provider != null && await _l1Provider.ExistsAsync(key, cancellationToken);
        if (l1Exists)
            return true;

        return _l2Provider != null && await _l2Provider.ExistsAsync(key, cancellationToken);
    }

    public async Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            if (_l2Provider == null)
            {
                return 0;
            }

            var keys = await _l2Provider.ScanKeysAsync(pattern, 10000, cancellationToken);
            var removeTasks = keys.Select(key => RemoveAsync(key, cancellationToken));
            var results = await Task.WhenAll(removeTasks);

            var count = results.Count(r => r);

            sw.Stop();
            _statistics.RecordOperationDuration("RemoveByPattern", sw.Elapsed.TotalMilliseconds);

            _logger.LogInformation("Cache remove by pattern '{Pattern}' removed {Count} keys in {Duration}ms",
                pattern, count, sw.Elapsed.TotalMilliseconds);

            return count;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error removing cache by pattern '{Pattern}' after {Duration}ms", pattern, sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    public async Task<int> RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var tagKey = $"tag:{tag}";
            if (_l2Provider == null)
            {
                return 0;
            }

            var tagData = await _l2Provider.GetAsync(tagKey, cancellationToken);

            if (tagData == null)
            {
                return 0;
            }

            var keys = _serializer.Deserialize<HashSet<string>>(tagData) ?? new HashSet<string>();

            var removeTasks = keys.Select(key => RemoveAsync(key, cancellationToken)).ToList();
            removeTasks.Add(_l2Provider.RemoveAsync(tagKey, cancellationToken));

            var results = await Task.WhenAll(removeTasks);
            var count = results.Count(r => r) - 1; // Exclude tag key itself

            sw.Stop();
            _statistics.RecordOperationDuration("RemoveByTag", sw.Elapsed.TotalMilliseconds);

            _logger.LogInformation("Cache remove by tag '{Tag}' removed {Count} keys in {Duration}ms",
                tag, count, sw.Elapsed.TotalMilliseconds);

            return count;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error removing cache by tag '{Tag}' after {Duration}ms", tag, sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    public Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_statistics);
    }

    public async Task<WarmupResult> WarmupAsync(IEnumerable<KeyValuePair<string, object>> entries, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new WarmupResult();
        int successCount = 0;
        int failureCount = 0;

        var semaphore = new SemaphoreSlim(_settings.WarmupConcurrencyLimit);
        var tasks = new List<Task>();

        foreach (var entry in entries)
        {
            await semaphore.WaitAsync(cancellationToken);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await SetAsync(entry.Key, entry.Value, null, cancellationToken);
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failureCount);
                    result.FailedKeys.Add(entry.Key);
                    _logger.LogError(ex, "Failed to warm up cache key '{Key}'", entry.Key);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        sw.Stop();
        result.SuccessCount = successCount;
        result.FailureCount = failureCount;
        result.Duration = sw.Elapsed;

        _logger.LogInformation("Cache warmup completed: {Success} succeeded, {Failed} failed in {Duration}ms",
            result.SuccessCount, result.FailureCount, sw.Elapsed.TotalMilliseconds);

        return result;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var testKey = $"health:check:{Guid.NewGuid()}";
            var testValue = "health-check-value";

            await SetAsync(testKey, testValue, null, cancellationToken);
            var retrievedValue = await GetAsync<string>(testKey, cancellationToken);
            await RemoveAsync(testKey, cancellationToken);

            sw.Stop();

            var isHealthy = retrievedValue == testValue;

            return new HealthCheckResult
            {
                IsHealthy = isHealthy,
                ProviderName = "MultiLevel",
                ResponseTimeMs = sw.Elapsed.TotalMilliseconds,
                ErrorMessage = isHealthy ? null : "Health check value mismatch"
            };
        }
        catch (Exception ex)
        {
            sw.Stop();

            return new HealthCheckResult
            {
                IsHealthy = false,
                ProviderName = "MultiLevel",
                ResponseTimeMs = sw.Elapsed.TotalMilliseconds,
                ErrorMessage = ex.Message
            };
        }
    }
}
