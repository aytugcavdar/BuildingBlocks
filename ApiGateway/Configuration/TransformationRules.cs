namespace ApiGateway.Configuration;

public class TransformationRules
{
    public Dictionary<string, string> RequestHeaderMappings { get; set; } = new();
    public Dictionary<string, string> ResponseFieldMappings { get; set; } = new();
    public List<string> ResponseFieldsToRemove { get; set; } = new();
    public Dictionary<string, object> ResponseFieldsToAdd { get; set; } = new();
}
