using System.Runtime.CompilerServices;
using System.Text;
using System.Runtime.InteropServices;

namespace DemoFile;

public ref struct BitBuffer
{
    private static readonly ulong[] BitMask64;

    private int _bitsRead = 0;
    private int _bitsAvail = 0;
    private ulong _buf = 0;
    private ReadOnlySpan<byte> _original;
    private ReadOnlySpan<byte> _pointer;

    static BitBuffer()
    {
        BitMask64 = new ulong[65];
        for (var i = 1; i < 64; ++i)
        {
            BitMask64[i] = (1UL << i) - 1;
        }
        BitMask64[64] = ulong.MaxValue;
    }

    public BitBuffer(ReadOnlySpan<byte> pointer)
    {
        _original = pointer;
        _pointer = pointer;
        FetchNext();
    }

    public BitBuffer Clone()
    {
        var (fromByte, skipBits) = Math.DivRem(_bitsRead, 8);
        var cloned = new BitBuffer(_original[fromByte..]);
        if (skipBits > 0)
            cloned.ReadUBits(skipBits);
        return cloned;
    }

    public int TellBits => _bitsRead;

    public int RemainingBytes => _pointer.Length + _bitsAvail / 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FetchNext()
    {
        var len = _pointer.Length;
        _bitsAvail = len >= 8 ? 64 : len * 8;
        UpdateBuffer();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUBits(int numBits)
    {
        _bitsRead += numBits;

        if (_bitsAvail >= numBits)
        {
            var ret = (uint)(_buf & BitMask64[numBits]);
            _bitsAvail -= numBits;
            _buf >>= numBits;

            if (_bitsAvail == 0)
                FetchNext();

            return ret;
        }
        else
        {
            var ret = (uint)_buf;
            numBits -= _bitsAvail;

            UpdateBuffer();

            ret |= (uint)(_buf & BitMask64[numBits]) << _bitsAvail;
            _bitsAvail = 64 - numBits;
            _buf >>= numBits;

            return ret;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => (byte) ReadUBits(8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateBuffer()
    {
        var len = _pointer.Length;

        if (len >= 8)
        {
            _buf = MemoryMarshal.Read<ulong>(_pointer);
            _pointer = _pointer[8..];
        }
        else if (len > 0)
        {
            // Read remaining bytes into lower bits
            _buf = 0;
            for (int i = 0; i < len; i++)
            {
                _buf |= (ulong)_pointer[i] << (i * 8);
            }
            _pointer = default;
        }
        else
        {
            _buf = 0;
            _pointer = default;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadOneBit()
    {
        bool ret = (_buf & 1) != 0;
        _buf >>= 1;
        unchecked {_bitsRead++;}

        if (--_bitsAvail == 0)
            FetchNext();

        return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => BitConverter.UInt32BitsToSingle(ReadUBits(32));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUBitVar()
    {
        var ret = ReadUBits(6);
        switch (ret & (16 | 32))
        {
            case 16:
                ret = (ret & 15) | (ReadUBits(4) << 4);
                break;
            case 32:
                ret = (ret & 15) | (ReadUBits(8) << 4);
                break;
            case 48:
                ret = (ret & 15) | (ReadUBits(32 - 4) << 4);
                break;
        }

        return ret;
    }

    public uint ReadUVarInt32()
    {
        uint result = 0;
        var shift = 0;
        byte byteRead;

        do
        {
            byteRead = ReadByte();
            result |= (uint)(byteRead & 0x7F) << shift;
            shift += 7;
        } while ((byteRead & 0x80) != 0);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadVarInt32()
    {
        var result = ReadUVarInt32();
        return (int)(result >> 1) ^ -(int)(result & 1);
    }

    public void ReadBytes(scoped Span<byte> output)
    {
        for (var i = 0; i < output.Length; ++i)
        {
            output[i] = ReadByte();
        }
    }

    public void ReadBitsAsBytes(scoped Span<byte> output, int bits)
    {
        var (bytes, remainder) = Math.DivRem(bits, 8);

        for (var i = 0; i < bytes; ++i)
        {
            output[i] = ReadByte();
        }

        if (remainder != 0)
        {
            output[bytes] = (byte)ReadUBits(remainder);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadUBitVarFieldPath()
    {
        if (ReadOneBit())
            return (int)ReadUBits(2);
        if (ReadOneBit())
            return (int)ReadUBits(4);
        if (ReadOneBit())
            return (int)ReadUBits(10);
        if (ReadOneBit())
            return (int)ReadUBits(17);

        return (int)ReadUBits(31);
    }

    public ulong ReadUVarInt64()
    {
        var result = 0UL;
        var shift = 0;
        byte b;

        do
        {
            b = ReadByte();
            if (shift < 70) // 10 * 7
                result |= (ulong)(b & 0x7f) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadVarInt64()
    {
        var result = ReadUVarInt64();
        return (long)(result >> 1) ^ -(long)(result & 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadAngle(int bits)
    {
        var max = (1UL << bits) - 1;
        return 360.0f * ReadUBits(bits) / max;
    }

    public float ReadCoord()
    {
        const int FRACT_BITS = 5;
        const float FRACT_SCALE = 1.0f / (1 << FRACT_BITS);

        var hasInt = ReadOneBit();
        var hasFract = ReadOneBit();

        if (hasInt || hasFract)
        {
            var signBit = ReadOneBit();

            var intval = hasInt ? ReadUBits(14) + 1.0f : 0.0f;
            var fractval = hasFract ? ReadUBits(FRACT_BITS) : 0.0f;

            var value = intval + fractval * FRACT_SCALE;
            return signBit ? -value : value;
        }

        return 0.0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadCoordPrecise()
    {
        const float SCALE = 360.0f / (1 << 20);
        return ReadUBits(20) * SCALE - 180.0f;
    }

    public string ReadStringUtf8()
    {
        // Allocate on the stack initially
        Span<byte> buf = stackalloc byte[260];
        
        var i = 0;
        byte b;
        while ((b = ReadByte()) != 0)
        {
            if (i == buf.Length)
            {
                var newBuf = new byte[buf.Length * 2];
                buf.CopyTo(newBuf);
                buf = newBuf;
            }

            buf[i++] = b;
        }

        // perf: tried using StringPool here, practically no difference
        return Encoding.UTF8.GetString(buf[..i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadNormal()
    {
        const float SCALE = 1.0f / ((1 << 11) - 1);
        var isNeg = ReadOneBit();
        var len = ReadUBits(11);
        var ret = len * SCALE;
        return isNeg ? -ret : ret;
    }

    public Vector Read3BitNormal()
    {
        var hasX = ReadOneBit();
        var hasY = ReadOneBit();

        var x = hasX ? ReadNormal() : 0.0f;
        var y = hasY ? ReadNormal() : 0.0f;

        var negZ = ReadOneBit();
        var sumSqr = x * x + y * y;

        var z = sumSqr < 1.0f
            ? MathF.Sqrt(1.0f - sumSqr)
            : 0.0f;

        return new Vector(x, y, negZ ? -z : z);
    }
}