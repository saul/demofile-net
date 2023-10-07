namespace DemoFile.SdkGen;

public record SchemaModule(
    IReadOnlyDictionary<string, SchemaEnum> Enums,
    IReadOnlyDictionary<string, SchemaClass> Classes);
