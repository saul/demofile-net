using System.Text.Json;

namespace DemoFile.SdkGen;

public record SchemaMetadata(string Name, JsonElement Value)
{
    public bool HasValue => Value.ValueKind != JsonValueKind.Undefined;

    public override string ToString() => Value.ValueKind switch
    {
        JsonValueKind.String => $"\"{StringValue}\"",
        JsonValueKind.Object => $"\"{ClassValue.Type} {ClassValue.Name}\"",
        _ => Value.ToString()
    };

    public SchemaClassMetadata ClassValue => Value.Deserialize<SchemaClassMetadata>(SchemaJson.SerializerOptions)!;
    public string StringValue => Value.GetString()!;
    public int IntValue => Value.GetInt32();
    public float FloatValue => Value.GetSingle();
}
