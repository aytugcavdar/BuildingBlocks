namespace BuildingBlocks.CrossCutting.Caching.Exceptions;

/// <summary>
/// Exception thrown when cache configuration is invalid.
/// </summary>
public class ConfigurationException : Exception
{
    public string ConfigurationKey { get; }

    public ConfigurationException(string configurationKey, string message)
        : base($"Configuration error for '{configurationKey}': {message}")
    {
        ConfigurationKey = configurationKey;
    }

    public ConfigurationException(string configurationKey, string message, Exception innerException)
        : base($"Configuration error for '{configurationKey}': {message}", innerException)
    {
        ConfigurationKey = configurationKey;
    }
}
