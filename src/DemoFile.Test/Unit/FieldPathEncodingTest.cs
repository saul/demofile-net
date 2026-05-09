using System.Text;

namespace DemoFile.Test.Unit;

internal record FieldPathEncodingOp(string Name, int Frequency, string? Reader)
{
    public override string ToString() => Name;
}

[TestFixture]
public class FieldPathEncodingTest
{
    // Huffman codes from: https://github.com/jordanorelli/hyperstone/blob/1ac40a344457a0adc3a297edd52cf47a25edc584/ent/huff_test.go#L7-L51
    // Columns: huffman bit string | extra elements to append to FieldPath.Default before the call (null = none) | expected return | expected path after the op
    // Data reads in the op body consume zeros from the padding added by PaddedBitStream.
    private static readonly TestCaseData[] DecodeCases =
    [
        // @formatter:off
        new TestCaseData("0",                    null,          true,  new[] { 0 })            .SetName("PlusOne"),
        new TestCaseData("10",                   null,          false, new[] { -1 })           .SetName("FieldPathEncodeFinish"),
        new TestCaseData("1110",                 null,          true,  new[] { 1 })            .SetName("PlusTwo"),
        new TestCaseData("1111",                 null,          true,  new[] { 1, 1 })         .SetName("PushOneLeftDeltaNRightNonZeroPack6Bits"),
        new TestCaseData("11000",                null,          true,  new[] { 0, 0 })         .SetName("PushOneLeftDeltaOneRightNonZero"),
        new TestCaseData("11010",                null,          true,  new[] { 4 })            .SetName("PlusN"),
        new TestCaseData("110010",               null,          true,  new[] { 2 })            .SetName("PlusThree"),
        new TestCaseData("110011",               null,          true,  new[] { 0 })            .SetName("PopAllButOnePlusOne"),
        new TestCaseData("11011001",             null,          true,  new[] { 1, 1 })         .SetName("PushOneLeftDeltaNRightNonZero"),
        new TestCaseData("11011010",             null,          true,  new[] { 0, 0 })         .SetName("PushOneLeftDeltaOneRightZero"),
        new TestCaseData("11011100",             null,          true,  new[] { -1, 0 })        .SetName("PushOneLeftDeltaNRightZero"),
        new TestCaseData("11011110",             null,          true,  new[] { 0 })            .SetName("PopAllButOnePlusNPack6Bits"),
        new TestCaseData("11011111",             null,          true,  new[] { 3 })            .SetName("PlusFour"),
        new TestCaseData("110110000",            null,          true,  new[] { 0 })            .SetName("PopAllButOnePlusN"),
        new TestCaseData("110110110",            null,          true,  new[] { 1, 1 })         .SetName("PushOneLeftDeltaNRightNonZeroPack8Bits"),
        new TestCaseData("110110111",            new[] { 0 },   true,  new[] { 0, 0 })         .SetName("NonTopoPenultimatePlusOne"),
        new TestCaseData("110111010",            null,          true,  new[] { 0 })            .SetName("PopAllButOnePlusNPack3Bits"),
        new TestCaseData("110111011",            null,          true,  new[] { -1 })           .SetName("PushNAndNonTopological"),
        new TestCaseData("1101100010",           null,          true,  new[] { -1 })           .SetName("NonTopoComplexPack4Bits"),
        new TestCaseData("11011000111",          null,          true,  new[] { -1 })           .SetName("NonTopoComplex"),
        new TestCaseData("110110001101",         null,          true,  new[] { -1, 0 })        .SetName("PushOneLeftDeltaZeroRightZero"),
        new TestCaseData("110110001100001",      new[] { 0 },   true,  new[] { 0 })            .SetName("PopOnePlusOne"),
        new TestCaseData("110110001100101",      null,          true,  new[] { -1, 0 })        .SetName("PushOneLeftDeltaZeroRightNonZero"),
        new TestCaseData("1101100011000000",     null,          true,  new[] { -1 })           .SetName("PopNAndNonTopographical"),
        new TestCaseData("1101100011000001",     null,          true,  new[] { -1 })           .SetName("PopNPlusN"),
        new TestCaseData("1101100011000100",     null,          true,  new[] { -1 })           .SetName("PushN"),
        new TestCaseData("1101100011000101",     null,          true,  new[] { 1, 0, 0, 0 })   .SetName("PushThreePack5LeftDeltaN"),
        new TestCaseData("1101100011000110",     null,          true,  new[] { 0 })            .SetName("PopNPlusOne"),
        new TestCaseData("1101100011000111",     new[] { 0 },   true,  new[] { 0 })            .SetName("PopOnePlusN"),
        new TestCaseData("1101100011001000",     null,          true,  new[] { -1, 0, 0 })     .SetName("PushTwoLeftDeltaZero"),
        new TestCaseData("11011000110010010",    null,          true,  new[] { -1, 0, 0, 0 })  .SetName("PushThreeLeftDeltaZero"),
        new TestCaseData("11011000110010011",    null,          true,  new[] { -1, 0, 0 })     .SetName("PushTwoPack5LeftDeltaZero"),
        new TestCaseData("11011000110011000",    null,          true,  new[] { 1, 0, 0 })      .SetName("PushTwoLeftDeltaN"),
        new TestCaseData("11011000110011001",    null,          true,  new[] { 0, 0, 0, 0 })   .SetName("PushThreePack5LeftDeltaOne"),
        new TestCaseData("11011000110011010",    null,          true,  new[] { 1, 0, 0, 0 })   .SetName("PushThreeLeftDeltaN"),
        new TestCaseData("11011000110011011",    null,          true,  new[] { 1, 0, 0 })      .SetName("PushTwoPack5LeftDeltaN"),
        new TestCaseData("11011000110011100",    null,          true,  new[] { 0, 0, 0 })      .SetName("PushTwoLeftDeltaOne"),
        new TestCaseData("11011000110011101",    null,          true,  new[] { -1, 0, 0, 0 })  .SetName("PushThreePack5LeftDeltaZero"),
        new TestCaseData("11011000110011110",    null,          true,  new[] { 0, 0, 0, 0 })   .SetName("PushThreeLeftDeltaOne"),
        new TestCaseData("11011000110011111",    null,          true,  new[] { 0, 0, 0 })      .SetName("PushTwoPack5LeftDeltaOne"),
        // @formatter:on
    ];

