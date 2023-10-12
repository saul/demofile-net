using System.Runtime.InteropServices;

namespace DemoFile.Test.Unit;

[TestFixture]
public class FieldPathEncodingTest
{
    // From: https://github.com/jordanorelli/hyperstone/blob/1ac40a344457a0adc3a297edd52cf47a25edc584/ent/huff_test.go#L7-L51
    // thanks to @spheenik and @invokr for the expected huffman codes. these are
    // ripped from the huffman trees that are known to be working in clarity and
    // manta.

    private static readonly TestCaseData[] DecodeCases = new TestCaseData[]
    {
        // @formatter:off
        new("PlusOne",                                  ToBitStream("0")),
        new("FieldPathEncodeFinish",                    ToBitStream("10")),
        new("PlusTwo",                                  ToBitStream("1110")),
        new("PushOneLeftDeltaNRightNonZeroPack6Bits",   ToBitStream("1111")),
        new("PushOneLeftDeltaOneRightNonZero",          ToBitStream("11000")),
        new("PlusN",                                    ToBitStream("11010")),
        new("PlusThree",                                ToBitStream("110010")),
        new("PopAllButOnePlusOne",                      ToBitStream("110011")),
        new("PushOneLeftDeltaNRightNonZero",            ToBitStream("11011001")),
        new("PushOneLeftDeltaOneRightZero",             ToBitStream("11011010")),
        new("PushOneLeftDeltaNRightZero",               ToBitStream("11011100")),
        new("PopAllButOnePlusNPack6Bits",               ToBitStream("11011110")),
        new("PlusFour",                                 ToBitStream("11011111")),
        new("PopAllButOnePlusN",                        ToBitStream("110110000")),
        new("PushOneLeftDeltaNRightNonZeroPack8Bits",   ToBitStream("110110110")),
        new("NonTopoPenultimatePlusOne",                ToBitStream("110110111")),
        new("PopAllButOnePlusNPack3Bits",               ToBitStream("110111010")),
        new("PushNAndNonTopological",                   ToBitStream("110111011")),
        new("NonTopoComplexPack4Bits",                  ToBitStream("1101100010")),
        new("NonTopoComplex",                           ToBitStream("11011000111")),
        new("PushOneLeftDeltaZeroRightZero",            ToBitStream("110110001101")),
        new("PopOnePlusOne",                            ToBitStream("110110001100001")),
        new("PushOneLeftDeltaZeroRightNonZero",         ToBitStream("110110001100101")),
        new("PopNAndNonTopographical",                  ToBitStream("1101100011000000")),
        new("PopNPlusN",                                ToBitStream("1101100011000001")),
        new("PushN",                                    ToBitStream("1101100011000100")),
        new("PushThreePack5LeftDeltaN",                 ToBitStream("1101100011000101")),
        new("PopNPlusOne",                              ToBitStream("1101100011000110")),
        new("PopOnePlusN",                              ToBitStream("1101100011000111")),
        new("PushTwoLeftDeltaZero",                     ToBitStream("1101100011001000")),
        new("PushThreeLeftDeltaZero",                   ToBitStream("11011000110010010")),
        new("PushTwoPack5LeftDeltaZero",                ToBitStream("11011000110010011")),
        new("PushTwoLeftDeltaN",                        ToBitStream("11011000110011000")),
        new("PushThreePack5LeftDeltaOne",               ToBitStream("11011000110011001")),
        new("PushThreeLeftDeltaN",                      ToBitStream("11011000110011010")),
        new("PushTwoPack5LeftDeltaN",                   ToBitStream("11011000110011011")),
        new("PushTwoLeftDeltaOne",                      ToBitStream("11011000110011100")),
        new("PushThreePack5LeftDeltaZero",              ToBitStream("11011000110011101")),
        new("PushThreeLeftDeltaOne",                    ToBitStream("11011000110011110")),
        new("PushTwoPack5LeftDeltaOne",                 ToBitStream("11011000110011111")),
        // @formatter:on
    };

    [TestCaseSource(nameof(DecodeCases))]
    public void Decode(string expectedName, byte[] encodedBytes)
    {
        var buffer = new BitBuffer(encodedBytes);
        var encodingOp = FieldPathEncoding.ReadFieldPathOp(ref buffer);
        Assert.That(buffer.RemainingBytes, Is.EqualTo(0));
        Assert.That(encodingOp.Name, Is.EqualTo(expectedName));
    }

    [TestCaseSource(nameof(DecodeCases))]
    public void BuildLookupTable(string expectedName, byte[] encodedBytes)
    {
        Span<byte> temp = stackalloc byte[4];
        encodedBytes.CopyTo(temp);
        var encoded = MemoryMarshal.Read<uint>(temp);

        var lookupTable = FieldPathEncoding.HuffmanRoot.BuildLookupTable();

        Assert.That(lookupTable[encoded].Symbol.Name, Is.EqualTo(expectedName));
    }
}
