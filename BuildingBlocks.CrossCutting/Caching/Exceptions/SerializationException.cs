namespace BuildingBlocks.CrossCutting.Caching.Exceptions;

/// <summary>
/// Exception thrown when serialization or deserialization fails.
/// </summary>
public class SerializationException : Exception
{
    public Type? TargetType { get; }

    public SerializationException(string message)
        : base(message)
    {
    }

    public SerializationException(string message, Type targetType)
        : base($"{message} (Type: {targetType.FullName})")
    {
        TargetType = targetType;
    }

    public SerializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SerializationException(string message, Type targetType, Exception innerException)
        : base($"{message} (Type: {targetType.FullName})", innerException)
    {
        TargetType = targetType;
    }
}
