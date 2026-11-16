using DemoFile.Game.Cs;
using BitWriter = DemoFile.Test.TestBitWriter;

namespace DemoFile.Test.Unit;

/// <summary>
/// Decoder behaviour verified against the CS2 engine (networksystem.dll, patch 14197).
/// See ENGINE_PARITY.md for the reverse-engineering notes behind each case.
/// </summary>
[TestFixture]
public class EngineParityDecodeTest
{
    private static FieldEncodingInfo Encoding(string? encoder, int bitCount = 0) =>
        new(encoder, bitCount, 0, null, null);

    /// <summary>
    /// Golden values for quantized float decoding, computed with the engine's formula
    /// <c>min(high, (float)((double)u * (1.0 / (steps - 1)) * (double)(high - low) + (double)low))</c>.
    /// Each case is one where the previous float-precision implementation gave a different answer
    /// (e.g. 16.093748 instead of 16.09375 for player origin cell offsets), or overshot <c>high</c>.
    /// </summary>
    // m_vecX/m_vecY (CNetworkedQuantizedFloat, origin cell offset): 15 bits, RoundDown, [0, 1024]
    [TestCase(15, 1, float.NaN, 1024.0f, 515u, 16.09375f)]
    [TestCase(15, 1, float.NaN, 1024.0f, 32767u, 1023.96875f)]
    // m_flPlaybackRate: 8 bits, RoundDown | EncodeZero, [-4, 12]. Max used to decode as 11.9375009 > high
    [TestCase(8, 5, -4.0f, 12.0f, 33u, -1.9375f)]
    [TestCase(8, 5, -4.0f, 12.0f, 255u, 11.9375f)]
    // 20 bits, EncodeZero, [-16384, 16384]
    [TestCase(20, 4, -16384.0f, 16384.0f, 16949u, -15854.3427734375f)]
    // m_TextureScale: 10 bits, [0.1, 10]
    [TestCase(10, 0, 0.1f, 10.0f, 5u, 0.1483870893716812f)]
    // m_flWaterLevel: 8 bits, EncodeIntegers (flags -8), [0, 1] -> engine range [0, 2)
    [TestCase(8, -8, float.NaN, 1.0f, 128u, 1.0f)]
    [TestCase(8, -8, float.NaN, 1.0f, 255u, 1.9921875f)]
    public void QuantizedFloat_Decode_MatchesEngineGoldenValues(
        int bits, int flags, float low, float high, uint raw, float expected)
    {
        var info = new FieldEncodingInfo(null, bits, flags, float.IsNaN(low) ? null : low, high);
        var qf = QuantizedFloatEncoding.Create(info);

        // Any remaining round/zero flags are read as a leading 0 bit each before the value
        var writer = new BitWriter();
        foreach (var flag in new[] { QuantizedFloatFlags.RoundDown, QuantizedFloatFlags.RoundUp, QuantizedFloatFlags.EncodeZero })
        {
            if (qf.Flags.HasFlag(flag))
                writer.Bit(false);
        }
        writer.Bits(raw, qf.BitCount);

        var buffer = new BitBuffer(writer.ToArray());
        var decoded = FieldDecode.CreateDecoder_float(info)(ref buffer);

        Assert.That(BitConverter.SingleToUInt32Bits(decoded), Is.EqualTo(BitConverter.SingleToUInt32Bits(expected)),
            $"expected {expected:R}, got {decoded:R}");
        Assert.That(decoded, Is.LessThanOrEqualTo(qf.High));
    }

    [Test]
    public void BitBuffer_PositionIsCorrectNearEndOfBuffer()
    {
        // 12 bytes: the second refill only loads 4 bytes (32 bits)
        var data = Enumerable.Range(0, 12).Select(i => (byte)(i * 37)).ToArray();
        var buffer = new BitBuffer(data);

        buffer.ReadUBits(30);
        buffer.ReadUBits(30);
        buffer.ReadUBits(10); // crosses into the final 4 bytes

        Assert.That(buffer.TellBits, Is.EqualTo(70));
        Assert.That(buffer.RemainingBytes, Is.EqualTo(3));

        var clone = buffer.Clone();
        Assert.That(clone.ReadUBits(26), Is.EqualTo(buffer.ReadUBits(26)));
        Assert.That(buffer.RemainingBytes, Is.EqualTo(0));
        Assert.That(buffer.TellBits, Is.EqualTo(96));
    }

