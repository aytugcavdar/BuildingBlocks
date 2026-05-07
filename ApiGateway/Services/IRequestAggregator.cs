namespace ApiGateway.Services;

public interface IRequestAggregator
{
    Task<AggregatedResponse> AggregateAsync(
        AggregationRequest request,
        CancellationToken cancellationToken = default);
}