    [TestCaseSource(nameof(DecodeCases))]
    public void Decode(string huffmanBits, int[]? extraInitialElements, bool expectedReturn, int[] expectedPath)
    {
        var buffer = new BitBuffer(PaddedBitStream(huffmanBits));
        var fieldPath = FieldPath.Default;
        if (extraInitialElements != null)
            foreach (var e in extraInitialElements)
                fieldPath.Add(e);

        var result = FieldPathEncoding.ReadFieldPathOp(ref buffer, ref fieldPath);

        Assert.That(result, Is.EqualTo(expectedReturn));
        Assert.That(PathToArray(fieldPath), Is.EqualTo(expectedPath));
    }

    private static byte[] PaddedBitStream(string bits) =>
        ToBitStream(bits).Concat(new byte[16]).ToArray();

    private static int[] PathToArray(FieldPath path)
    {
        var result = new int[path.Count];
        for (var i = 0; i < path.Count; i++)
            result[i] = path[i];
        return result;
    }

    internal static readonly HuffmanNode<FieldPathEncodingOp> HuffmanTree;

    static FieldPathEncodingTest()
    {
        var encodingOps = new FieldPathEncodingOp[]
        {
            new("PlusOne", 36271,
                """
                path[^1] += 1;
                """),
            new("PlusTwo", 10334,
                """
                path[^1] += 2;
                """),
            new("PlusThree", 1375,
                """
                path[^1] += 3;
                """),
            new("PlusFour", 646,
                """
                path[^1] += 4;
                """),
            new("PlusN", 4128,
                """
                path[^1] += buffer.ReadUBitVarFieldPath() + 5;
                """),
            new("PushOneLeftDeltaZeroRightZero", 35,
                """
                path.Add(0);
                """),
            new("PushOneLeftDeltaZeroRightNonZero", 3,
                """
                path.Add(buffer.ReadUBitVarFieldPath());
                """),
            new("PushOneLeftDeltaOneRightZero", 521,
                """
                path[^1] += 1;
                path.Add(0);
                """),
            new("PushOneLeftDeltaOneRightNonZero", 2942,
                """
                path[^1] += 1;
                path.Add(buffer.ReadUBitVarFieldPath());
                """),
            new("PushOneLeftDeltaNRightZero", 560,
                """
                path[^1] += buffer.ReadUBitVarFieldPath();
                path.Add(0);
                """),
            new("PushOneLeftDeltaNRightNonZero", 471,
                """
                path[^1] += buffer.ReadUBitVarFieldPath() + 2;
                path.Add(buffer.ReadUBitVarFieldPath() + 1);
                """),
            new("PushOneLeftDeltaNRightNonZeroPack6Bits", 10530,
                """
                path[^1] += (int) buffer.ReadUBits(3) + 2;
                path.Add((int) buffer.ReadUBits(3) + 1);
                """),
            new("PushOneLeftDeltaNRightNonZeroPack8Bits", 251,
                """
                path[^1] += (int) buffer.ReadUBits(4) + 2;
                path.Add((int) buffer.ReadUBits(4) + 1);
                """),
            new("PushTwoLeftDeltaZero", 0,
                """
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
                """),
            new("PushTwoPack5LeftDeltaZero", 0,
                """
                path.Add((int) buffer.ReadUBits(5));
                path.Add((int) buffer.ReadUBits(5));
                """),
            new("PushThreeLeftDeltaZero", 0,
                """
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
                """),
            new("PushThreePack5LeftDeltaZero", 0,
                """
                path.Add((int) buffer.ReadUBits(5));
                path.Add((int) buffer.ReadUBits(5));
                path.Add((int) buffer.ReadUBits(5));
                """),
            new("PushTwoLeftDeltaOne", 0,
                """
                path[^1] += 1;
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
                """),
            new("PushTwoPack5LeftDeltaOne", 0,
                """
                path[^1] += 1;
                path.Add((int) buffer.ReadUBits(5));
                path.Add((int) buffer.ReadUBits(5));
                """),
            new("PushThreeLeftDeltaOne", 0,
                """
                path[^1] += 1;
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
                """),
            new("PushThreePack5LeftDeltaOne", 0,
                """
                path[^1] += 1;
                path.Add((int) buffer.ReadUBits(5));
                path.Add((int) buffer.ReadUBits(5));
                path.Add((int) buffer.ReadUBits(5));
                """),
            new("PushTwoLeftDeltaN", 0,
                """
                path[^1] += (int) buffer.ReadUBitVar() + 2;
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
                """),
            new("PushTwoPack5LeftDeltaN", 0,
                """
                path[^1] += (int) buffer.ReadUBitVar() + 2;
                path.Add((int) buffer.ReadUBits(5));
                path.Add((int) buffer.ReadUBits(5));
                """),
            new("PushThreeLeftDeltaN", 0,
                """
                path[^1] += (int) buffer.ReadUBitVar() + 2;
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
                """),
            new("PushThreePack5LeftDeltaN", 0,
                """
                path[^1] += (int) buffer.ReadUBitVar() + 2;
                path.Add((int) buffer.ReadUBits(5));
                path.Add((int) buffer.ReadUBits(5));
                path.Add((int) buffer.ReadUBits(5));
                """),
            new("PushN", 0,
                """
                var count = (int) buffer.ReadUBitVar();
                path[^1] += (int) buffer.ReadUBitVar();
                for (var i = 0; i < count; ++i)
                {
                    path.Add(buffer.ReadUBitVarFieldPath());
                }
                """),
            new("PushNAndNonTopological", 310,
                """
                for (var i = 0; i < path.Count; ++i)
                {
                    if (buffer.ReadOneBit())
                    {
                        path[i] += buffer.ReadVarInt32() + 1;
                    }
                }

                var count = (int) buffer.ReadUBitVar();
                for (var i = 0; i < count; ++i)
                {
                    path.Add(buffer.ReadUBitVarFieldPath());
                }
                """),
            new("PopOnePlusOne", 2,
                """
                path.Pop(1);
                path[^1] += 1;
                """),
            new("PopOnePlusN", 0,
                """
                path.Pop(1);
                path[^1] += buffer.ReadUBitVarFieldPath() + 1;
                """),
            new("PopAllButOnePlusOne", 1837,
                """
                path.Pop(path.Count - 1);
                path[0] += 1;
                """),
            new("PopAllButOnePlusN", 149,
                """
                path.Pop(path.Count - 1);
                path[0] += buffer.ReadUBitVarFieldPath() + 1;
                """),
            new("PopAllButOnePlusNPack3Bits", 300,
                """
                path.Pop(path.Count - 1);
                path[0] += (int) buffer.ReadUBits(3) + 1;
                """),
            new("PopAllButOnePlusNPack6Bits", 634,
                """
                path.Pop(path.Count - 1);
                path[0] += (int) buffer.ReadUBits(6) + 1;
                """),
            new("PopNPlusOne", 0,
                """
                path.Pop(buffer.ReadUBitVarFieldPath());
                path[^1] += 1;
                """),
            new("PopNPlusN", 0,
                """
                path.Pop(buffer.ReadUBitVarFieldPath());
                path[^1] += buffer.ReadVarInt32();
                """),
            new("PopNAndNonTopographical", 1,
                """
                path.Pop(buffer.ReadUBitVarFieldPath());
                for (var i = 0; i < path.Count; ++i)
                {
                    if (buffer.ReadOneBit())
                    {
                        path[i] += buffer.ReadVarInt32();
                    }
                }
                """),
            new("NonTopoComplex", 76,
                """
                for (var i = 0; i < path.Count; ++i)
                {
                    if (buffer.ReadOneBit())
                    {
                        path[i] += buffer.ReadVarInt32();
                    }
                }
                """),
            new("NonTopoPenultimatePlusOne", 271,
                """
                path[^2] += 1;
                """),
            new("NonTopoComplexPack4Bits", 99,
                """
                for (var i = 0; i < path.Count; ++i)
                {
                    if (buffer.ReadOneBit())
                    {
                        path[i] += (int) buffer.ReadUBits(4) - 7;
                    }
                }
                """),
            new("FieldPathEncodeFinish", 25474, null)
        };

        HuffmanTree =
            HuffmanNode<FieldPathEncodingOp>.Build(encodingOps.Select(op =>
                new KeyValuePair<FieldPathEncodingOp, int>(op, op.Frequency)));
    }

