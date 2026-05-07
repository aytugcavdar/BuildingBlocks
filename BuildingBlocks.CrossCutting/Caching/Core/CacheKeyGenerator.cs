using System.Text;
using System.Text.RegularExpressions;

namespace BuildingBlocks.CrossCutting.Caching.Core;

/// <summary>
/// Generates and sanitizes cache keys with namespace support.
/// </summary>
public class CacheKeyGenerator
{
    private const int MaxKeyLength = 512;
    private static readonly Regex SanitizeRegex = new(@"[^a-zA-Z0-9\-_\.]", RegexOptions.Compiled);

    /// <summary>
    /// Generates a cache key from namespace and identifiers.
    /// </summary>
    /// <param name="namespace">The namespace for the key (defaults to "default" if empty).</param>
    /// <param name="identifiers">The identifiers to include in the key.</param>
    /// <returns>A sanitized cache key in format "{namespace}:{identifier1}:{identifier2}..."</returns>
    public string Generate(string @namespace, params object[] identifiers)
    {
        if (string.IsNullOrWhiteSpace(@namespace))
        {
            @namespace = "default";
        }

        var keyBuilder = new StringBuilder();
        keyBuilder.Append(Sanitize(@namespace));

        foreach (var identifier in identifiers)
        {
            if (identifier != null)
            {
                keyBuilder.Append(':');
                keyBuilder.Append(Sanitize(identifier.ToString() ?? string.Empty));
            }
        }

        var key = keyBuilder.ToString();

        if (key.Length > MaxKeyLength)
        {
            throw new ArgumentException(
                $"Generated cache key exceeds maximum length of {MaxKeyLength} characters. Key: {key.Substring(0, Math.Min(100, key.Length))}...",
                nameof(identifiers));
        }

        return key;
    }

    /// <summary>
    /// Sanitizes a string by removing special characters.
    /// Keeps only alphanumeric characters, dashes, underscores, and dots.
    /// </summary>
    private string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return SanitizeRegex.Replace(input, string.Empty);
    }
}
