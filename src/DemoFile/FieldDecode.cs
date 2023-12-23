using System.Buffers.Binary;
using System.Diagnostics;
using System.Drawing;
using DemoFile.Sdk;

namespace DemoFile;

internal static class FieldDecode
{
    // Note: some encodings for atomic types are defined in:
    //   game/core/tools/demoinfo2/demoinfo2.txt

    public delegate T FieldDecoder<T>(ref BitBuffer buffer);

    public delegate TValue CustomDeserializer<TType, TValue>(TType @this, ref BitBuffer buffer);

    public static FieldDecoder<T> CreateDecoder_enum<T>(
        FieldEncodingInfo fieldEncodingInfo)
        where T : struct
    {
        // Note that some enums are NOT encoded as unsigned 64-bit,
        // which is what we assume here.
        // One example is TakeDamageFlags_t, which is supposedly
        // NET_DATA_TYPE_INT64. I haven't seen it set in CS2 demos
        // (even though it exists on CBaseEntity)
        return (ref BitBuffer buffer) => (T)Enum.ToObject(typeof(T), buffer.ReadUVarInt64());
    }

    public static FieldDecoder<Vector> CreateDecoder_Vector(FieldEncodingInfo fieldEncodingInfo)
    {
        if (fieldEncodingInfo.VarEncoder == "normal")
        {
            return (ref BitBuffer buffer) => buffer.Read3BitNormal();
        }

        var floatDecoder = CreateDecoder_float(fieldEncodingInfo);
        return (ref BitBuffer buffer) =>
        {
            var x = floatDecoder(ref buffer);
            var y = floatDecoder(ref buffer);
            var z = floatDecoder(ref buffer);

            return new Vector(x, y, z);
        };
    }

    public static FieldDecoder<Vector2D> CreateDecoder_Vector2D(FieldEncodingInfo fieldEncodingInfo)
    {
        var floatDecoder = CreateDecoder_float(fieldEncodingInfo);
        return (ref BitBuffer buffer) =>
        {
            var x = floatDecoder(ref buffer);
            var y = floatDecoder(ref buffer);

            return new Vector2D(x, y);
        };
    }

    public static FieldDecoder<Vector4D> CreateDecoder_Vector4D(FieldEncodingInfo fieldEncodingInfo)
    {
        var floatDecoder = CreateDecoder_float(fieldEncodingInfo);
        return (ref BitBuffer buffer) =>
        {
            var x = floatDecoder(ref buffer);
            var y = floatDecoder(ref buffer);
            var z = floatDecoder(ref buffer);
            var w = floatDecoder(ref buffer);

            return new Vector4D(x, y, z, w);
        };
    }

    public static FieldDecoder<float> CreateDecoder_float(FieldEncodingInfo fieldEncodingInfo)
    {
        switch (fieldEncodingInfo.VarEncoder)
        {
            case "coord":
                return (ref BitBuffer buffer) => buffer.ReadCoord();
            case "simtime":
                return (ref BitBuffer buffer) => DecodeSimulationTime(ref buffer);
            case "runetime":
                return (ref BitBuffer buffer) => DecodeRuneTime(ref buffer);
            case null:
                break;
            default:
                throw new Exception($"Unknown float encoder: {fieldEncodingInfo.VarEncoder}");
        }

        if (fieldEncodingInfo.BitCount <= 0 || fieldEncodingInfo.BitCount >= 32)
        {
            Debug.Assert(fieldEncodingInfo.BitCount <= 32);
            return (ref BitBuffer buffer) => DecodeFloatNoscale(ref buffer);
        }

        var encoding = QuantizedFloatEncoding.Create(fieldEncodingInfo);
        return (ref BitBuffer buffer) => encoding.Decode(ref buffer);
    }

    public static FieldDecoder<ulong> CreateDecoder_UInt64(FieldEncodingInfo fieldEncodingInfo)
    {
        if (fieldEncodingInfo.VarEncoder == "fixed64")
        {
            return (ref BitBuffer buffer) => DecodeFixed64(ref buffer);
        }
        else if (fieldEncodingInfo.VarEncoder != null)
        {
            throw new Exception($"Unknown uint64 encoder: {fieldEncodingInfo.VarEncoder}");
        }

        return (ref BitBuffer buffer) => buffer.ReadUVarInt64();
    }

    public static FieldDecoder<CStrongHandle<T>> CreateDecoder_CStrongHandle<T>(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) => new CStrongHandle<T>(buffer.ReadUVarInt64());

    public static FieldDecoder<CHandle<T>> CreateDecoder_CHandle<T>(FieldEncodingInfo fieldEncodingInfo)
        where T : CEntityInstance =>
        (ref BitBuffer buffer) => new CHandle<T>(buffer.ReadUVarInt64());

    public static FieldDecoder<Color> CreateDecoder_Color(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            var rgba = buffer.ReadUVarInt32();
            uint rr = (rgba & 0xFF000000) >> 24;
            uint gg = (rgba & 0x00FF0000) >> 16;
            uint bb = (rgba & 0x0000FF00) >> 8;
            uint aa = (rgba & 0x000000FF);
            return Color.FromArgb((int)((aa << 24) | (rr << 16) | (gg << 8) | bb));
        };

