using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace DemoFile.Game.Cs;

/// <summary>
/// Minimal allocation-free protobuf reader for Valve's <c>codegen_delta_encoder</c> deltas.
/// </summary>
/// <remarks>
/// The delta format is the protobuf wire format with one extension: wire type 7 (no payload) means
/// "clear this field". Present fields carry their new value; message fields carry a nested delta;
/// repeated fields carry an array delta (see <see cref="UserCmdDeltaDecoder"/>).
/// </remarks>
internal ref struct ProtoDeltaReader
{
    public const int WireVarint = 0;
    public const int WireFixed64 = 1;
    public const int WireLengthDelimited = 2;
    public const int WireFixed32 = 5;
    public const int WireClear = 7;

    private readonly ReadOnlySpan<byte> _data;
    private int _pos;

    public ProtoDeltaReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _pos = 0;
    }

    public bool IsAtEnd
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _pos >= _data.Length;
    }

    public int Remaining => _data.Length - _pos;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadTag(out int fieldNumber, out int wireType)
    {
        if (_pos >= _data.Length)
        {
            fieldNumber = 0;
            wireType = 0;
            return false;
        }

        var tag = ReadVarint32();
        fieldNumber = (int)(tag >> 3);
        wireType = (int)(tag & 7);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadVarint32()
    {
        // Fast path for single-byte varints (the vast majority of tags)
        var b = _data[_pos];
        if (b < 0x80)
        {
            _pos++;
            return b;
        }

        return (uint)ReadVarint64Slow();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadVarint64()
    {
        var b = _data[_pos];
        if (b < 0x80)
        {
            _pos++;
            return b;
        }

        return ReadVarint64Slow();
    }

    private ulong ReadVarint64Slow()
    {
        // Unrolled for 2 and 3 byte varints, which cover most multi-byte values in user commands
        // (e.g. ticks ~50000). Callers have already handled the 1-byte case.
        var pos = _pos;
        if (pos + 2 < _data.Length)
        {
            ulong b0 = _data[pos];
            ulong b1 = _data[pos + 1];
            if (b1 < 0x80)
            {
                _pos = pos + 2;
                return (b0 & 0x7F) | (b1 << 7);
            }

            ulong b2 = _data[pos + 2];
            if (b2 < 0x80)
            {
                _pos = pos + 3;
                return (b0 & 0x7F) | ((b1 & 0x7F) << 7) | (b2 << 14);
            }
        }

        ulong result = 0;
        var shift = 0;
        byte b;
        do
        {
            b = _data[pos++];
            if (shift < 64)
                result |= (ulong)(b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        _pos = pos;
        return result;
    }

    /// <summary>Read a protobuf <c>int32</c> (negative values are 10-byte varints).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32() => (int)ReadVarint64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32() => (uint)ReadVarint64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => ReadVarint64() != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat()
    {
        var value = BinaryPrimitives.ReadSingleLittleEndian(_data.Slice(_pos, 4));
        _pos += 4;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadLengthDelimited()
    {
        var length = (int)ReadVarint32();
        var span = _data.Slice(_pos, length);
        _pos += length;
        return span;
    }

    public void SkipField(int wireType)
    {
        switch (wireType)
        {
            case WireVarint:
                ReadVarint64();
                break;
            case WireFixed64:
                _pos += 8;
                break;
            case WireLengthDelimited:
                ReadLengthDelimited();
                break;
            case WireFixed32:
                _pos += 4;
                break;
            case WireClear:
                break;
            default:
                throw new InvalidDataException($"Unsupported wire type {wireType} in user command delta");
        }
    }
}
