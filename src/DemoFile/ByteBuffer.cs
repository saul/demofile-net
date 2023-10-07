using System.Runtime.CompilerServices;

namespace DemoFile;

internal ref struct ByteBuffer
{
    public ReadOnlySpan<byte> Span;
    public int Position;

    public ByteBuffer(ReadOnlySpan<byte> span)
    {
        Span = span;
        Position = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => Span[Position++];

    public uint ReadUVarInt32()
    {
        var c = 0;
        uint result = 0;
        byte b;

        do
        {
            b = ReadByte();
            if (c < 5)
                result |= (uint)(b & 0x7f) << (7 * c);
            c += 1;
        } while ((b & 0x80) != 0);

        return result;
    }

    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        var slice = Span[Position..(Position + length)];
        Position += length;
        return slice;
    }
}
