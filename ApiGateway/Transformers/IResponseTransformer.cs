using ApiGateway.Configuration;

namespace ApiGateway.Transformers;

public interface IResponseTransformer
{
    Task<HttpResponseMessage> TransformAsync(
        HttpResponseMessage response,
        TransformationRules rules,
        CancellationToken cancellationToken = default);
}
