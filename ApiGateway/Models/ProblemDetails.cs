using System.Text.Json.Serialization;

namespace ApiGateway.Models;

/// <summary>
/// RFC 7807 Problem Details for HTTP APIs
/// </summary>
public class ProblemDetails
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "about:blank";
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public int Status { get; set; }
    
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
    
    [JsonPropertyName("instance")]
    public string? Instance { get; set; }
    
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
    
    [JsonPropertyName("extensions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Extensions { get; set; }
}
