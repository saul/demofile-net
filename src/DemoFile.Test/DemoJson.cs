using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using DemoFile.Sdk;
using Google.Protobuf;

namespace DemoFile.Test;

public static class DemoJson
{
    private class CHandleJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() == typeof(CHandle<>);

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var handleType = typeToConvert.GetGenericArguments()[0];
            return Activator.CreateInstance(typeof(CHandleJsonConverter<>).MakeGenericType(handleType)) as JsonConverter;
        }
    }

    private class CHandleJsonConverter<T> : JsonConverter<CHandle<T>>
        where T : CEntityInstance
    {
        public override CHandle<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, CHandle<T> value, JsonSerializerOptions options)
        {
            if (!value.IsValid)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            if (typeof(T) != typeof(CEntityInstance))
                writer.WriteString("type", typeof(T).Name);

            writer.WriteNumber("index", value.Index.Value);
            writer.WriteNumber("serial", value.SerialNum);

            writer.WriteEndObject();
        }
    }

    private class CEntityInstanceJsonConverter : JsonConverter<CEntityInstance>
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsAssignableTo(typeof(CEntityInstance));

        public override CEntityInstance Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, CEntityInstance entity, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("type", entity.ServerClass.Name);
            writer.WriteNumber("index", entity.EntityIndex.Value);
            writer.WriteNumber("serial", entity.SerialNumber);
            writer.WriteEndObject();
        }
    }

    private class CEntityIndexJsonConverter : JsonConverter<CEntityIndex>
    {
        public override CEntityIndex Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, CEntityIndex value, JsonSerializerOptions options)
        {
            if (!value.IsValid)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WriteNumber("index", value.Value);
            writer.WriteEndObject();
        }
    }

    private class ByteStringJsonConverter : JsonConverter<ByteString>
    {
        public override ByteString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, ByteString value, JsonSerializerOptions options)
        {
            if (value.IsEmpty)
                writer.WriteNullValue();
            else
                writer.WriteStringValue($"<{value.Length} bytes>");
        }
    }

    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new CHandleJsonConverterFactory(),
            new CEntityIndexJsonConverter(),
            new ByteStringJsonConverter(),
            new JsonStringEnumConverter(),
            new CEntityInstanceJsonConverter()
        },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                IgnoreProtoPresenceProperties
            }
        }
    };

    private static void IgnoreProtoPresenceProperties(JsonTypeInfo typeInfo)
    {
        if (!typeInfo.Type.IsAssignableTo(typeof(IMessage)))
            return;

        var properties = typeInfo.Properties.ToDictionary(x => x.Name);

        foreach (var propertyInfo in typeInfo.Properties)
        {
            if (!propertyInfo.Name.StartsWith("Has"))
                continue;

            if (properties.ContainsKey(propertyInfo.Name[3..]))
            {
                propertyInfo.ShouldSerialize = (o, o1) => false;
            }
        }
    }
}
