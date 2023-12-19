using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DemoFile;

internal ref struct BitBuffer
{
    private int _bitsAvail = 0;
    private uint _buf = 0;
    private ReadOnlySpan<byte> _pointer;

    public BitBuffer(ReadOnlySpan<byte> pointer)
    {
        _pointer = pointer;
        FetchNext();
    }

    public int RemainingBytes => _pointer.Length + _bitsAvail / 8;

    private void FetchNext()
    {
        _bitsAvail = _pointer.Length >= 4 ? 32 : _pointer.Length * 8;
        UpdateBuffer();
    }

    public uint ReadUBits(int numBits)
    {
        if (_bitsAvail >= numBits)
        {
            var ret = (uint)(_buf & ((1 << numBits) - 1));
            _bitsAvail -= numBits;
            if (_bitsAvail != 0)
            {
                _buf >>= numBits;
            }
            else
            {
                FetchNext();
            }

            return ret;
        }
        else
        {
            var ret = _buf;
            numBits -= _bitsAvail;

            UpdateBuffer();

            ret |= (uint)((_buf & ((1 << numBits) - 1)) << _bitsAvail);
            _bitsAvail = 32 - numBits;
            _buf >>= numBits;

            return ret;
        }
    }

    public byte ReadByte() => (byte)ReadUBits(8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void UpdateBuffer()
    {
        if (_pointer.Length < 4)
        {
            // .NET 8/PGO optimisation issue (https://github.com/dotnet/runtime/issues/95056)
            // We can't depend on stackalloc being zero-initialised here.
            fixed (uint* bufPtr = &_buf)
            {
                var bufBytes = (byte*) bufPtr;
                for (var i = 0; i < 4; ++i)
                {
                    bufBytes[i] = i < _pointer.Length ? _pointer[i] : default;
                }
            }

            _pointer = default;
        }
        else
        {
            _buf = MemoryMarshal.Read<uint>(_pointer[..4]);
            _pointer = _pointer[4..];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadOneBit()
    {
        var ret = _buf & 1;
        if (--_bitsAvail == 0)
        {
            FetchNext();
        }
        else
        {
            _buf >>= 1;
        }

        return ret != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat()
    {
        var bits = ReadUBits(32);
        unsafe
        {
            return *(float*)&bits;
        }
    }

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
        var bytes = bits / 8;
        var remainder = bits % 8;

        for (var i = 0; i < bytes; ++i)
        {
            output[i] = ReadByte();
        }

        if (remainder != 0)
        {
            output[bytes] = (byte)ReadUBits(remainder);
        }
    }

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
        var c = 0;
        var result = 0UL;
        byte b;

        do
        {
            b = ReadByte();
            if (c < 10)
                result |= (ulong)(b & 0x7f) << 7 * c;
            c += 1;
        } while ((b & 0x80) != 0);

        return result;
    }

    public long ReadVarInt64()
    {
        var result = ReadUVarInt64();
        return (long)(result >> 1) ^ -(long)(result & 1);
    }

    public float ReadAngle(int bits)
    {
        var max = (float)((1UL << bits) - 1);
        return 360.0f * (ReadUBits(bits) / max);
    }

    public float ReadCoord()
    {
        const int FRACT_BITS = 5;

        var hasInt = ReadOneBit();
        var hasFract = ReadOneBit();

        if (hasInt || hasFract)
        {
            var signBit = ReadOneBit();

            var intval = hasInt ? ReadUBits(14) + 1.0f : 0.0f;
            var fractval = hasFract ? ReadUBits(FRACT_BITS) : 0.0f;

            var value = intval + fractval * (1.0f / (1 << FRACT_BITS));
            return signBit ? -value : value;
        }

        return 0.0f;
    }

    public float ReadCoordPrecise()
    {
        return ReadUBits(20) * (360.0f / (1 << 20)) - 180.0f;
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

    public float ReadNormal()
    {
        var isNeg = ReadOneBit();
        var len = ReadUBits(11);
        var ret = len * (1.0f / ((1 << 11) - 1));
        return isNeg ? -ret : ret;
    }

    public Vector Read3BitNormal()
    {
        float x = 0.0f, y = 0.0f;
        
        var hasX = ReadOneBit();
        var hasY = ReadOneBit();
        if (hasX)
            x = ReadNormal();
        if (hasY)
            y = ReadNormal();

        var negZ = ReadOneBit();
        var sumSqr = x * x + y * y;

        var z = sumSqr < 1.0f
            ? (float)Math.Sqrt(1.0 - sumSqr)
            : 0.0f;

        return new Vector(x, y, negZ ? -z : z);
    }
}
