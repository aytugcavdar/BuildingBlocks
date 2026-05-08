using ApiGateway.Configuration;
using ApiGateway.Transformers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ApiGateway.Tests.Transformers;

public class JsonTransformerTests
{
    private readonly ILogger<JsonTransformer> _logger;
    private readonly JsonTransformer _transformer;

    public JsonTransformerTests()
    {
        _logger = Substitute.For<ILogger<JsonTransformer>>();
        _transformer = new JsonTransformer(_logger);
    }

    [Fact]
    public async Task TransformAsync_Response_ShouldRenameFields()
    {
        // Arrange
        var response = CreateJsonResponse(new { user_id = 123, user_name = "John" });
        var rules = new TransformationRules
        {
            ResponseFieldMappings = new Dictionary<string, string>
            {
                ["user_id"] = "userId",
                ["user_name"] = "userName"
            }
        };

        // Act
        var result = await _transformer.TransformAsync(response, rules);
        var content = await result.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert
        json.RootElement.TryGetProperty("userId", out var userId).Should().BeTrue();
        userId.GetInt32().Should().Be(123);
        json.RootElement.TryGetProperty("userName", out var userName).Should().BeTrue();
        userName.GetString().Should().Be("John");
        json.RootElement.TryGetProperty("user_id", out _).Should().BeFalse();
        json.RootElement.TryGetProperty("user_name", out _).Should().BeFalse();
    }

