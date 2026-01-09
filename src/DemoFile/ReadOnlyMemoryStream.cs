namespace DemoFile;


    /// <summary>
    /// Represents a read-only stream that provides access to a sequence of bytes stored in a <see cref="ReadOnlyMemory{T}"/>.
    /// This stream implementation is backed by a <see cref="ReadOnlyMemory{T}"/> instance and provides support for reading and seeking operations.
    /// </summary>
public sealed class ReadOnlyMemoryStream : Stream
{
    private readonly ReadOnlyMemory<byte> _memory;
    private int _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyMemoryStream"/> class.
    /// </summary>
    /// <param name="memory">The memory to read from.</param>
    public ReadOnlyMemoryStream(ReadOnlyMemory<byte> memory) => _memory = memory;

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => _memory.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _position;
        set => _position = (int)value;
    }

    /// <inheritdoc />
    public override void Flush() { }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        var remaining = _memory.Length - _position;
        var toCopy = Math.Min(count, remaining);
        if (toCopy <= 0)
        {
            return 0;
        }

        _memory.Span.Slice(_position, toCopy).CopyTo(buffer.AsSpan(offset, toCopy));
        _position += toCopy;
        return toCopy;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPos = origin switch
        {
            SeekOrigin.Begin => (int)offset,
            SeekOrigin.Current => _position + (int)offset,
            SeekOrigin.End => _memory.Length + (int)offset,
            _ => _position
        };

        if (newPos < 0 || newPos > _memory.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        _position = newPos;
        return _position;
    }

    /// <summary>
    /// Sets the length of the stream.
    /// </summary>
    /// <param name="value">The new length of the stream.</param>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <summary>
    /// Writes the specified data to the target output.
    /// </summary>
    /// <param name="buffer">The buffer to write.</param>
    /// <param name="offset">The offset in the buffer to start writing from.</param>
    /// <param name="count">The number of bytes to write.</param>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}