namespace DemoFile;

internal readonly record struct FieldEncodingInfo(
    string? VarEncoder,
    int BitCount,
    int EncodeFlags,
    float? LowValue,
    float? HighValue);