namespace DemoFile;

internal record Serializer(SerializerKey Key, SerializableField[] Fields);

internal record SerializableField(
    SerializerKey DeclaredSerializer,
    string VarName,
    FieldType VarType,
    ReadOnlyMemory<string> SendNode,
    FieldEncodingInfo FieldEncodingInfo,
    SerializerKey? FieldSerializerKey)
{
    public override string ToString()
    {
        var prefix = SendNode.Length > 0 ? string.Join('.', SendNode.ToArray()) + "." : "";
        return $"{DeclaredSerializer}.{prefix}{VarName} ({VarType}{(FieldEncodingInfo.VarEncoder != null ? $" - {FieldEncodingInfo.VarEncoder}" : "")})";
    }
}

internal readonly record struct SerializerKey(string Name, int Version)
{
    public override string ToString() => Version != 0 ? $"{Name}#{Version}" : Name;
}
