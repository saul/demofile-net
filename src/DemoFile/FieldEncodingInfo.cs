using System.Text;

namespace DemoFile;

public readonly record struct FieldEncodingInfo(
    string? VarEncoder,
    int BitCount,
    int EncodeFlags,
    float? LowValue,
    float? HighValue)
{
    /// <summary>
    /// Whether the server reported this field's enum type as signed (zig-zag encoded).
    /// Only sent by newer servers (<c>ProtoFlattenedSerializerField_t.var_enum_info</c>); <c>null</c> otherwise.
    /// </summary>
    public bool? IsSignedEnum { get; init; }

    /// <summary>
    /// Coord/normal/angle bit widths for the serializers this field belongs to.
    /// <c>null</c> means <see cref="DemoFile.CoordSizeParams.Legacy"/>.
    /// </summary>
    public CoordSizeParams? CoordSizeParams { get; init; }

    /// <summary>
    /// Coord/normal/angle bit widths to decode this field with.
    /// </summary>
    public CoordSizeParams CoordParams => CoordSizeParams ?? DemoFile.CoordSizeParams.Legacy;

    public override string ToString()
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(VarEncoder))
            AppendField(nameof(VarEncoder), VarEncoder);
        if (BitCount != 0)
            AppendField(nameof(BitCount), BitCount);
        if (EncodeFlags != 0)
            AppendField(nameof(EncodeFlags), EncodeFlags);
        if (LowValue.HasValue)
            AppendField(nameof(LowValue), LowValue.Value);
        if (HighValue.HasValue)
            AppendField(nameof(HighValue), HighValue.Value);
        if (IsSignedEnum.HasValue)
            AppendField(nameof(IsSignedEnum), IsSignedEnum.Value);

        return builder.Length == 0 ? "(none)" : builder.ToString();

        void AppendField<T>(string name, T value)
        {
            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append($"{name} = {value}");
        }
    }
}
