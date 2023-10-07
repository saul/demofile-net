namespace DemoFile.SdkGen;

public record SchemaClass(
    int Index,
    string? Parent,
    IReadOnlyList<SchemaField> Fields);