    [TestCase(8, 64u, 90.0f)]
    [TestCase(8, 128u, 180.0f)]
    [TestCase(13, 2048u, 90.0f)]
    public void ReadAngle_DividesByPowerOfTwo(int bits, uint value, float expected)
    {
        var buffer = new BitBuffer(new BitWriter().Bits(value, bits).ToArray());
        Assert.That(buffer.ReadAngle(bits), Is.EqualTo(expected));
    }

    [Test]
    public void QAngle_Qangle32_ReadsRawFloats()
    {
        // e.g. CCSPlayer_AimPunchServices.m_predictableBaseAngle
        var writer = new BitWriter().Float(-0.0695f).Float(-0.0167f).Float(0.0f);
        var buffer = new BitBuffer(writer.ToArray());

        var decoded = FieldDecode.CreateDecoder_QAngle(Encoding("qangle", 32))(ref buffer);

        Assert.That(decoded, Is.EqualTo(new QAngle(-0.0695f, -0.0167f, 0.0f)));
        Assert.That(buffer.TellBits, Is.EqualTo(96));
    }

    [Test]
    public void QAngle_QangleWithBitCount_ReadsBitAngles()
    {
        var writer = new BitWriter().Bits(1024, 13).Bits(2048, 13).Bits(0, 13);
        var buffer = new BitBuffer(writer.ToArray());

        var decoded = FieldDecode.CreateDecoder_QAngle(Encoding("qangle", 13))(ref buffer);

        Assert.That(decoded, Is.EqualTo(new QAngle(45.0f, 90.0f, 0.0f)));
    }

    [Test]
    public void QAngle_QanglePreciseWithBitCount_HasNoPresenceBits()
    {
        var writer = new BitWriter().Bits(64, 8).Bits(128, 8).Bits(192, 8);
        var buffer = new BitBuffer(writer.ToArray());

        var decoded = FieldDecode.CreateDecoder_QAngle(Encoding("qangle_precise", 8))(ref buffer);

        Assert.That(decoded, Is.EqualTo(new QAngle(90.0f, 180.0f, 270.0f)));
        Assert.That(buffer.TellBits, Is.EqualTo(24));
    }

    [Test]
    public void QAngle_QanglePrecise_UsesAngleBitsFromCoordSizeParams()
    {
        var encoding = Encoding("qangle_precise") with
        {
            CoordSizeParams = CoordSizeParams.Legacy with { AngleBits = 10 }
        };
        // pitch present, yaw absent, roll present
        var writer = new BitWriter().Bit(true).Bit(false).Bit(true).Bits(768, 10).Bits(256, 10);
        var buffer = new BitBuffer(writer.ToArray());

        var decoded = FieldDecode.CreateDecoder_QAngle(encoding)(ref buffer);

        Assert.That(decoded, Is.EqualTo(new QAngle(90.0f, 0.0f, -90.0f)));
    }

    [TestCase(0)]
    [TestCase(32)]
    public void QAngle_QanglePitchYaw_RawFloats(int bitCount)
    {
        var buffer = new BitBuffer(new BitWriter().Float(12.5f).Float(-170.25f).ToArray());

        var decoded = FieldDecode.CreateDecoder_QAngle(Encoding("qangle_pitch_yaw", bitCount))(ref buffer);

        Assert.That(decoded, Is.EqualTo(new QAngle(12.5f, -170.25f, 0.0f)));
    }

    [Test]
    public void QAngle_NoEncoder_ReadsThreeFloats()
    {
        var buffer = new BitBuffer(new BitWriter().Float(1.0f).Float(2.0f).Float(3.0f).ToArray());

        var decoded = FieldDecode.CreateDecoder_QAngle(Encoding(null, 32))(ref buffer);

        Assert.That(decoded, Is.EqualTo(new QAngle(1.0f, 2.0f, 3.0f)));
    }

