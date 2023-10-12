using System.Text;

namespace DemoFile;

internal readonly record struct FieldEncodingInfo(
    string? VarEncoder,
    int BitCount,
    int EncodeFlags,
    float? LowValue,
    float? HighValue)
{
    public override string ToString()
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(VarEncoder))
            AppendField(nameof(VarEncoder), VarEncoder);
        if (BitCount != 0)
            AppendField(nameof(BitCount), BitCount);
        if (EncodeFlags != 0)
            AppendField(nameof(EncodeFlags), EncodeFlags);
        if (LowValue.HasValue)
            AppendField(nameof(LowValue), LowValue.Value);
        if (HighValue.HasValue)
            AppendField(nameof(HighValue), HighValue.Value);

        return builder.Length == 0 ? "(none)" : builder.ToString();

        void AppendField<T>(string name, T value)
        {
            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append($"{name} = {value}");
        }
    }
}
