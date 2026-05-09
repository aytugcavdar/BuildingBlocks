using System.IO.Compression;

namespace BuildingBlocks.CrossCutting.Caching.Core;

/// <summary>
/// Helper class for compressing and decompressing cache data using GZip.
/// </summary>
public static class CompressionHelper
{
    private static readonly byte[] Header = "BBGZ1"u8.ToArray();

    /// <summary>
    /// Compresses data using GZip. Returns original data if compression increases size.
    /// </summary>
    public static byte[] Compress(byte[] data)
    {
        if (data.Length == 0)
        {
            return data;
        }

        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Fastest))
        {
            gzipStream.Write(data, 0, data.Length);
        }

        var compressedData = outputStream.ToArray();

        if (compressedData.Length + Header.Length >= data.Length)
        {
            return data;
        }

        var result = new byte[Header.Length + compressedData.Length];
        Buffer.BlockCopy(Header, 0, result, 0, Header.Length);
        Buffer.BlockCopy(compressedData, 0, result, Header.Length, compressedData.Length);
        return result;
    }

    /// <summary>
    /// Decompresses data compressed by <see cref="Compress"/>. Unmarked data is returned unchanged.
    /// </summary>
    public static byte[] Decompress(byte[] data)
    {
        if (!IsCompressed(data))
        {
            return data;
        }

        using var inputStream = new MemoryStream(data, Header.Length, data.Length - Header.Length);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        
        gzipStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    public static bool IsCompressed(byte[] data)
    {
        if (data.Length < Header.Length)
        {
            return false;
        }

        for (var i = 0; i < Header.Length; i++)
        {
            if (data[i] != Header[i])
            {
                return false;
            }
        }

        return true;
    }
}