    [Fact]
    public async Task TransformAsync_Response_ShouldRemoveFields()
    {
        // Arrange
        var response = CreateJsonResponse(new { id = 1, password = "secret", email = "test@test.com" });
        var rules = new TransformationRules
        {
            ResponseFieldsToRemove = new List<string> { "password" }
        };

        // Act
        var result = await _transformer.TransformAsync(response, rules);
        var content = await result.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert
        json.RootElement.TryGetProperty("id", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("email", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("password", out _).Should().BeFalse();
    }

    [Fact]
    public async Task TransformAsync_Response_ShouldAddFields()
    {
        // Arrange
        var response = CreateJsonResponse(new { id = 1, name = "Test" });
        var rules = new TransformationRules
        {
            ResponseFieldsToAdd = new Dictionary<string, object>
            {
                ["version"] = "1.0",
                ["timestamp"] = 1234567890
            }
        };

        // Act
        var result = await _transformer.TransformAsync(response, rules);
        var content = await result.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert
        json.RootElement.TryGetProperty("id", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("name", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("version", out var version).Should().BeTrue();
        version.GetString().Should().Be("1.0");
        json.RootElement.TryGetProperty("timestamp", out var timestamp).Should().BeTrue();
        timestamp.GetInt32().Should().Be(1234567890);
    }

    [Fact]
    public async Task TransformAsync_Response_ShouldTransformNestedObjects()
    {
        // Arrange
        var response = CreateJsonResponse(new
        {
            user_id = 1,
            profile = new
            {
                first_name = "John",
                last_name = "Doe"
            }
        });
        var rules = new TransformationRules
        {
            ResponseFieldMappings = new Dictionary<string, string>
            {
                ["user_id"] = "userId",
                ["first_name"] = "firstName",
                ["last_name"] = "lastName"
            }
        };

        // Act
        var result = await _transformer.TransformAsync(response, rules);
        var content = await result.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert
        json.RootElement.TryGetProperty("userId", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("profile", out var profile).Should().BeTrue();
        profile.TryGetProperty("firstName", out var firstName).Should().BeTrue();
        firstName.GetString().Should().Be("John");
        profile.TryGetProperty("lastName", out var lastName).Should().BeTrue();
        lastName.GetString().Should().Be("Doe");
    }

    [Fact]
    public async Task TransformAsync_Response_ShouldTransformArrays()
    {
        // Arrange
        var response = CreateJsonResponse(new
        {
            users = new[]
            {
                new { user_id = 1, user_name = "John" },
                new { user_id = 2, user_name = "Jane" }
            }
        });
        var rules = new TransformationRules
        {
            ResponseFieldMappings = new Dictionary<string, string>
            {
                ["user_id"] = "userId",
                ["user_name"] = "userName"
            }
        };

        // Act
        var result = await _transformer.TransformAsync(response, rules);
        var content = await result.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert
        json.RootElement.TryGetProperty("users", out var users).Should().BeTrue();
        users.GetArrayLength().Should().Be(2);
        
        var firstUser = users[0];
        firstUser.TryGetProperty("userId", out var userId1).Should().BeTrue();
        userId1.GetInt32().Should().Be(1);
        firstUser.TryGetProperty("userName", out var userName1).Should().BeTrue();
        userName1.GetString().Should().Be("John");
    }

    [Fact]
    public async Task TransformAsync_Response_ShouldNotTransform_WhenContentTypeIsNotJson()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Plain text content", Encoding.UTF8, "text/plain")
        };
        var rules = new TransformationRules
        {
            ResponseFieldMappings = new Dictionary<string, string> { ["test"] = "transformed" }
        };

        // Act
        var result = await _transformer.TransformAsync(response, rules);
        var content = await result.Content.ReadAsStringAsync();

        // Assert
        content.Should().Be("Plain text content");
    }

    [Fact]
    public async Task TransformAsync_Response_ShouldNotThrow_WhenBodyIsEmpty()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };
        var rules = new TransformationRules();

        // Act
        var act = async () => await _transformer.TransformAsync(response, rules);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TransformAsync_Request_ShouldTransformHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://test.com");
        request.Headers.TryAddWithoutValidation("X-Old-Header", "value1");
        request.Headers.TryAddWithoutValidation("X-Another-Header", "value2");

        var rules = new TransformationRules
        {
            RequestHeaderMappings = new Dictionary<string, string>
            {
                ["X-Old-Header"] = "X-New-Header"
            }
        };

        // Act
        var result = await _transformer.TransformAsync(request, rules);

        // Assert
        result.Headers.Contains("X-New-Header").Should().BeTrue();
        result.Headers.GetValues("X-New-Header").First().Should().Be("value1");
        result.Headers.Contains("X-Old-Header").Should().BeFalse();
        result.Headers.Contains("X-Another-Header").Should().BeTrue();
    }

    [Fact]
    public async Task TransformAsync_Response_ShouldHandleNullValues()
    {
        // Arrange
        var jsonString = "{\"id\":1,\"name\":null,\"active\":true}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonString, Encoding.UTF8, "application/json")
        };
        var rules = new TransformationRules
        {
            ResponseFieldMappings = new Dictionary<string, string> { ["id"] = "userId" }
        };

        // Act
        var result = await _transformer.TransformAsync(response, rules);
        var content = await result.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert
        json.RootElement.TryGetProperty("userId", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("name", out var name).Should().BeTrue();
        name.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task TransformAsync_Response_ShouldReturnOriginal_WhenJsonIsInvalid()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ invalid json", Encoding.UTF8, "application/json")
        };
        var rules = new TransformationRules
        {
            ResponseFieldMappings = new Dictionary<string, string> { ["test"] = "transformed" }
        };

        // Act
        var result = await _transformer.TransformAsync(response, rules);
        var content = await result.Content.ReadAsStringAsync();

        // Assert
        content.Should().Be("{ invalid json");
    }

    [Fact]
    public async Task TransformAsync_Response_ShouldHandleComplexNestedStructures()
    {
        // Arrange
        var response = CreateJsonResponse(new
        {
            user_id = 1,
            metadata = new
            {
                created_at = "2024-01-01",
                tags = new[] { "tag1", "tag2" },
                settings = new
                {
                    theme_color = "blue",
                    notifications_enabled = true
                }
            }
        });
        var rules = new TransformationRules
        {
            ResponseFieldMappings = new Dictionary<string, string>
            {
                ["user_id"] = "userId",
                ["created_at"] = "createdAt",
                ["theme_color"] = "themeColor",
                ["notifications_enabled"] = "notificationsEnabled"
            }
        };

        // Act
        var result = await _transformer.TransformAsync(response, rules);
        var content = await result.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert
        json.RootElement.TryGetProperty("userId", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("metadata", out var metadata).Should().BeTrue();
        metadata.TryGetProperty("createdAt", out _).Should().BeTrue();
        metadata.TryGetProperty("settings", out var settings).Should().BeTrue();
        settings.TryGetProperty("themeColor", out _).Should().BeTrue();
        settings.TryGetProperty("notificationsEnabled", out _).Should().BeTrue();
    }

    private HttpResponseMessage CreateJsonResponse(object data)
    {
        var json = JsonSerializer.Serialize(data);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}
