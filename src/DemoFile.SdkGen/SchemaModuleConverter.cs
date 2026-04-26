using System.Text.Json;
using System.Text.Json.Serialization;

namespace DemoFile.SdkGen;

public sealed class SchemaModuleConverter : JsonConverter<SchemaModule>
{
    private record SchemaClassBody(
        string? Parent,
        IReadOnlyList<SchemaMetadata> Metadata,
        IReadOnlyList<SchemaField> Fields);

    public override SchemaModule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        IReadOnlyDictionary<string, SchemaEnum>? enums = null;
        var classes = new Dictionary<string, SchemaClass>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            var propName = reader.GetString();
            reader.Read();

            switch (propName)
            {
                case "enums":
                    enums = JsonSerializer.Deserialize<IReadOnlyDictionary<string, SchemaEnum>>(ref reader, options);
                    break;
                case "classes":
                    ReadClasses(ref reader, options, classes);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return new SchemaModule(
            enums ?? new Dictionary<string, SchemaEnum>(),
            classes);
    }

    private static void ReadClasses(ref Utf8JsonReader reader, JsonSerializerOptions options, Dictionary<string, SchemaClass> classes)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            var name = reader.GetString()!;
            reader.Read();

            var body = JsonSerializer.Deserialize<SchemaClassBody>(ref reader, options)!;
            classes[name] = new SchemaClass(name, body.Parent, body.Metadata, body.Fields);
        }
    }

    public override void Write(Utf8JsonWriter writer, SchemaModule value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("enums");
        JsonSerializer.Serialize(writer, value.Enums, options);

        writer.WritePropertyName("classes");
        writer.WriteStartObject();
        foreach (var (_, schemaClass) in value.Classes)
        {
            writer.WritePropertyName(schemaClass.Name);
            JsonSerializer.Serialize(writer, new SchemaClassBody(schemaClass.Parent, schemaClass.Metadata, schemaClass.Fields), options);
        }
        writer.WriteEndObject();

        writer.WriteEndObject();
    }
}
