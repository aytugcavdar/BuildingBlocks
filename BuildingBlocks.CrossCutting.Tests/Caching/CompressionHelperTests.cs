using System.Text;
using BuildingBlocks.CrossCutting.Caching.Core;
using FluentAssertions;

namespace BuildingBlocks.CrossCutting.Tests.Caching;

public class CompressionHelperTests
{
    [Fact]
    public void Compress_ShouldMarkCompressedData_WhenCompressionReducesSize()
    {
        var data = Encoding.UTF8.GetBytes(new string('a', 4096));

        var compressed = CompressionHelper.Compress(data);

        CompressionHelper.IsCompressed(compressed).Should().BeTrue();
        compressed.Length.Should().BeLessThan(data.Length);
    }

    [Fact]
    public void Decompress_ShouldRoundTripCompressedData()
    {
        var data = Encoding.UTF8.GetBytes(new string('b', 4096));
        var compressed = CompressionHelper.Compress(data);

        var decompressed = CompressionHelper.Decompress(compressed);

        decompressed.Should().Equal(data);
    }

    [Fact]
    public void Decompress_ShouldReturnUnmarkedDataUnchanged()
    {
        var data = Encoding.UTF8.GetBytes("plain cache payload");

        var decompressed = CompressionHelper.Decompress(data);

        decompressed.Should().Equal(data);
        CompressionHelper.IsCompressed(decompressed).Should().BeFalse();
    }
}