    [Test]
    public void Fixed32_ReadsRawBits()
    {
        var data = new BitWriter().Bits(0xFFFFFFFE, 32).Bits(0xFFFFFFFE, 32).Bits(5, 8).ToArray();
        var buffer = new BitBuffer(data);

        Assert.That(FieldDecode.CreateDecoder_UInt32(Encoding("fixed32"))(ref buffer), Is.EqualTo(0xFFFFFFFE));
        Assert.That(FieldDecode.CreateDecoder_Int32(Encoding("fixed32"))(ref buffer), Is.EqualTo(-2));
        Assert.That(buffer.ReadUVarInt32(), Is.EqualTo(5));
    }

    [Test]
    public void CoordIntegral_ReadsSignedIntegers()
    {
        var writer = new BitWriter()
            .Bits(0b10, 2).Bits(((5 - 1) << 1) | 1, 14 + 1) // present, coord_integer_bits, -5
            .Bits(0b11, 2).Bits((7 - 1) << 1, 11 + 1)       // present, coord_integer_bits_mp, +7
            .Bits(0b00, 2);                                  // not present -> 0
        var buffer = new BitBuffer(writer.ToArray());
        var decoder = FieldDecode.CreateDecoder_Vector(Encoding("coord_integral"));

        Assert.That(decoder(ref buffer), Is.EqualTo(new Vector(-5.0f, 7.0f, 0.0f)));
        Assert.That(buffer.TellBits, Is.EqualTo(writer.BitCount));
    }

    [Test]
    public void Coord_UsesBitsFromCoordSizeParams()
    {
        var encoding = Encoding("coord") with
        {
            CoordSizeParams = CoordSizeParams.Legacy with { IntegerBits = 10, FractionalBits = 3 }
        };
        // has int, has frac, positive, int = 99 + 1, frac = 4/8
        var writer = new BitWriter().Bit(true).Bit(true).Bit(false).Bits(99, 10).Bits(4, 3);
        var buffer = new BitBuffer(writer.ToArray());

        Assert.That(FieldDecode.CreateDecoder_float(encoding)(ref buffer), Is.EqualTo(100.5f));
    }

    [Test]
    public void CoordSizeParams_InvalidFallsBackToLegacy()
    {
        var proto = new ProtoCoordSizeParams_t
        {
            CoordIntegerBits = 20, CoordFractionalBits = 10, CoordIntegerBitsMp = 11,
            CoordFractionalBitsMp = 3, NormalFractionalBits = 11, AngleBits = 20
        };

        Assert.That(CoordSizeParams.FromProto(proto), Is.EqualTo(CoordSizeParams.Legacy));
        Assert.That(CoordSizeParams.FromProto(null), Is.EqualTo(CoordSizeParams.Legacy));
    }

    [TestCase(2418, 2418u)]
    [TestCase(-1, uint.MaxValue)]
    public void GameTick_IsZigZagEncoded(long tick, uint expected)
    {
        var buffer = new BitBuffer(new BitWriter().ZigZag(tick).ToArray());
        Assert.That(FieldDecode.CreateDecoder_GameTick(Encoding(null))(ref buffer).Value, Is.EqualTo(expected));
    }

    [TestCase(12, 12u)]
    [TestCase(-1, uint.MaxValue)]
    public void CPlayerSlot_IsZigZagEncoded(long slot, uint expected)
    {
        var buffer = new BitBuffer(new BitWriter().ZigZag(slot).ToArray());
        Assert.That(FieldDecode.CreateDecoder_CPlayerSlot(Encoding(null))(ref buffer).Value, Is.EqualTo(expected));
    }

    [Test]
    public void Enum_WithNegativeMembers_IsZigZagEncoded()
    {
        var data = new BitWriter()
            .ZigZag((long)AnimLoopMode.ANIM_LOOP_MODE_USE_SEQUENCE_SETTINGS)
            .ZigZag((long)AnimLoopMode.ANIM_LOOP_MODE_INVALID)
            .ToArray();
        var buffer = new BitBuffer(data);
        var decoder = FieldDecode.CreateDecoder_enum<AnimLoopMode>(Encoding(null));

        Assert.That(decoder(ref buffer), Is.EqualTo(AnimLoopMode.ANIM_LOOP_MODE_USE_SEQUENCE_SETTINGS));
        Assert.That(decoder(ref buffer), Is.EqualTo(AnimLoopMode.ANIM_LOOP_MODE_INVALID));
    }

