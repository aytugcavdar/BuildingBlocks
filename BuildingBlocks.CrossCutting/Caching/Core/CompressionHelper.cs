using System.IO.Compression;

namespace BuildingBlocks.CrossCutting.Caching.Core;

/// <summary>
/// Helper class for compressing and decompressing cache data using GZip.
/// </summary>
public static class CompressionHelper
{
    /// <summary>
    /// Compresses data using GZip. Returns original data if compression increases size.
    /// </summary>
    public static byte[] Compress(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return data;
        }

        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Fastest))
        {
            gzipStream.Write(data, 0, data.Length);
        }

        var compressedData = outputStream.ToArray();

        // Return original if compression doesn't reduce size
        return compressedData.Length < data.Length ? compressedData : data;
    }

    /// <summary>
    /// Decompresses GZip-compressed data.
    /// </summary>
    public static byte[] Decompress(byte[] compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
        {
            return compressedData;
        }

        using var inputStream = new MemoryStream(compressedData);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        
        gzipStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }
}
