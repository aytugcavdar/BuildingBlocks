namespace ApiGateway.Exceptions;

public class DownstreamServiceException : Exception
{
    public string ServiceName { get; }
    public int? StatusCode { get; }
    
    public DownstreamServiceException(string serviceName, string message) 
        : base(message)
    {
        ServiceName = serviceName;
    }
    
    public DownstreamServiceException(string serviceName, string message, int statusCode) 
        : base(message)
    {
        ServiceName = serviceName;
        StatusCode = statusCode;
    }
    
    public DownstreamServiceException(string serviceName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }
}
