namespace BuildingBlocks.CrossCutting.Caching.Exceptions;

/// <summary>
/// Exception thrown when a cache provider operation fails.
/// </summary>
public class CacheProviderException : Exception
{
    public string ProviderName { get; }

    public CacheProviderException(string providerName, string message)
        : base($"[{providerName}] {message}")
    {
        ProviderName = providerName;
    }

    public CacheProviderException(string providerName, string message, Exception innerException)
        : base($"[{providerName}] {message}", innerException)
    {
        ProviderName = providerName;
    }
}
