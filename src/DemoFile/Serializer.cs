using System.Text;

namespace DemoFile;

internal record Serializer(SerializerKey Key, SerializableField[] Fields);

internal record SerializableField(
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

    public static SerializableField FromProto(ProtoFlattenedSerializerField_t field, IReadOnlyList<string> symbols)
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
            HighValue: field.HasHighValue ? field.HighValue : default(float?));
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

internal readonly record struct SerializerKey(string Name, int Version)
{
    public override string ToString() => Version != 0 ? $"{Name}#{Version}" : Name;
}
