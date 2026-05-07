namespace ApiGateway.Exceptions;

public class GatewayConfigurationException : Exception
{
    public GatewayConfigurationException(string message) : base(message)
    {
    }
    
    public GatewayConfigurationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
