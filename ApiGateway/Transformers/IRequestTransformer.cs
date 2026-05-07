using ApiGateway.Configuration;

namespace ApiGateway.Transformers;

public interface IRequestTransformer
{
    Task<HttpRequestMessage> TransformAsync(
        HttpRequestMessage request,
        TransformationRules rules,
        CancellationToken cancellationToken = default);
}
