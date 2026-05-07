using System.Text;
using System.Text.Json;
using ApiGateway.Configuration;

namespace ApiGateway.Transformers;

public class JsonTransformer : IRequestTransformer, IResponseTransformer
{
    private readonly ILogger<JsonTransformer> _logger;
    
    public JsonTransformer(ILogger<JsonTransformer> logger)
    {
        _logger = logger;
    }
    
    public async Task<HttpRequestMessage> TransformAsync(
        HttpRequestMessage request,
        TransformationRules rules,
        CancellationToken cancellationToken = default)
    {
        // Transform headers
        foreach (var mapping in rules.RequestHeaderMappings)
        {
            if (request.Headers.TryGetValues(mapping.Key, out var values))
            {
                request.Headers.Remove(mapping.Key);
                request.Headers.TryAddWithoutValidation(mapping.Value, values);
                _logger.LogDebug("Transformed request header {OldName} to {NewName}", mapping.Key, mapping.Value);
            }
        }
        
        return request;
    }
    
    public async Task<HttpResponseMessage> TransformAsync(
        HttpResponseMessage response,
        TransformationRules rules,
        CancellationToken cancellationToken = default)
    {
        if (response.Content == null)
            return response;
        
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType != "application/json")
            return response;
        
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(content))
                return response;
            
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;
            
            var transformed = TransformJsonElement(root, rules);
            
            var newContent = JsonSerializer.Serialize(transformed);
            response.Content = new StringContent(newContent, Encoding.UTF8, "application/json");
            
            _logger.LogDebug("Transformed response JSON");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transform response JSON, returning original response");
        }
        
        return response;
    }
    
    private object TransformJsonElement(JsonElement element, TransformationRules rules)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var result = new Dictionary<string, object>();
            
            foreach (var property in element.EnumerateObject())
            {
                // Skip fields to remove
                if (rules.ResponseFieldsToRemove.Contains(property.Name))
                {
                    _logger.LogDebug("Removed field {FieldName}", property.Name);
                    continue;
                }
                
                // Apply field mappings
                var fieldName = rules.ResponseFieldMappings.TryGetValue(property.Name, out var mapped)
                    ? mapped
                    : property.Name;
                
                if (fieldName != property.Name)
                {
                    _logger.LogDebug("Renamed field {OldName} to {NewName}", property.Name, fieldName);
                }
                
                result[fieldName] = TransformJsonElement(property.Value, rules);
            }
            
            // Add additional fields
            foreach (var field in rules.ResponseFieldsToAdd)
            {
                result[field.Key] = field.Value;
                _logger.LogDebug("Added field {FieldName}", field.Key);
            }
            
            return result;
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray()
                .Select(e => TransformJsonElement(e, rules))
                .ToList();
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? string.Empty;
        }
        else if (element.ValueKind == JsonValueKind.Number)
        {
            if (element.TryGetInt32(out var intValue))
                return intValue;
            if (element.TryGetInt64(out var longValue))
                return longValue;
            if (element.TryGetDouble(out var doubleValue))
                return doubleValue;
            return element.GetRawText();
        }
        else if (element.ValueKind == JsonValueKind.True)
        {
            return true;
        }
        else if (element.ValueKind == JsonValueKind.False)
        {
            return false;
        }
        else if (element.ValueKind == JsonValueKind.Null)
        {
            return null!;
        }
        else
        {
            return element.GetRawText();
        }
    }
}
