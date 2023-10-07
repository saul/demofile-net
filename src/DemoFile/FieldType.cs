using System.Text.RegularExpressions;

namespace DemoFile;

internal partial record FieldType(string Name, FieldType? GenericParameter, bool IsPointer, int ArrayLength)
{
    private static readonly Dictionary<string, FieldType> FieldTypeCache = new();

    public override string ToString()
    {
        var genericStr = GenericParameter != null ? $"< {GenericParameter} >" : "";
        var pointerStr = IsPointer ? "*" : "";
        var arrayLengthStr = ArrayLength != 0 ? $"[{ArrayLength}]" : "";
        return $"{Name}{genericStr}{pointerStr}{arrayLengthStr}";
    }

    public static FieldType Parse(string typeName)
    {
        if (FieldTypeCache.TryGetValue(typeName, out var fieldType))
        {
            return fieldType;
        }

        fieldType = ParseCore(typeName);
        FieldTypeCache[typeName] = fieldType;
        return fieldType;
    }

    private static FieldType ParseCore(string typeName)
    {
        var match = TypeNameRegex().Match(typeName);
        if (!match.Success)
        {
            throw new Exception($"Invalid field type: {typeName}");
        }

        var name = match.Groups["name"].Value;
        var genericParam = match.Groups["generic"] is { Success: true, Value: var genericName }
            ? Parse(genericName)
            : null;
        var isPointer = match.Groups["ptr"].Success;
        
        // todo: may need to support consts here
        var count = match.Groups["count"] is { Success: true, Value: var countStr }
            ? int.Parse(countStr)
            : 0;

        return new FieldType(Name: name, GenericParameter: genericParam, IsPointer: isPointer, ArrayLength: count);
    }

    [GeneratedRegex(@"^(?<name>[^\<\[\*]+)(\<\s(?<generic>.*)\s\>)?(?<ptr>\*)?(\[(?<count>.*)\])?$")]
    private static partial Regex TypeNameRegex();
}
