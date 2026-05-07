namespace ApiGateway.Exceptions;

public class AggregationException : Exception
{
    public Dictionary<string, string> ServiceErrors { get; }
    
    public AggregationException(string message, Dictionary<string, string> serviceErrors) 
        : base(message)
    {
        ServiceErrors = serviceErrors;
    }
    
    public AggregationException(string message, Dictionary<string, string> serviceErrors, Exception innerException) 
        : base(message, innerException)
    {
        ServiceErrors = serviceErrors;
    }
}
