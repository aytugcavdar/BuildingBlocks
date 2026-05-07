namespace ApiGateway.Exceptions;

public class TransformationException : Exception
{
    public string TransformationType { get; }
    
    public TransformationException(string transformationType, string message) 
        : base(message)
    {
        TransformationType = transformationType;
    }
    
    public TransformationException(string transformationType, string message, Exception innerException) 
        : base(message, innerException)
    {
        TransformationType = transformationType;
    }
}
