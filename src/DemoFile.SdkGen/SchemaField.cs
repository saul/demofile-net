using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace DemoFile.SdkGen;

public record SchemaField(
    string Name,
    SchemaFieldType Type,
    IReadOnlyList<SchemaMetadata> Metadata)
{
    public bool TryGetMetadata(string name, [NotNullWhen(true)] out SchemaMetadata? metadata)
    {
        foreach (var md in Metadata)
        {
            if (md.Name == name)
            {
                metadata = md;
                return true;
            }
        }

        metadata = null;
        return false;
    }
}
