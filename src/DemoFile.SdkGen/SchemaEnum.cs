namespace DemoFile.SdkGen;

public record SchemaEnum(
    int Align,
    IReadOnlyList<SchemaEnumItem> Items);
