namespace DemoFile.Test.Unit;

[TestFixture]
public class BitBufferTest
{
    [Test]
    public void Read3BitsAtATime()
    {
        var buffer = new BitBuffer(ToBitStream("010101010110110010110011"));

        // Note the endianness is flipped on 0b011
        // That value would be read from a stream of [1, 1, 0] bits

        Assert.That(buffer.ReadUBits(3), Is.EqualTo(0b010));
        Assert.That(buffer.ReadUBits(3), Is.EqualTo(0b101));
        Assert.That(buffer.ReadUBits(3), Is.EqualTo(0b010));
        Assert.That(buffer.ReadUBits(3), Is.EqualTo(0b011));
        Assert.That(buffer.ReadUBits(3), Is.EqualTo(0b011));
        Assert.That(buffer.ReadUBits(3), Is.EqualTo(0b010));
        Assert.That(buffer.ReadUBits(3), Is.EqualTo(0b011));
        Assert.That(buffer.ReadUBits(3), Is.EqualTo(0b110));
    }
}
