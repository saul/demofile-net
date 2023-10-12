using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DemoFile.Sdk;

namespace DemoFile.Test.Integration;

[TestFixture]
public class Source1GameEventIntegrationTest
{
    internal class CHandleJsonConverterFactory : JsonConverterFactory
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

    internal class CHandleJsonConverter<T> : JsonConverter<CHandle<T>>
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

    internal class CEntityIndexJsonConverter : JsonConverter<CEntityIndex>
    {
        public override CEntityIndex Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, CEntityIndex value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("index", value.Value);
            writer.WriteEndObject();
        }
    }

    private static readonly JsonSerializerOptions DemoSerializerOptions = new()
    {
        Converters =
        {
            new CHandleJsonConverterFactory(),
            new CEntityIndexJsonConverter()
        }
    };

    // todo: finish this test
    [Test, Explicit]
    public async Task Snapshot()
    {
        // Arrange
        var snapshot = new StringBuilder();
        var demo = new DemoParser();

        demo.Source1GameEvents.Source1GameEvent += e =>
        {
            snapshot.AppendLine($"[{demo.CurrentGameTick}] {JsonSerializer.Serialize(e, DemoSerializerOptions)}");
        };

        // Act
        await demo.Start(SpaceVsForwardM1Stream, default);

        File.WriteAllText(@"C:\Scratch\snapshot.txt", snapshot.ToString());
    }
}
