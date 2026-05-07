namespace BuildingBlocks.CrossCutting.Caching.Models;

/// <summary>
/// Cache statistics including hit rate and operation metrics.
/// </summary>
public class CacheStatistics
{
    private long _totalHits;
    private long _totalMisses;
    private long _l1Hits;
    private long _l2Hits;
    private readonly Dictionary<string, List<double>> _operationDurations = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets the total number of cache hits.
    /// </summary>
    public long TotalHits => Interlocked.Read(ref _totalHits);

    /// <summary>
    /// Gets the total number of cache misses.
    /// </summary>
    public long TotalMisses => Interlocked.Read(ref _totalMisses);

    /// <summary>
    /// Gets the number of L1 (memory) cache hits.
    /// </summary>
    public long L1Hits => Interlocked.Read(ref _l1Hits);

    /// <summary>
    /// Gets the number of L2 (Redis) cache hits.
    /// </summary>
    public long L2Hits => Interlocked.Read(ref _l2Hits);

    /// <summary>
    /// Gets the cache hit rate (0.0 to 1.0).
    /// </summary>
    public double HitRate
    {
        get
        {
            var total = TotalHits + TotalMisses;
            return total == 0 ? 0.0 : (double)TotalHits / total;
        }
    }

    /// <summary>
    /// Gets the average operation durations by operation type.
    /// </summary>
    public IReadOnlyDictionary<string, double> AverageOperationDurations
    {
        get
        {
            lock (_lock)
            {
                return _operationDurations
                    .Where(kvp => kvp.Value.Count > 0)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Average());
            }
        }
    }

    /// <summary>
    /// Records a cache hit.
    /// </summary>
    public void RecordHit() => Interlocked.Increment(ref _totalHits);

    /// <summary>
    /// Records a cache miss.
    /// </summary>
    public void RecordMiss() => Interlocked.Increment(ref _totalMisses);

    /// <summary>
    /// Records an L1 cache hit.
    /// </summary>
    public void RecordL1Hit() => Interlocked.Increment(ref _l1Hits);

    /// <summary>
    /// Records an L2 cache hit.
    /// </summary>
    public void RecordL2Hit() => Interlocked.Increment(ref _l2Hits);

    /// <summary>
    /// Records an operation duration.
    /// </summary>
    public void RecordOperationDuration(string operation, double durationMs)
    {
        lock (_lock)
        {
            if (!_operationDurations.ContainsKey(operation))
            {
                _operationDurations[operation] = new List<double>();
            }
            _operationDurations[operation].Add(durationMs);
        }
    }
}