    [Test, Explicit]
    public void RegenerateSource()
    {
        var baseDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (baseDir != null)
        {
            if (Directory.Exists(Path.Combine(baseDir.FullName, "src")))
                break;

            baseDir = baseDir.Parent;
        }

        Assert.That(baseDir, Is.Not.Null);

        var functions = new List<KeyValuePair<string, string>>();
        var result = GenerateForNode(HuffmanTree, "");

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine();
        sb.AppendLine("namespace DemoFile;");
        sb.AppendLine();
        sb.AppendLine("internal static class FieldPathEncoding");
        sb.AppendLine("{");
        sb.AppendLine("    public static bool ReadFieldPathOp(ref BitBuffer buffer, ref FieldPath path)");
        sb.AppendLine("    {");
        sb.AppendLine("        " + result.ReplaceLineEndings("\n        "));
        sb.AppendLine("    }");

        foreach (var (name, reader) in functions)
        {
            sb.AppendLine();

            // perf: benchmarkdotnet 1.904 s (with aggressive inlining) vs 1.918 s (without)
            sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"    private static void {name}(ref BitBuffer buffer, ref FieldPath path)");
            sb.AppendLine("    {");
            sb.AppendLine("        " + reader.ReplaceLineEndings("\n        "));
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        var path =  Path.Combine(baseDir.FullName, "src", "DemoFile", "FieldPathEncoding.g.cs");
        File.WriteAllText(path, sb.ToString().ReplaceLineEndings());

        string GenerateForNode(HuffmanNode<FieldPathEncodingOp> node, string path)
        {
            var sb = new StringBuilder();

            if (node.Symbol is { } symbol)
            {
                sb.AppendLine($"// Frequency: {symbol.Frequency}");
                sb.AppendLine($"// Bit string: {path}");
                if (!string.IsNullOrEmpty(symbol.Reader))
                {
                    sb.AppendLine($"{symbol.Name}(ref buffer, ref path);");
                    functions.Add(new KeyValuePair<string, string>(symbol.Name, symbol.Reader));
                }

                sb.AppendLine($"return {(symbol.Reader == null ? "false" : "true")};");
            }
            else
            {
                sb.AppendLine("if (!buffer.ReadOneBit())");
                sb.AppendLine("{");
                sb.AppendLine($"    {GenerateForNode(node.Left!, path + "0").ReplaceLineEndings("\n    ")}");
                sb.AppendLine("}");
                sb.AppendLine("else");
                sb.AppendLine("{");
                sb.AppendLine($"    {GenerateForNode(node.Right!, path + "1").ReplaceLineEndings("\n    ")}");
                sb.AppendLine("}");
            }

            return sb.ToString().TrimEnd();
        }
    }
}