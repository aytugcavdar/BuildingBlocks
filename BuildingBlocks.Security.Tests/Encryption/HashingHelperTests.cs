using BuildingBlocks.Security.Encryption;
using FluentAssertions;

namespace BuildingBlocks.Security.Tests.Encryption;

public class HashingHelperTests
{
    [Fact]
    public void VerifyPasswordHash_ShouldReturnTrue_WhenPasswordMatches()
    {
        HashingHelper.CreatePasswordHash("correct-password", out var hash, out var salt);

        var result = HashingHelper.VerifyPasswordHash("correct-password", hash, salt);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPasswordHash_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        HashingHelper.CreatePasswordHash("correct-password", out var hash, out var salt);

        var result = HashingHelper.VerifyPasswordHash("wrong-password", hash, salt);

        result.Should().BeFalse();
    }
}
