using System.Diagnostics.CodeAnalysis;

namespace DemoFile;

internal static class StreamExtensions
{
    public static uint ReadUVarInt32(this Stream stream)
    {
        var c = 0;
        uint result = 0;
        byte b;

        do
        {
            b = stream.ReadByte() is >= 0 and var foo
                ? (byte)foo
                : Throw<byte>();
            
            if (c < 5)
                result |= (uint)(b & 0x7f) << (7 * c);
            c += 1;
        } while ((b & 0x80) != 0);

        return result;
    }

    [DoesNotReturn]
    private static T Throw<T>()
    {
        throw new EndOfStreamException("Unexpected EOF");
    }
}