    [Test]
    public void Enum_IsSignedEnumFromServer_TakesPrecedence()
    {
        var buffer = new BitBuffer(new BitWriter().VarInt(4).ToArray());
        var decoder = FieldDecode.CreateDecoder_enum<AnimLoopMode>(Encoding(null) with { IsSignedEnum = false });

        Assert.That((int)decoder(ref buffer), Is.EqualTo(4));
    }

    [Test]
    public void Quaternion_ReadsFourFloats()
    {
        var buffer = new BitBuffer(new BitWriter().Float(0.1f).Float(0.2f).Float(0.3f).Float(0.9f).ToArray());

        var decoded = FieldDecode.CreateDecoder_Quaternion(Encoding(null, 32))(ref buffer);

        Assert.That(decoded, Is.EqualTo(new Quaternion(0.1f, 0.2f, 0.3f, 0.9f)));
    }

    [Test]
    public void CTransform_NoEncoder_IsPositionPlusAngles()
    {
        var writer = new BitWriter()
            .Float(1.0f).Float(2.0f).Float(3.0f)   // position
            .Float(0.0f).Float(90.0f).Float(0.0f); // angles (pitch, yaw, roll)
        var buffer = new BitBuffer(writer.ToArray());

        var decoded = FieldDecode.CreateDecoder_CTransform(Encoding(null, 32))(ref buffer);

        Assert.That(decoded.Position, Is.EqualTo(new Vector(1.0f, 2.0f, 3.0f)));
        Assert.That(decoded.Orientation.X, Is.EqualTo(0.0f).Within(1e-6f));
        Assert.That(decoded.Orientation.Y, Is.EqualTo(0.0f).Within(1e-6f));
        Assert.That(decoded.Orientation.Z, Is.EqualTo(MathF.Sqrt(0.5f)).Within(1e-6f));
        Assert.That(decoded.Orientation.W, Is.EqualTo(MathF.Sqrt(0.5f)).Within(1e-6f));
    }

    [Test]
    public void CTransform_Coord_IsCoordPositionPlusPresenceBitAngles()
    {
        var writer = new BitWriter()
            .Bit(true).Bit(false).Bit(false).Bits(9, 14) // x = 10
            .Bit(false).Bit(false)                       // y = 0
            .Bit(true).Bit(false).Bit(true).Bits(1, 14)  // z = -2
            .Bit(false).Bit(true).Bit(false)             // only yaw present
            .Bit(true).Bit(false).Bit(false).Bits(179, 14); // yaw = 180
        var buffer = new BitBuffer(writer.ToArray());

        var decoded = FieldDecode.CreateDecoder_CTransform(Encoding("coord"))(ref buffer);

        Assert.That(decoded.Position, Is.EqualTo(new Vector(10.0f, 0.0f, -2.0f)));
        Assert.That(decoded.Orientation.Z, Is.EqualTo(1.0f).Within(1e-6f));
        Assert.That(decoded.Orientation.W, Is.EqualTo(0.0f).Within(1e-6f));
        Assert.That(buffer.TellBits, Is.EqualTo(writer.BitCount));
    }

    [Test]
    public void QuantizedFloat_EncodeIntegers_MatchesEngineRange()
    {
        // e.g. m_flWaterLevel: 8 bits, QFE_ENCODE_INTEGERS_EXACTLY (flags -8), [0, 1].
        // The engine uses a range of 2 here, not 1.
        var qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 8, -8, null, 1.0f));

        Assert.That(qf.Low, Is.EqualTo(0.0f));
        Assert.That(qf.High, Is.EqualTo(2.0f - 2.0f / 256));
        Assert.That(qf.BitCount, Is.EqualTo(8));
    }

    [Test]
    public void QuantizedFloat_RoundUpAndDown_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 8, 3, -4.0f, 4.0f)));
    }
}
