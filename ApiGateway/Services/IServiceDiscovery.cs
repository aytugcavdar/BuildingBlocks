namespace ApiGateway.Services;

public interface IServiceDiscovery
{
    Task<string> ResolveServiceUrlAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<List<ServiceInstance>> GetServiceInstancesAsync(string serviceName, CancellationToken cancellationToken = default);
}
