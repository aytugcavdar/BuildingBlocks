using BuildingBlocks.Security.Extensions;
using BuildingBlocks.Security.JWT;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Security.Tests.JWT;

public class JwtHelperTests
{
    [Fact]
    public void CreateToken_ShouldUseBuildingBlocksTokenOptions()
    {
        var helper = new JwtHelper(CreateConfiguration("BuildingBlocks:Security:TokenOptions"));
        var userId = Guid.NewGuid();

        var token = helper.CreateToken(userId, "user@example.com", "Test User", ["admin"]);
        var principal = helper.ValidateToken(token.Token);

        principal.Should().NotBeNull();
        var validatedPrincipal = principal!;

        validatedPrincipal.GetUserId().Should().Be(userId.ToString());
        validatedPrincipal.GetUserEmail().Should().Be("user@example.com");
        validatedPrincipal.GetUserName().Should().Be("Test User");
        validatedPrincipal.GetUserRoles().Should().Contain("admin");
        token.Expiration.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void CreateToken_ShouldFallbackToLegacyTokenOptions()
    {
        var helper = new JwtHelper(CreateConfiguration("TokenOptions"));

        var token = helper.CreateToken(Guid.NewGuid(), "user@example.com", "Test User", []);

        helper.ValidateToken(token.Token).Should().NotBeNull();
    }

    [Fact]
    public void CreateRefreshToken_ShouldReturnRandomBase64Token()
    {
        var helper = new JwtHelper(CreateConfiguration("TokenOptions"));

        var first = helper.CreateRefreshToken();
        var second = helper.CreateRefreshToken();

        first.Should().NotBeNullOrWhiteSpace();
        second.Should().NotBeNullOrWhiteSpace();
        first.Should().NotBe(second);
        Convert.FromBase64String(first).Should().HaveCount(32);
    }

    [Fact]
    public void ValidateToken_ShouldReturnNull_WhenTokenIsInvalid()
    {
        var helper = new JwtHelper(CreateConfiguration("TokenOptions"));

        var principal = helper.ValidateToken("not-a-token");

        principal.Should().BeNull();
    }

    private static IConfiguration CreateConfiguration(string sectionName)
    {
        var values = new Dictionary<string, string?>
        {
            [$"{sectionName}:Audience"] = "test-audience",
            [$"{sectionName}:Issuer"] = "test-issuer",
            [$"{sectionName}:AccessTokenExpiration"] = "15",
            [$"{sectionName}:RefreshTokenExpiration"] = "60",
            [$"{sectionName}:SecurityKey"] = "test-security-key-with-at-least-64-characters-for-hs512-signing-1234567890"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
