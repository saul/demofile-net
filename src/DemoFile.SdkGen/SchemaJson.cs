using System.Text.Json;

namespace DemoFile.SdkGen;

public static class SchemaJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true
    };
}
