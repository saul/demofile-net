using System.Diagnostics;

namespace DemoFile;

internal delegate void FieldPathReader(ref BitBuffer buffer, ref FieldPath fieldPath);

internal record FieldPathEncodingOp(string Name, int Frequency, FieldPathReader? Reader)
{
    public override string ToString() => Name;
}

internal static class FieldPathEncoding
{
    internal static readonly HuffmanNode<FieldPathEncodingOp> HuffmanRoot;
    private static readonly int MaxEncodedBits;
    private static readonly (FieldPathEncodingOp Symbol, int NumBits)[] HuffmanLookup;

    static FieldPathEncoding()
    {
        var encodingOps = new FieldPathEncodingOp[]
        {
            new("PlusOne", 36271, (ref BitBuffer buffer, ref FieldPath path) => { path[^1] += 1; }),
            new("PlusTwo", 10334, (ref BitBuffer buffer, ref FieldPath path) => { path[^1] += 2; }),
            new("PlusThree", 1375, (ref BitBuffer buffer, ref FieldPath path) => { path[^1] += 3; }),
            new("PlusFour", 646, (ref BitBuffer buffer, ref FieldPath path) => { path[^1] += 4; }),
            new("PlusN", 4128,
                (ref BitBuffer buffer, ref FieldPath path) => { path[^1] += buffer.ReadUBitVarFieldPath() + 5; }),
            new("PushOneLeftDeltaZeroRightZero", 35,
                (ref BitBuffer buffer, ref FieldPath path) => { path.Add(0); }),
            new("PushOneLeftDeltaZeroRightNonZero", 3,
                (ref BitBuffer buffer, ref FieldPath path) => { path.Add(buffer.ReadUBitVarFieldPath()); }),
            new("PushOneLeftDeltaOneRightZero", 521, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += 1;
                path.Add(0);
            }),
            new("PushOneLeftDeltaOneRightNonZero", 2942, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += 1;
                path.Add(buffer.ReadUBitVarFieldPath());
            }),
            new("PushOneLeftDeltaNRightZero", 560, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += buffer.ReadUBitVarFieldPath();
                path.Add(0);
            }),
            new("PushOneLeftDeltaNRightNonZero", 471, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += buffer.ReadUBitVarFieldPath() + 2;
                path.Add(buffer.ReadUBitVarFieldPath() + 1);
            }),
            new("PushOneLeftDeltaNRightNonZeroPack6Bits", 10530, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += (int)buffer.ReadUBits(3) + 2;
                path.Add((int)buffer.ReadUBits(3) + 1);
            }),
            new("PushOneLeftDeltaNRightNonZeroPack8Bits", 251, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += (int)buffer.ReadUBits(4) + 2;
                path.Add((int)buffer.ReadUBits(4) + 1);
            }),
            new("PushTwoLeftDeltaZero", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
            }),
            new("PushTwoPack5LeftDeltaZero", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Add((int)buffer.ReadUBits(5));
                path.Add((int)buffer.ReadUBits(5));
            }),
            new("PushThreeLeftDeltaZero", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
            }),
            new("PushThreePack5LeftDeltaZero", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Add((int)buffer.ReadUBits(5));
                path.Add((int)buffer.ReadUBits(5));
                path.Add((int)buffer.ReadUBits(5));
            }),
            new("PushTwoLeftDeltaOne", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += 1;
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
            }),
            new("PushTwoPack5LeftDeltaOne", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += 1;
                path.Add((int)buffer.ReadUBits(5));
                path.Add((int)buffer.ReadUBits(5));
            }),
            new("PushThreeLeftDeltaOne", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += 1;
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
            }),
            new("PushThreePack5LeftDeltaOne", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += 1;
                path.Add((int)buffer.ReadUBits(5));
                path.Add((int)buffer.ReadUBits(5));
                path.Add((int)buffer.ReadUBits(5));
            }),
            new("PushTwoLeftDeltaN", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += (int)buffer.ReadUBitVar() + 2;
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
            }),
            new("PushTwoPack5LeftDeltaN", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += (int)buffer.ReadUBitVar() + 2;
                path.Add((int)buffer.ReadUBits(5));
                path.Add((int)buffer.ReadUBits(5));
            }),
            new("PushThreeLeftDeltaN", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += (int)buffer.ReadUBitVar() + 2;
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
                path.Add(buffer.ReadUBitVarFieldPath());
            }),
            new("PushThreePack5LeftDeltaN", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path[^1] += (int)buffer.ReadUBitVar() + 2;
                path.Add((int)buffer.ReadUBits(5));
                path.Add((int)buffer.ReadUBits(5));
                path.Add((int)buffer.ReadUBits(5));
            }),
            new("PushN", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                var count = (int)buffer.ReadUBitVar();
                path[^1] += (int)buffer.ReadUBitVar();
                for (var i = 0; i < count; ++i)
                {
                    path.Add(buffer.ReadUBitVarFieldPath());
                }
            }),
            new("PushNAndNonTopological", 310, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                for (var i = 0; i < path.Count; ++i)
                {
                    if (buffer.ReadOneBit())
                    {
                        path[i] += buffer.ReadVarInt32() + 1;
                    }
                }

                var count = (int)buffer.ReadUBitVar();
                for (var i = 0; i < count; ++i)
                {
                    path.Add(buffer.ReadUBitVarFieldPath());
                }
            }),
            new("PopOnePlusOne", 2, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Pop(1);
                path[^1] += 1;
            }),
            new("PopOnePlusN", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Pop(1);
                path[^1] += buffer.ReadUBitVarFieldPath() + 1;
            }),
            new("PopAllButOnePlusOne", 1837, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Pop(path.Count - 1);
                path[0] += 1;
            }),
            new("PopAllButOnePlusN", 149, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Pop(path.Count - 1);
                path[0] += buffer.ReadUBitVarFieldPath() + 1;
            }),
            new("PopAllButOnePlusNPack3Bits", 300, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Pop(path.Count - 1);
                path[0] += (int)buffer.ReadUBits(3) + 1;
            }),
            new("PopAllButOnePlusNPack6Bits", 634, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Pop(path.Count - 1);
                path[0] += (int)buffer.ReadUBits(6) + 1;
            }),
            new("PopNPlusOne", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Pop(buffer.ReadUBitVarFieldPath());
                path[^1] += 1;
            }),
            new("PopNPlusN", 0, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Pop(buffer.ReadUBitVarFieldPath());
                path[^1] += buffer.ReadVarInt32();
            }),
            new("PopNAndNonTopographical", 1, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                path.Pop(buffer.ReadUBitVarFieldPath());
                for (var i = 0; i < path.Count; ++i)
                {
                    if (buffer.ReadOneBit())
                    {
                        path[i] += buffer.ReadVarInt32();
                    }
                }
            }),
            new("NonTopoComplex", 76, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                for (var i = 0; i < path.Count; ++i)
                {
                    if (buffer.ReadOneBit())
                    {
                        path[i] += buffer.ReadVarInt32();
                    }
                }
            }),
            new("NonTopoPenultimatePlusOne", 271, (ref BitBuffer buffer, ref FieldPath path) => { path[^2] += 1; }),
            new("NonTopoComplexPack4Bits", 99, (ref BitBuffer buffer, ref FieldPath path) =>
            {
                for (var i = 0; i < path.Count; ++i)
                {
                    if (buffer.ReadOneBit())
                    {
                        path[i] += (int)buffer.ReadUBits(4) - 7;
                    }
                }
            }),
            new("FieldPathEncodeFinish", 25474, null)
        };
        
        HuffmanRoot = HuffmanNode<FieldPathEncodingOp>.Build(encodingOps.Select(op => new KeyValuePair<FieldPathEncodingOp, int>(op, op.Frequency)));

        var table = HuffmanRoot.BuildLookupTable();
        MaxEncodedBits = table.MaxBy(kvp => kvp.Value.NumBits).Value.NumBits;
        HuffmanLookup = new (FieldPathEncodingOp Symbol, int NumBits)[1 << MaxEncodedBits];
        foreach (var (encoded, value) in table)
        {
            if (value.NumBits == MaxEncodedBits)
            {
                Debug.Assert(HuffmanLookup[(int)encoded].NumBits == 0);
                HuffmanLookup[(int)encoded] = value;
            }
            else
            {
                var lengthDiff = MaxEncodedBits - value.NumBits;
                for (var i = 0; i < (1 << lengthDiff); ++i)
                {
                    var index = (int)encoded | (i << value.NumBits);
                    Debug.Assert(HuffmanLookup[index].NumBits == 0);
                    HuffmanLookup[index] = value;
                }
            }
        }
    }

    public static bool UseLookup = true;

    public static FieldPathEncodingOp ReadFieldPathOp(ref BitBuffer buffer)
    {
        // todo: implement peek on BitBuffer and build a lookup table of symbols
        if (UseLookup)
        {
            var symbol = buffer.PeekUBits(MaxEncodedBits);
            var (encodingOp, length) = HuffmanLookup[symbol];
            Debug.Assert(length > 0);
            buffer.ReadUBits(length);
            return encodingOp!;
        }
        else
        {
            var node = HuffmanRoot;
            for (;;)
            {
                var next =
                    (buffer.ReadOneBit()
                        ? node.Right
                        : node.Left)
                    ?? throw new InvalidOperationException("Invalid field path encoding");

                // Is this node a leaf?
                if (next.Symbol is { } encodingOp)
                    return encodingOp;

                node = next;
            }
        }
    }
}
