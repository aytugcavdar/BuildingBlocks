using System.Diagnostics;
using System.Text.Json;
using ApiGateway.Observability;

namespace ApiGateway.Services;

public class RequestAggregator : IRequestAggregator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RequestAggregator> _logger;
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly GatewayMetrics _metrics;
    
    public RequestAggregator(
        IHttpClientFactory httpClientFactory,
        ILogger<RequestAggregator> logger,
        IServiceDiscovery serviceDiscovery,
        GatewayMetrics metrics)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _serviceDiscovery = serviceDiscovery;
        _metrics = metrics;
    }
    
    public async Task<AggregatedResponse> AggregateAsync(
        AggregationRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var responses = new Dictionary<string, ServiceResponse>();
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));
        
        var tasks = request.Requests.Select(async req =>
        {
            var serviceResponse = await ExecuteDownstreamRequestAsync(req, cts.Token);
            return (req.ServiceName, serviceResponse);
        });
        
        var results = await Task.WhenAll(tasks);
        
        foreach (var (serviceName, response) in results)
        {
            responses[serviceName] = response;
        }
        
        sw.Stop();
        
        var hasErrors = responses.Values.Any(r => !r.Success);
        
        _logger.LogInformation(
            "Aggregation completed in {Duration}ms with {SuccessCount}/{TotalCount} successful calls",
            sw.ElapsedMilliseconds,
            responses.Values.Count(r => r.Success),
            responses.Count);
        
        return new AggregatedResponse
        {
            Responses = responses,
            TotalDuration = sw.Elapsed,
            HasErrors = hasErrors
        };
    }
    
    private async Task<ServiceResponse> ExecuteDownstreamRequestAsync(
        DownstreamRequest request,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        
        // Create custom span for downstream call
        using var activity = GatewayActivitySource.StartActivity(
            $"downstream_call_{request.ServiceName}",
            ActivityKind.Client);
        
        activity?.AddGatewayTags(
            serviceName: request.ServiceName,
            httpMethod: request.Method.Method,
            path: request.Path);
        
        try
        {
            var serviceUrl = await _serviceDiscovery.ResolveServiceUrlAsync(
                request.ServiceName, cancellationToken);
            
            var client = _httpClientFactory.CreateClient("downstream");
            var httpRequest = new HttpRequestMessage(request.Method, $"{serviceUrl}{request.Path}");
            
            foreach (var header in request.Headers)
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            
            if (request.Body != null)
            {
                httpRequest.Content = JsonContent.Create(request.Body);
            }
            
            var response = await client.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            sw.Stop();
            
            var statusCode = (int)response.StatusCode;
            
            // Add status code to span
            activity?.AddGatewayTags(statusCode: statusCode);
            
            // Record downstream call metric
            _metrics.RecordDownstreamCall(
                serviceName: request.ServiceName,
                method: request.Method.Method,
                statusCode: statusCode);
            
            object? data = null;
            if (!string.IsNullOrEmpty(content))
            {
                try
                {
                    data = JsonSerializer.Deserialize<object>(content);
                }
                catch
                {
                    data = content;
                }
            }
            
            return new ServiceResponse
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = statusCode,
                Data = data,
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Downstream request to {ServiceName} failed", request.ServiceName);
            
            // Record error in span
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddGatewayTags(statusCode: 500);
            
            // Record downstream call metric with error status
            _metrics.RecordDownstreamCall(
                serviceName: request.ServiceName,
                method: request.Method.Method,
                statusCode: 500);
            
            return new ServiceResponse
            {
                Success = false,
                StatusCode = 500,
                Error = ex.Message,
                Duration = sw.Elapsed
            };
        }
    }
}
