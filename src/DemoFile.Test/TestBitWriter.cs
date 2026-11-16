namespace DemoFile.Test;

/// <summary>
/// Writes bits LSB-first, matching <see cref="BitBuffer"/>.
/// </summary>
internal sealed class TestBitWriter
{
    private readonly List<bool> _bits = new();

    public int BitCount => _bits.Count;

    public TestBitWriter Bits(ulong value, int count)
    {
        for (var i = 0; i < count; i++)
            _bits.Add(((value >> i) & 1) != 0);
        return this;
    }

    public TestBitWriter Bit(bool value) => Bits(value ? 1UL : 0UL, 1);

    public TestBitWriter Float(float value) => Bits(BitConverter.SingleToUInt32Bits(value), 32);

    public TestBitWriter VarInt(ulong value)
    {
        do
        {
            var b = value & 0x7F;
            value >>= 7;
            Bits(value != 0 ? b | 0x80 : b, 8);
        } while (value != 0);
        return this;
    }

    public TestBitWriter ZigZag(long value) => VarInt((ulong)((value << 1) ^ (value >> 63)));

    /// <summary>
    /// The inverse of <see cref="BitBuffer.ReadUBitVar"/>.
    /// </summary>
    public TestBitWriter UBitVar(uint value) => value switch
    {
        < 16 => Bits(value, 6),
        < 256 => Bits((value & 15) | 16, 6).Bits(value >> 4, 4),
        < 4096 => Bits((value & 15) | 32, 6).Bits(value >> 4, 8),
        _ => Bits((value & 15) | 48, 6).Bits(value >> 4, 28),
    };

    /// <summary>
    /// An empty field path list (the "finish" field path op), i.e. no field changes.
    /// </summary>
    public TestBitWriter NoFieldChanges() => Bit(true).Bit(false);

    public byte[] ToArray()
    {
        var bytes = new byte[(_bits.Count + 7) / 8];
        for (var i = 0; i < _bits.Count; i++)
        {
            if (_bits[i])
                bytes[i / 8] |= (byte)(1 << (i % 8));
        }
        return bytes;
    }
}
