using System.Buffers.Binary;
using System.Diagnostics;
using System.Drawing;
using DemoFile.Sdk;

namespace DemoFile;

public static class FieldDecode
{
    // Note: some encodings for atomic types are defined in:
    //   game/core/tools/demoinfo2/demoinfo2.txt

    public delegate T FieldDecoder<T>(ref BitBuffer buffer);

    public delegate TValue CustomDeserializer<TType, TValue>(TType @this, ref BitBuffer buffer);

    /// <summary>
    /// Read an unsigned integer, respecting the field's var encoder.
    /// Varint by default; <c>fixed8</c> (8-bit fields since CS2 build 14197), <c>fixed32</c> and <c>fixed64</c> are raw bits.
    /// </summary>
    public static FieldDecoder<ulong> CreateDecoder_UnsignedInteger(FieldEncodingInfo fieldEncodingInfo) =>
        fieldEncodingInfo.VarEncoder switch
        {
            null => (ref BitBuffer buffer) => buffer.ReadUVarInt64(),
            "fixed8" => (ref BitBuffer buffer) => buffer.ReadByte(),
            "fixed32" => (ref BitBuffer buffer) => buffer.ReadUBits(32),
            "fixed64" => (ref BitBuffer buffer) => DecodeFixed64(ref buffer),
            _ => throw new Exception($"Unknown integer encoder: {fieldEncodingInfo.VarEncoder}")
        };

    /// <summary>
    /// Read a signed integer, respecting the field's var encoder.
    /// Zig-zag varint by default; <c>fixed8</c>/<c>fixed32</c> are raw bits reinterpreted as two's complement.
    /// </summary>
    public static FieldDecoder<long> CreateDecoder_SignedInteger(FieldEncodingInfo fieldEncodingInfo) =>
        fieldEncodingInfo.VarEncoder switch
        {
            null => (ref BitBuffer buffer) => buffer.ReadVarInt64(),
            "fixed8" => (ref BitBuffer buffer) => (sbyte)buffer.ReadByte(),
            "fixed32" => (ref BitBuffer buffer) => (int)buffer.ReadUBits(32),
            "fixed64" => (ref BitBuffer buffer) => (long)DecodeFixed64(ref buffer),
            _ => throw new Exception($"Unknown integer encoder: {fieldEncodingInfo.VarEncoder}")
        };

    /// <summary>
    /// Read an enum value.
    /// </summary>
    /// <remarks>
    /// Enums with a signed representation are zig-zag encoded (the engine's signed integer group), others are
    /// plain unsigned varints. Newer servers say which via <see cref="FieldEncodingInfo.IsSignedEnum"/>;
    /// for older demos we treat an enum as signed if it has a negative member (e.g. <c>AnimLoopMode_t</c>,
    /// <c>PlayerConnectedState</c>, <c>HitGroup_t</c>, <c>loadout_slot_t</c>), which matches Valve's
    /// <c>demoinfo2.txt</c> type table.
    /// </remarks>
    public static FieldDecoder<T> CreateDecoder_enum<T>(
        FieldEncodingInfo fieldEncodingInfo)
        where T : struct
    {
        var isSigned = fieldEncodingInfo.IsSignedEnum ?? EnumSignedness<T>.HasNegativeMembers;
        if (isSigned)
        {
            var signedDecoder = CreateDecoder_SignedInteger(fieldEncodingInfo);
            return (ref BitBuffer buffer) => (T)Enum.ToObject(typeof(T), signedDecoder(ref buffer));
        }

        var decoder = CreateDecoder_UnsignedInteger(fieldEncodingInfo);
        return (ref BitBuffer buffer) => (T)Enum.ToObject(typeof(T), decoder(ref buffer));
    }

    private static class EnumSignedness<T> where T : struct
    {
        public static readonly bool HasNegativeMembers = ComputeHasNegativeMembers();