    public static FieldDecoder<string> CreateDecoder_string(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            return buffer.ReadStringUtf8();
        };

    public static FieldDecoder<GameTime> CreateDecoder_GameTime(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            return new GameTime(DecodeFloatNoscale(ref buffer));
        };

    public static FieldDecoder<GameTick> CreateDecoder_GameTick(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            return new GameTick(buffer.ReadUVarInt32());
        };

    public static FieldDecoder<CEntityIndex> CreateDecoder_CEntityIndex(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            return new CEntityIndex((uint) buffer.ReadVarInt32());
        };

    public static FieldDecoder<CUtlStringToken> CreateDecoder_CUtlStringToken(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            return new CUtlStringToken(buffer.ReadUVarInt32());
        };

    public static FieldDecoder<NetworkedString> CreateDecoder_NetworkedString(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            return new NetworkedString(buffer.ReadStringUtf8());
        };

    public static FieldDecoder<QAngle> CreateDecoder_QAngle(FieldEncodingInfo fieldEncodingInfo)
    {
        if (fieldEncodingInfo.VarEncoder == "qangle_pitch_yaw")
        {
            return (ref BitBuffer buffer) => new QAngle(
                buffer.ReadAngle(fieldEncodingInfo.BitCount),
                buffer.ReadAngle(fieldEncodingInfo.BitCount),
                0.0f);
        }
        else if (fieldEncodingInfo.VarEncoder == "qangle_precise")
        {
            return (ref BitBuffer buffer) =>
            {
                var hasPitch = buffer.ReadOneBit();
                var hasYaw = buffer.ReadOneBit();
                var hasRoll = buffer.ReadOneBit();
                return new QAngle(
                    hasPitch ? buffer.ReadCoordPrecise() : 0.0f,
                    hasYaw ? buffer.ReadCoordPrecise() : 0.0f,
                    hasRoll ? buffer.ReadCoordPrecise() : 0.0f);
            };
        }
        else if (fieldEncodingInfo.VarEncoder != "qangle")
        {
            throw new NotImplementedException($"Unsupported QAngle encoder: {fieldEncodingInfo.VarEncoder}");
        }

        if (fieldEncodingInfo.BitCount != 0)
        {
            return (ref BitBuffer buffer) => new QAngle(
                buffer.ReadAngle(fieldEncodingInfo.BitCount),
                buffer.ReadAngle(fieldEncodingInfo.BitCount),
                buffer.ReadAngle(fieldEncodingInfo.BitCount));
        }

        return (ref BitBuffer buffer) =>
        {
            var hasPitch = buffer.ReadOneBit();
            var hasYaw = buffer.ReadOneBit();
            var hasRoll = buffer.ReadOneBit();
            return new QAngle(
                hasPitch ? buffer.ReadCoord() : 0.0f,
                hasYaw ? buffer.ReadCoord() : 0.0f,
                hasRoll ? buffer.ReadCoord() : 0.0f);
        };
    }

    private static ulong DecodeFixed64(ref BitBuffer buffer)
    {
        Span<byte> bytes = stackalloc byte[8];
        buffer.ReadBytes(bytes);
        return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }

    private static float DecodeRuneTime(ref BitBuffer buffer)
    {
        var bits = buffer.ReadUBits(4);
        unsafe
        {
            return *(float*)&bits;
        }
    }

    internal static float DecodeSimulationTime(ref BitBuffer buffer)
    {
        var ticks = new GameTick(buffer.ReadUVarInt32());
        return ticks.ToGameTime().Value;
    }

    internal static float DecodeFloatNoscale(ref BitBuffer buffer) => buffer.ReadFloat();

    public static FieldDecoder<bool> CreateDecoder_bool(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) => buffer.ReadOneBit();

    public static FieldDecoder<byte> CreateDecoder_byte(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) => (byte)buffer.ReadUVarInt32();

    public static FieldDecoder<sbyte> CreateDecoder_sbyte(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) => (sbyte)buffer.ReadVarInt32();

    public static FieldDecoder<int> CreateDecoder_Int32(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) => buffer.ReadVarInt32();

    public static FieldDecoder<uint> CreateDecoder_UInt32(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) => buffer.ReadUVarInt32();

    public static FieldDecoder<short> CreateDecoder_Int16(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) => (short)buffer.ReadVarInt32();

    public static FieldDecoder<ushort> CreateDecoder_UInt16(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) => (ushort)buffer.ReadUVarInt32();

    public static FieldDecoder<WorldGroupId> CreateDecoder_WorldGroupId(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) => new WorldGroupId(buffer.ReadUVarInt32());

    public static FieldDecoder<AttachmentHandle> CreateDecoder_AttachmentHandle(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) => new AttachmentHandle(buffer.ReadUVarInt64());

    public static FieldDecoder<CTransform> CreateDecoder_CTransform(FieldEncodingInfo fieldFieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            // not seen in CS2 demos
            // equivalent to Vector3 + Quaternion (although represented as 6 floats)
            throw new NotImplementedException("CTransform decoding is not implemented");
        };

    public static FieldDecoder<Quaternion> CreateDecoder_Quaternion(FieldEncodingInfo fieldFieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            // not seen in CS2 demos
            // equivalent to Vector4D
            throw new NotImplementedException("Quaternion decoding is not implemented");
        };
}
