using DemoFile.Game.Cs;

namespace DemoFile.Test.Unit;

// CS2 build 14197 (2026-11-11) networks 8-bit fields with the "fixed8" var encoder: one raw byte, not a varint.
[TestFixture]
public class FixedWidthEncodingTest
{
    private static readonly FieldEncodingInfo Fixed8 = new("fixed8", 0, 0, null, null);
    private static readonly FieldEncodingInfo Fixed16 = new("fixed16", 0, 0, null, null);
    private static readonly FieldEncodingInfo Varint = new(null, 0, 0, null, null);

    private enum SignedByteEnum : sbyte { MinusOne = -1, One = 1 }
    private enum IntEnum { MinusOne = -1 }

    [Test]
    public void Fixed8_Byte_ConsumesExactlyOneByte()
    {
        // 0x85 has the varint continuation bit set: a varint read would also eat the 0x2A
        var buffer = new BitBuffer(new byte[] { 0x85, 0x2A });
        Assert.That(FieldDecode.CreateDecoder_byte(Fixed8)(ref buffer), Is.EqualTo(0x85));
        Assert.That(buffer.ReadByte(), Is.EqualTo(0x2A));
    }

    [Test]
    public void NoEncoder_Byte_StillReadsVarint()
    {
        var buffer = new BitBuffer(new byte[] { 0x85, 0x07, 0x2A });
        Assert.That(FieldDecode.CreateDecoder_byte(Varint)(ref buffer), Is.EqualTo(unchecked((byte)0x385)));
        Assert.That(buffer.ReadByte(), Is.EqualTo(0x2A));
    }

    [Test]
    public void Fixed8_SignedTypes_SignExtend()
    {
        var buffer = new BitBuffer(new byte[] { 0xFF, 0x80, 0x7F, 0x2A });
        Assert.That(FieldDecode.CreateDecoder_sbyte(Fixed8)(ref buffer), Is.EqualTo((sbyte)-1));
        Assert.That(FieldDecode.CreateDecoder_Int32(Fixed8)(ref buffer), Is.EqualTo(-128));
        Assert.That(FieldDecode.CreateDecoder_Int16(Fixed8)(ref buffer), Is.EqualTo((short)127));
        Assert.That(buffer.ReadByte(), Is.EqualTo(0x2A));
    }

    [Test]
    public void Fixed8_UnsignedTypes_AreRaw()
    {
        var buffer = new BitBuffer(new byte[] { 0x80, 0xFF, 0x90, 0x2A });
        Assert.That(FieldDecode.CreateDecoder_UInt32(Fixed8)(ref buffer), Is.EqualTo(128u));
        Assert.That(FieldDecode.CreateDecoder_UInt16(Fixed8)(ref buffer), Is.EqualTo((ushort)255));
        Assert.That(FieldDecode.CreateDecoder_UInt64(Fixed8)(ref buffer), Is.EqualTo(0x90ul));
        Assert.That(buffer.ReadByte(), Is.EqualTo(0x2A));
    }

    [Test]
    public void Fixed16_ReadsTwoBytesLittleEndian()
    {
        var buffer = new BitBuffer(new byte[] { 0x34, 0x92, 0x2A });
        Assert.That(FieldDecode.CreateDecoder_UInt16(Fixed16)(ref buffer), Is.EqualTo((ushort)0x9234));
        Assert.That(buffer.ReadByte(), Is.EqualTo(0x2A));

        buffer = new BitBuffer(new byte[] { 0x34, 0x92, 0x2A });
        Assert.That(FieldDecode.CreateDecoder_Int16(Fixed16)(ref buffer), Is.EqualTo(unchecked((short)0x9234)));
        Assert.That(buffer.ReadByte(), Is.EqualTo(0x2A));
    }

    [Test]
    public void Fixed8_Enums()
    {
        // Generated 8-bit enums such as MoveType are `: byte`
        var buffer = new BitBuffer(new byte[] { 0x09, 0x85, 0xFF, 0xFF, 0x2A });
        Assert.That(FieldDecode.CreateDecoder_enum<MoveType>(Fixed8)(ref buffer), Is.EqualTo((MoveType)9));
        Assert.That(FieldDecode.CreateDecoder_enum<MoveType>(Fixed8)(ref buffer), Is.EqualTo((MoveType)0x85));
        Assert.That(FieldDecode.CreateDecoder_enum<SignedByteEnum>(Fixed8)(ref buffer), Is.EqualTo(SignedByteEnum.MinusOne));
        Assert.That(FieldDecode.CreateDecoder_enum<IntEnum>(Fixed8)(ref buffer), Is.EqualTo(IntEnum.MinusOne));
        Assert.That(buffer.ReadByte(), Is.EqualTo(0x2A));
    }

    [Test]
    public void Fixed8_Bool_ConsumesOneByte()
    {
        var buffer = new BitBuffer(new byte[] { 0x01, 0x00, 0x2A });
        Assert.That(FieldDecode.CreateDecoder_bool(Fixed8)(ref buffer), Is.True);
        Assert.That(FieldDecode.CreateDecoder_bool(Fixed8)(ref buffer), Is.False);
        Assert.That(buffer.ReadByte(), Is.EqualTo(0x2A));
    }

    [Test]
    public void Fixed8_AttachmentHandle()
    {
        var buffer = new BitBuffer(new byte[] { 0x05, 0x2A });
        Assert.That(AttachmentHandle.CreateDecoder(Fixed8)(ref buffer), Is.EqualTo(new AttachmentHandle(5)));
        Assert.That(buffer.ReadByte(), Is.EqualTo(0x2A));
    }

    [Test]
    public void Fixed8_FallbackInteger_ConsumesOneByte()
    {
        var fieldType = FieldType.Parse("uint8");
        Assert.That(FallbackDecoder.TryCreate("m_test", fieldType, Fixed8, null!, out var decoder), Is.True);
        var buffer = new BitBuffer(new byte[] { 0x85, 0x2A });
        decoder!(default, new[] { 0 }, ref buffer);
        Assert.That(buffer.ReadByte(), Is.EqualTo(0x2A));
    }
}