        private static bool ComputeHasNegativeMembers()
        {
            if (!typeof(T).IsEnum)
                return false;

            var underlying = Enum.GetUnderlyingType(typeof(T));
            if (underlying == typeof(byte) || underlying == typeof(ushort) || underlying == typeof(uint) || underlying == typeof(ulong))
                return false;

            foreach (var value in Enum.GetValuesAsUnderlyingType(typeof(T)))
            {
                if (Convert.ToInt64(value) < 0)
                    return true;
            }

            return false;
        }
    }

    public static FieldDecoder<Vector> CreateDecoder_Vector(FieldEncodingInfo fieldEncodingInfo)
    {
        var coord = fieldEncodingInfo.CoordParams;

        switch (fieldEncodingInfo.VarEncoder)
        {
            case "normal":
            {
                var normalBits = coord.NormalFractionalBits;
                return (ref BitBuffer buffer) => buffer.Read3BitNormal(normalBits);
            }
            case "coord_integral":
            {
                var (intBits, intBitsMp) = (coord.IntegerBits, coord.IntegerBitsMp);
                return (ref BitBuffer buffer) => new Vector(
                    buffer.ReadCoordIntegral(intBits, intBitsMp),
                    buffer.ReadCoordIntegral(intBits, intBitsMp),
                    buffer.ReadCoordIntegral(intBits, intBitsMp));
            }
            case "qangle" or "qangle_pitch_yaw" or "qangle_precise":
            {
                // All float-group encoders work on any 3-component type
                var angleDecoder = CreateDecoder_QAngle(fieldEncodingInfo);
                return (ref BitBuffer buffer) =>
                {
                    var angle = angleDecoder(ref buffer);
                    return new Vector(angle.Pitch, angle.Yaw, angle.Roll);
                };
            }
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
        var coord = fieldEncodingInfo.CoordParams;

        switch (fieldEncodingInfo.VarEncoder)
        {
            case "coord":
            {
                var (intBits, fracBits) = (coord.IntegerBits, coord.FractionalBits);
                return (ref BitBuffer buffer) => buffer.ReadCoord(intBits, fracBits);
            }
            case "coord_integral":
            {
                var (intBits, intBitsMp) = (coord.IntegerBits, coord.IntegerBitsMp);
                return (ref BitBuffer buffer) => buffer.ReadCoordIntegral(intBits, intBitsMp);
            }
            case "noscale":
                return (ref BitBuffer buffer) => DecodeFloatNoscale(ref buffer);
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

    public static FieldDecoder<ulong> CreateDecoder_UInt64(FieldEncodingInfo fieldEncodingInfo) =>
        CreateDecoder_UnsignedInteger(fieldEncodingInfo);

    public static FieldDecoder<CStrongHandle<T>> CreateDecoder_CStrongHandle<T>(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) => new CStrongHandle<T>(buffer.ReadUVarInt64());

    public static FieldDecoder<CHandle<T, TGameParser>> CreateDecoder_CHandle<T, TGameParser>(FieldEncodingInfo fieldEncodingInfo)
        where T : CEntityInstance<TGameParser>
        where TGameParser : DemoParser<TGameParser>, new()
    {
        if (fieldEncodingInfo.VarEncoder == null)
            return (ref BitBuffer buffer) => new CHandle<T, TGameParser>(buffer.ReadUVarInt64());

        var decoder = CreateDecoder_UnsignedInteger(fieldEncodingInfo);
        return (ref BitBuffer buffer) => new CHandle<T, TGameParser>(decoder(ref buffer));
    }

    public static FieldDecoder<Color> CreateDecoder_Color(FieldEncodingInfo fieldEncodingInfo)
    {
        var decoder = CreateDecoder_UInt32(fieldEncodingInfo);
        return (ref BitBuffer buffer) =>
        {
            var rgba = decoder(ref buffer);
            uint rr = (rgba & 0xFF000000) >> 24;
            uint gg = (rgba & 0x00FF0000) >> 16;
            uint bb = (rgba & 0x0000FF00) >> 8;
            uint aa = (rgba & 0x000000FF);
            return Color.FromArgb((int)((aa << 24) | (rr << 16) | (gg << 8) | bb));
        };
    }

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

    /// <summary>
    /// <c>GameTick_t</c> wraps an <c>int32</c>, so it's zig-zag encoded (engine signed integer group).
    /// Negative ticks (e.g. -1 for "never") wrap around in <see cref="GameTick.Value"/>.
    /// </summary>
    public static FieldDecoder<GameTick> CreateDecoder_GameTick(FieldEncodingInfo fieldEncodingInfo)
    {
        var decoder = CreateDecoder_Int32(fieldEncodingInfo);
        return (ref BitBuffer buffer) => new GameTick((uint)decoder(ref buffer));
    }

    public static FieldDecoder<CEntityIndex> CreateDecoder_CEntityIndex(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            return new CEntityIndex((uint) buffer.ReadVarInt32());
        };

    public static FieldDecoder<CUtlStringToken> CreateDecoder_CUtlStringToken(FieldEncodingInfo fieldEncodingInfo)
    {
        var decoder = CreateDecoder_UInt32(fieldEncodingInfo);
        return (ref BitBuffer buffer) => new CUtlStringToken(decoder(ref buffer));
    }

    public static FieldDecoder<CGlobalSymbol> CreateDecoder_CGlobalSymbol(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            return new CGlobalSymbol(buffer.ReadStringUtf8());
        };

    /// <summary>
    /// <c>CPlayerSlot</c> wraps an <c>int32</c>, so it's zig-zag encoded (engine signed integer group).
    /// -1 ("no player") wraps around to <see cref="uint.MaxValue"/> in <see cref="CPlayerSlot.Value"/>.
    /// </summary>
    public static FieldDecoder<CPlayerSlot> CreateDecoder_CPlayerSlot(FieldEncodingInfo fieldEncodingInfo)
    {
        var decoder = CreateDecoder_Int32(fieldEncodingInfo);
        return (ref BitBuffer buffer) => new CPlayerSlot((uint)decoder(ref buffer));
    }

    public static FieldDecoder<NetworkedString> CreateDecoder_NetworkedString(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            return new NetworkedString(buffer.ReadStringUtf8());
        };

    /// <remarks>
    /// Mirrors the engine's float-group encoders for 3-component types (networksystem):
    /// <list type="bullet">
    /// <item><c>qangle</c>: bits = 0 → 3 presence bits + coords; bits = 32 → 3 raw floats; else 3 bit-angles.</item>
    /// <item><c>qangle_pitch_yaw</c>: bits = 0 or 32 → 2 raw floats; else 2 bit-angles. Roll is always 0.</item>
    /// <item><c>qangle_precise</c>: bits = 0 → 3 presence bits + precise angles; else 3 bit-angles (no presence bits).</item>
    /// <item>no encoder: 3 floats using the field's float encoding.</item>
    /// </list>
    /// </remarks>
    public static FieldDecoder<QAngle> CreateDecoder_QAngle(FieldEncodingInfo fieldEncodingInfo)
    {
        var coord = fieldEncodingInfo.CoordParams;
        var bits = fieldEncodingInfo.BitCount;

        switch (fieldEncodingInfo.VarEncoder)
        {
            case null:
            {
                var floatDecoder = CreateDecoder_float(fieldEncodingInfo);
                return (ref BitBuffer buffer) =>
                {
                    var pitch = floatDecoder(ref buffer);
                    var yaw = floatDecoder(ref buffer);
                    var roll = floatDecoder(ref buffer);
                    return new QAngle(pitch, yaw, roll);
                };
            }
            case "qangle" when bits == 0:
            {
                var (intBits, fracBits) = (coord.IntegerBits, coord.FractionalBits);
                return (ref BitBuffer buffer) => ReadCoordAngles(ref buffer, intBits, fracBits);
            }
            case "qangle" when bits == 32:
                return (ref BitBuffer buffer) => new QAngle(
                    buffer.ReadFloat(),
                    buffer.ReadFloat(),
                    buffer.ReadFloat());
            case "qangle":
                return (ref BitBuffer buffer) => new QAngle(
                    buffer.ReadAngle(bits),
                    buffer.ReadAngle(bits),
                    buffer.ReadAngle(bits));
            case "qangle_pitch_yaw" when bits is 0 or 32:
                return (ref BitBuffer buffer) => new QAngle(
                    buffer.ReadFloat(),
                    buffer.ReadFloat(),
                    0.0f);
            case "qangle_pitch_yaw":
                return (ref BitBuffer buffer) => new QAngle(
                    buffer.ReadAngle(bits),
                    buffer.ReadAngle(bits),
                    0.0f);
            case "qangle_precise" when bits == 0:
            {
                var angleBits = coord.AngleBits;
                return (ref BitBuffer buffer) =>
                {
                    var hasPitch = buffer.ReadOneBit();
                    var hasYaw = buffer.ReadOneBit();
                    var hasRoll = buffer.ReadOneBit();
                    return new QAngle(
                        hasPitch ? buffer.ReadCoordPrecise(angleBits) : 0.0f,
                        hasYaw ? buffer.ReadCoordPrecise(angleBits) : 0.0f,
                        hasRoll ? buffer.ReadCoordPrecise(angleBits) : 0.0f);
                };
            }
            case "qangle_precise":
                return (ref BitBuffer buffer) => new QAngle(
                    buffer.ReadAngle(bits),
                    buffer.ReadAngle(bits),
                    buffer.ReadAngle(bits));
            case "coord":
            {
                var (intBits, fracBits) = (coord.IntegerBits, coord.FractionalBits);
                return (ref BitBuffer buffer) => new QAngle(
                    buffer.ReadCoord(intBits, fracBits),
                    buffer.ReadCoord(intBits, fracBits),
                    buffer.ReadCoord(intBits, fracBits));
            }
            case "coord_integral":
            {
                var (intBits, intBitsMp) = (coord.IntegerBits, coord.IntegerBitsMp);
                return (ref BitBuffer buffer) => new QAngle(
                    buffer.ReadCoordIntegral(intBits, intBitsMp),
                    buffer.ReadCoordIntegral(intBits, intBitsMp),
                    buffer.ReadCoordIntegral(intBits, intBitsMp));
            }
            case "normal":
            {
                var normalBits = coord.NormalFractionalBits;
                return (ref BitBuffer buffer) =>
                {
                    var normal = buffer.Read3BitNormal(normalBits);
                    return new QAngle(normal.X, normal.Y, normal.Z);
                };
            }
            default:
                throw new NotImplementedException($"Unsupported QAngle encoder: {fieldEncodingInfo.VarEncoder}");
        }
    }

    /// <summary>
    /// 3 presence bits, then a coord for each present component (the <c>qangle</c> encoder with no bit count,
    /// and the rotation half of a <c>coord</c>/<c>coord_integral</c> <c>CTransform</c>).
    /// </summary>
    private static QAngle ReadCoordAngles(ref BitBuffer buffer, int intBits, int fracBits)
    {
        var hasPitch = buffer.ReadOneBit();
        var hasYaw = buffer.ReadOneBit();
        var hasRoll = buffer.ReadOneBit();
        return new QAngle(
            hasPitch ? buffer.ReadCoord(intBits, fracBits) : 0.0f,
            hasYaw ? buffer.ReadCoord(intBits, fracBits) : 0.0f,
            hasRoll ? buffer.ReadCoord(intBits, fracBits) : 0.0f);
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

    public static float DecodeSimulationTime(ref BitBuffer buffer)
    {
        var ticks = new GameTick(buffer.ReadUVarInt32());
        return ticks.ToGameTime().Value;
    }

    public static float DecodeFloatNoscale(ref BitBuffer buffer) => buffer.ReadFloat();

    public static FieldDecoder<bool> CreateDecoder_bool(FieldEncodingInfo fieldEncodingInfo) =>
        (ref BitBuffer buffer) => buffer.ReadOneBit();

    public static FieldDecoder<byte> CreateDecoder_byte(FieldEncodingInfo fieldEncodingInfo)
    {
        var decoder = CreateDecoder_UnsignedInteger(fieldEncodingInfo);
        return (ref BitBuffer buffer) => (byte)decoder(ref buffer);
    }

    public static FieldDecoder<sbyte> CreateDecoder_sbyte(FieldEncodingInfo fieldEncodingInfo)
    {
        var decoder = CreateDecoder_SignedInteger(fieldEncodingInfo);
        return (ref BitBuffer buffer) => (sbyte)decoder(ref buffer);
    }

    // The 32/16-bit decoders keep a fast path for the (common) no-encoder case,
    // and otherwise honour fixed8/fixed32/fixed64 like the engine's integer groups.

    public static FieldDecoder<int> CreateDecoder_Int32(FieldEncodingInfo fieldEncodingInfo)
    {
        if (fieldEncodingInfo.VarEncoder == null)
            return (ref BitBuffer buffer) => buffer.ReadVarInt32();

        var decoder = CreateDecoder_SignedInteger(fieldEncodingInfo);
        return (ref BitBuffer buffer) => (int)decoder(ref buffer);
    }

    public static FieldDecoder<uint> CreateDecoder_UInt32(FieldEncodingInfo fieldEncodingInfo)
    {
        if (fieldEncodingInfo.VarEncoder == null)
            return (ref BitBuffer buffer) => buffer.ReadUVarInt32();

        var decoder = CreateDecoder_UnsignedInteger(fieldEncodingInfo);
        return (ref BitBuffer buffer) => (uint)decoder(ref buffer);
    }

    public static FieldDecoder<short> CreateDecoder_Int16(FieldEncodingInfo fieldEncodingInfo)
    {
        var decoder = CreateDecoder_Int32(fieldEncodingInfo);
        return (ref BitBuffer buffer) => (short)decoder(ref buffer);
    }

    public static FieldDecoder<ushort> CreateDecoder_UInt16(FieldEncodingInfo fieldEncodingInfo)
    {
        var decoder = CreateDecoder_UInt32(fieldEncodingInfo);
        return (ref BitBuffer buffer) => (ushort)decoder(ref buffer);
    }

    public static FieldDecoder<WorldGroupId> CreateDecoder_WorldGroupId(FieldEncodingInfo fieldEncodingInfo)
    {
        var decoder = CreateDecoder_UInt32(fieldEncodingInfo);
        return (ref BitBuffer buffer) => new WorldGroupId(decoder(ref buffer));
    }

    /// <remarks>
    /// The engine networks a transform as 6 floats: position (x, y, z) then orientation as a <see cref="QAngle"/>,
    /// converted with Source's <c>AngleQuaternion</c>. Encoders:
    /// <list type="bullet">
    /// <item>none: 6 floats using the field's float encoding.</item>
    /// <item><c>coord</c>: 3 coords for the position, then 3 presence bits + coords for the angles.</item>
    /// <item><c>coord_integral</c>: 3 integral coords for the position, then 3 presence bits + coords for the angles.</item>
    /// </list>
    /// </remarks>
    public static FieldDecoder<CTransform> CreateDecoder_CTransform(FieldEncodingInfo fieldEncodingInfo)
    {
        var coord = fieldEncodingInfo.CoordParams;
        var (intBits, fracBits, intBitsMp) = (coord.IntegerBits, coord.FractionalBits, coord.IntegerBitsMp);

        switch (fieldEncodingInfo.VarEncoder)
        {
            case null:
            {
                var floatDecoder = CreateDecoder_float(fieldEncodingInfo);
                return (ref BitBuffer buffer) =>
                {
                    var position = new Vector(floatDecoder(ref buffer), floatDecoder(ref buffer), floatDecoder(ref buffer));
                    var angles = new QAngle(floatDecoder(ref buffer), floatDecoder(ref buffer), floatDecoder(ref buffer));
                    return new CTransform(position, AngleQuaternion(angles));
                };
            }
            case "coord":
                return (ref BitBuffer buffer) =>
                {
                    var position = new Vector(
                        buffer.ReadCoord(intBits, fracBits),
                        buffer.ReadCoord(intBits, fracBits),
                        buffer.ReadCoord(intBits, fracBits));
                    var angles = ReadCoordAngles(ref buffer, intBits, fracBits);
                    return new CTransform(position, AngleQuaternion(angles));
                };
            case "coord_integral":
                return (ref BitBuffer buffer) =>
                {
                    var position = new Vector(
                        buffer.ReadCoordIntegral(intBits, intBitsMp),
                        buffer.ReadCoordIntegral(intBits, intBitsMp),
                        buffer.ReadCoordIntegral(intBits, intBitsMp));
                    var angles = ReadCoordAngles(ref buffer, intBits, fracBits);
                    return new CTransform(position, AngleQuaternion(angles));
                };
            default:
                throw new NotImplementedException($"Unsupported CTransform encoder: {fieldEncodingInfo.VarEncoder}");
        }
    }

    /// <summary>
    /// Source's <c>AngleQuaternion(const QAngle&amp;, Quaternion&amp;)</c>, using the same float operation order
    /// as the engine (mathlib, called from networksystem when setting a networked <c>CTransform</c>).
    /// </summary>
    internal static Quaternion AngleQuaternion(QAngle angles)
    {
        const float halfDegToRad = 0.008726646f; // pi / 360

        var (sp, cp) = MathF.SinCos(angles.Pitch * halfDegToRad);
        var (sy, cy) = MathF.SinCos(angles.Yaw * halfDegToRad);
        var (sr, cr) = MathF.SinCos(angles.Roll * halfDegToRad);

        var srXsp = sr * sp;
        var srXcp = sr * cp;
        var crXsp = cr * sp;
        var crXcp = cr * cp;

        return new Quaternion(
            X: cy * srXcp - sy * crXsp,
            Y: cy * crXsp + sy * srXcp,
            Z: sy * crXcp - cy * srXsp,
            W: sy * srXsp + cy * crXcp);
    }

    /// <summary>
    /// The engine networks quaternions as 4 floats (x, y, z, w) using the field's float encoding.
    /// </summary>
    public static FieldDecoder<Quaternion> CreateDecoder_Quaternion(FieldEncodingInfo fieldEncodingInfo)
    {
        var floatDecoder = CreateDecoder_float(fieldEncodingInfo);
        return (ref BitBuffer buffer) =>
        {
            var x = floatDecoder(ref buffer);
            var y = floatDecoder(ref buffer);
            var z = floatDecoder(ref buffer);
            var w = floatDecoder(ref buffer);
            return new Quaternion(x, y, z, w);
        };
    }

    public static FieldDecoder<CUtlBinaryBlock> CreateDecoder_CUtlBinaryBlock(FieldEncodingInfo fieldFieldEncodingInfo) =>
        (ref BitBuffer buffer) =>
        {
            var byteCount = buffer.ReadUVarInt32();

            // Avoid allocating opaque binary blob - just skip over
            for (var i = 0; i < byteCount; i++)
                buffer.ReadByte();

            return new CUtlBinaryBlock(Array.Empty<byte>());
        };
}
