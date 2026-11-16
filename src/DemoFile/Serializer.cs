using System.Text;

namespace DemoFile;

public record Serializer(SerializerKey Key, SerializableField[] Fields);

public record SerializableField(
    string VarName,
    FieldType VarType,
    ReadOnlyMemory<string> SendNode,
    FieldEncodingInfo FieldEncodingInfo,
    SerializerKey? FieldSerializerKey,
    IReadOnlyList<SerializerKey> PolymorphicTypes,
    string? VarSerializer)
{
    public override string ToString()
    {
        var result = new StringBuilder();
        if (SendNode.Length > 0)
            result.Append(string.Join('.', SendNode.ToArray()) + ".");

        result.Append(VarName);
        result.Append(" (" + VarType);

        if (FieldEncodingInfo != default)
            result.Append($": {FieldEncodingInfo})");

        if (VarSerializer != null)
            result.Append($", {nameof(VarSerializer)} = {VarSerializer}");

        result.Append(')');
        return result.ToString();
    }

    public static SerializableField FromProto(ProtoFlattenedSerializerField_t field, IReadOnlyList<string> symbols) =>
        FromProto(field, symbols, coordSizeParams: null);

    /// <summary>
    /// Create a field from its protobuf definition. <paramref name="coordSizeParams"/> are the coord size params
    /// from <see cref="CSVCMsg_FlattenedSerializer.CoordSizeParams"/>, or <c>null</c> for <see cref="CoordSizeParams.Legacy"/>.
    /// </summary>
    public static SerializableField FromProto(
        ProtoFlattenedSerializerField_t field,
        IReadOnlyList<string> symbols,
        CoordSizeParams? coordSizeParams)
    {
        var varName = symbols[field.VarNameSym];
        var varType = symbols[field.VarTypeSym];
        var sendNode = symbols[field.SendNodeSym].Split('.', StringSplitOptions.RemoveEmptyEntries);
        var varEncoder = field.HasVarEncoderSym
            ? symbols[field.VarEncoderSym]
            : null;
        var fieldSerializerKey = field.HasFieldSerializerNameSym
            ? new SerializerKey(
                symbols[field.FieldSerializerNameSym],
                field.FieldSerializerVersion)
            : default(SerializerKey?);
        var encodingInfo = new FieldEncodingInfo(
            VarEncoder: varEncoder,
            BitCount: field.BitCount,
            EncodeFlags: field.EncodeFlags,
            LowValue: field.HasLowValue ? field.LowValue : default(float?),
            HighValue: field.HasHighValue ? field.HighValue : default(float?))
        {
            IsSignedEnum = field.VarEnumInfo is { HasIsSignedEnum: true } enumInfo
                ? enumInfo.IsSignedEnum
                : null,
            CoordSizeParams = coordSizeParams,
        };
        var fieldType = FieldType.Parse(varType);
        var polymorphicTypes = field.PolymorphicTypes.Select(
            polymorphicField => new SerializerKey(
                symbols[polymorphicField.PolymorphicFieldSerializerNameSym],
                polymorphicField.PolymorphicFieldSerializerVersion))
            .ToArray();
        var varSerializer = field.HasVarSerializerSym
            ? symbols[field.VarSerializerSym]
            : null;

        return new SerializableField(
            varName,
            fieldType,
            sendNode,
            encodingInfo,
            fieldSerializerKey,
            polymorphicTypes,
            varSerializer
        );
    }
}

public readonly record struct SerializerKey(string Name, int Version)
{
    public override string ToString() => Version != 0 ? $"{Name}#{Version}" : Name;
}
