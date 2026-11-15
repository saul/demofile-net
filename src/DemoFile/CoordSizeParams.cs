namespace DemoFile;

/// <summary>
/// Bit widths used by the <c>coord</c>, <c>coord_integral</c>, <c>normal</c>, <c>qangle</c> and
/// <c>qangle_precise</c> field encoders.
/// </summary>
/// <remarks>
/// Since CS2 patch 14197 these are sent by the server in <see cref="CSVCMsg_FlattenedSerializer.CoordSizeParams"/>.
/// Older demos don't carry them, and the engine falls back to legacy values (see <see cref="Legacy"/>).
/// </remarks>
public readonly record struct CoordSizeParams(
    int IntegerBits,
    int FractionalBits,
    int IntegerBitsMp,
    int FractionalBitsMp,
    int NormalFractionalBits,
    int AngleBits)
{
    /// <summary>
    /// Values used before the server sent them explicitly (Source's COORD_INTEGER_BITS etc.).
    /// These are also the values sent by CS2 patch 14197.
    /// </summary>
    public static CoordSizeParams Legacy { get; } = new(
        IntegerBits: 14,
        FractionalBits: 5,
        IntegerBitsMp: 11,
        FractionalBitsMp: 3,
        NormalFractionalBits: 11,
        AngleBits: 20);

    /// <summary>
    /// Mirrors the engine's validation: every value must be in [1, 31] and a coord must fit in 32 bits.
    /// The engine rejects anything else and keeps its defaults.
    /// </summary>
    public bool IsValid =>
        IntegerBits is >= 1 and <= 31
        && FractionalBits is >= 1 and <= 31
        && IntegerBitsMp is >= 1 and <= 31
        && FractionalBitsMp is >= 1 and <= 31
        && NormalFractionalBits is >= 1 and <= 31
        && AngleBits is >= 1 and <= 31
        && Math.Max(IntegerBits, IntegerBitsMp) + Math.Max(FractionalBits, FractionalBitsMp) + 3 <= 32;

    /// <summary>
    /// Resolve the params to use for a set of serializers, falling back to <see cref="Legacy"/>
    /// when they are absent (pre-14197 demos) or invalid.
    /// </summary>
    public static CoordSizeParams FromProto(ProtoCoordSizeParams_t? proto)
    {
        if (proto == null)
            return Legacy;

        var result = new CoordSizeParams(
            IntegerBits: proto.CoordIntegerBits,
            FractionalBits: proto.CoordFractionalBits,
            IntegerBitsMp: proto.CoordIntegerBitsMp,
            FractionalBitsMp: proto.CoordFractionalBitsMp,
            NormalFractionalBits: proto.NormalFractionalBits,
            AngleBits: proto.AngleBits);

        return result.IsValid ? result : Legacy;
    }
}
