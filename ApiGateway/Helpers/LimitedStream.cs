namespace ApiGateway.Helpers;

public class LimitedStream : Stream
{
    private readonly Stream _innerStream;
    private readonly long _maxSize;
    private long _bytesRead;
    
    public LimitedStream(Stream innerStream, long maxSize)
    {
        _innerStream = innerStream;
        _maxSize = maxSize;
        _bytesRead = 0;
    }
    
    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;
    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);
        _bytesRead += bytesRead;
        
        if (_bytesRead > _maxSize)
        {
            throw new InvalidOperationException(
                $"Request body size exceeds the maximum allowed size of {_maxSize} bytes");
        }
        
        return bytesRead;
    }
    
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        _bytesRead += bytesRead;
        
        if (_bytesRead > _maxSize)
        {
            throw new InvalidOperationException(
                $"Request body size exceeds the maximum allowed size of {_maxSize} bytes");
        }
        
        return bytesRead;
    }
    
    public override void Flush() => _innerStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);
    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
    public override void SetLength(long value) => _innerStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
        }
        base.Dispose(disposing);
    }
}
