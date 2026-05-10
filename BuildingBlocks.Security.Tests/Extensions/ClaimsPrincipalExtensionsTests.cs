using System.Security.Claims;
using BuildingBlocks.Security.Extensions;
using FluentAssertions;

namespace BuildingBlocks.Security.Tests.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void ClaimExtensions_ShouldAddStandardUserClaims()
    {
        var claims = new List<Claim>();

        claims.AddNameIdentifier("user-1");
        claims.AddEmail("user@example.com");
        claims.AddName("Test User");
        claims.AddRoles(["admin", "editor"]);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        principal.GetUserId().Should().Be("user-1");
        principal.GetUserEmail().Should().Be("user@example.com");
        principal.GetUserName().Should().Be("Test User");
        principal.GetUserRoles().Should().BeEquivalentTo("admin", "editor");
    }

    [Fact]
    public void GetClaim_ShouldReturnNull_WhenClaimDoesNotExist()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        principal.GetClaim(ClaimTypes.Email).Should().BeNull();
    }
}
