using DemoFile.Game.Cs;

namespace DemoFile.Test.Unit;

[TestFixture]
public class FieldDecodeTest
{
    private static readonly FieldEncodingInfo Varint = new(null, 0, 0, null, null);
    private static readonly FieldEncodingInfo Fixed8 = new("fixed8", 0, 0, null, null);

    // 0xC8 (200) has the varint continuation bit set, so a varint reader would consume
    // the following 0x05 too. A fixed8 reader must stop after one byte.
    private static readonly byte[] Fixed8ThenVarint = [0xC8, 0x05];

    [Test]
    public void Byte_Fixed8_ReadsSingleByte()
    {
        var decoder = FieldDecode.CreateDecoder_byte(Fixed8);
        var buffer = new BitBuffer(Fixed8ThenVarint);

        Assert.That(decoder(ref buffer), Is.EqualTo(200));
        Assert.That(buffer.ReadUVarInt32(), Is.EqualTo(5));
    }

    [Test]
    public void Sbyte_Fixed8_ReadsTwosComplement()
    {
        var decoder = FieldDecode.CreateDecoder_sbyte(Fixed8);
        var buffer = new BitBuffer(new byte[] { 0xFF, 0x05 });

        Assert.That(decoder(ref buffer), Is.EqualTo(-1));
        Assert.That(buffer.ReadUVarInt32(), Is.EqualTo(5));
    }

    [Test]
    public void Enum_Fixed8_ReadsSingleByte()
    {
        var decoder = FieldDecode.CreateDecoder_enum<RenderMode>(Fixed8);
        var buffer = new BitBuffer(Fixed8ThenVarint);

        Assert.That((byte)decoder(ref buffer), Is.EqualTo(200));
        Assert.That(buffer.ReadUVarInt32(), Is.EqualTo(5));
    }

    [Test]
    public void BoxedPrimitive_Fixed8_ReadsSingleByte()
    {
        // AttachmentHandle_t wraps a uint8, so values >= 128 must stay positive
        var decoder = AttachmentHandle.CreateDecoder(Fixed8);
        var buffer = new BitBuffer(Fixed8ThenVarint);

        Assert.That(decoder(ref buffer), Is.EqualTo(new AttachmentHandle(200)));
        Assert.That(buffer.ReadUVarInt32(), Is.EqualTo(5));
    }

    [Test]
    public void Byte_NoEncoder_ReadsVarint()
    {
        // Demos before CS2 build 14197 have no encoder on 8-bit fields
        var decoder = FieldDecode.CreateDecoder_byte(Varint);
        var buffer = new BitBuffer(new byte[] { 0xC8, 0x01 });

        Assert.That(decoder(ref buffer), Is.EqualTo(200));
    }

    [Test]
    public void UnknownEncoder_Throws()
    {
        var unknown = new FieldEncodingInfo("fixed16", 0, 0, null, null);

        Assert.Throws<Exception>(() => FieldDecode.CreateDecoder_byte(unknown));
        Assert.Throws<Exception>(() => AttachmentHandle.CreateDecoder(unknown));
    }
}
