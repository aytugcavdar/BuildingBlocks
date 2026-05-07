using System.Text;
using System.Text.Json;
using BuildingBlocks.CrossCutting.Caching.Exceptions;

namespace BuildingBlocks.CrossCutting.Caching.Core;

/// <summary>
/// JSON serializer for cache values using System.Text.Json.
/// </summary>
public class JsonCacheSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    /// <summary>
    /// Serializes a value to JSON bytes.
    /// </summary>
    public byte[] Serialize<T>(T value)
    {
        try
        {
            if (value == null)
            {
                return Array.Empty<byte>();
            }

            var json = JsonSerializer.Serialize(value, Options);
            return Encoding.UTF8.GetBytes(json);
        }
        catch (Exception ex)
        {
            throw new SerializationException(
                "Failed to serialize value to JSON",
                typeof(T),
                ex);
        }
    }

    /// <summary>
    /// Deserializes JSON bytes to a value.
    /// </summary>
    public T? Deserialize<T>(byte[] data)
    {
        try
        {
            if (data == null || data.Length == 0)
            {
                return default;
            }

            var json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (Exception ex)
        {
            throw new SerializationException(
                "Failed to deserialize JSON to value",
                typeof(T),
                ex);
        }
    }
}
