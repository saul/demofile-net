using System.Diagnostics.CodeAnalysis;

namespace DemoFile.SdkGen;

public record SchemaFieldType(
    string Name,
    SchemaTypeCategory Category,
    SchemaAtomicCategory? Atomic,
    SchemaFieldType? Inner)
{
    public static SchemaFieldType FromDeclaredClass(string declaredTypeName) => new(
        Name: declaredTypeName,
        Category: SchemaTypeCategory.DeclaredClass,
        Atomic: null,
        Inner: null);

    public bool IsString =>
        Category == SchemaTypeCategory.FixedArray
        && Inner!.Category == SchemaTypeCategory.Builtin
        && Inner.Name == "char";

    public bool TryGetEntityHandleType(GameSdkInfo gameSdkInfo, [NotNullWhen(true)] out string? entityType)
    {
        entityType = null;

        if (Category != SchemaTypeCategory.Atomic)
            return false;

        if (Atomic == SchemaAtomicCategory.Basic && Name == "CEntityHandle")
        {
            entityType = $"CEntityInstance<{gameSdkInfo.DemoParserClass}>";
            return true;
        }

        if (Atomic == SchemaAtomicCategory.T && Name.StartsWith("CHandle<"))
        {
            entityType = Inner!.Name;
            return true;
        }

        return false;
    }

    public bool IsDeclared => Category is SchemaTypeCategory.DeclaredClass or SchemaTypeCategory.DeclaredEnum;

    public bool TryGetArrayElementType([NotNullWhen(true)] out SchemaFieldType? elementType)
    {
        if (Category == SchemaTypeCategory.FixedArray && !IsString)
        {
            elementType = Inner!;
            return true;
        }
        else
        {
            elementType = null;
            return false;
        }
    }

    private static string BuiltinToCsKeyword(string name) => name switch
    {
        "float32" => "float",
        "float64" => "double",
        "int8" => "sbyte",
        "int16" => "Int16",
        "int32" => "Int32",
        "int64" => "Int64",
        "uint8" => "byte",
        "uint16" => "UInt16",
        "uint32" => "UInt32",
        "uint64" => "UInt64",
        "bool" => "bool",
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, $"Unknown built-in: {name}")
    };

    private static string AtomicToCsTypeName(GameSdkInfo gameSdkInfo, string name, SchemaAtomicCategory atomic, SchemaFieldType? inner) => atomic switch
    {
        SchemaAtomicCategory.Basic => name switch
        {
            "CUtlString" or "CUtlSymbolLarge" => "NetworkedString",
            "CEntityHandle" => $"CHandle<CEntityInstance<{gameSdkInfo.DemoParserClass}>, {gameSdkInfo.DemoParserClass}>",
            "CNetworkedQuantizedFloat" => "float",
            _ => SanitiseTypeName(gameSdkInfo, name)
        },
        SchemaAtomicCategory.T when name.StartsWith("CHandle<") => $"CHandle<{inner!.GetCsTypeName(gameSdkInfo)}, {gameSdkInfo.DemoParserClass}>",
        SchemaAtomicCategory.T => $"{name.Split('<')[0]}<{inner!.GetCsTypeName(gameSdkInfo)}>",
        SchemaAtomicCategory.Collection => $"NetworkedVector<{inner!.GetCsTypeName(gameSdkInfo)}>",
        _ => throw new ArgumentOutOfRangeException(nameof(atomic), atomic, $"Unsupported atomic: {atomic}")
    };

    public string GetCsTypeName(GameSdkInfo gameSdkInfo) => Category switch
    {
        SchemaTypeCategory.Builtin => BuiltinToCsKeyword(Name),
        SchemaTypeCategory.Ptr => $"{Inner!.GetCsTypeName(gameSdkInfo)}?",
        SchemaTypeCategory.FixedArray => IsString
            ? "string"
            : $"{Inner!.GetCsTypeName(gameSdkInfo)}[]",
        SchemaTypeCategory.Atomic => AtomicToCsTypeName(gameSdkInfo, Name, Atomic!.Value, Inner),
        SchemaTypeCategory.DeclaredClass => SanitiseTypeName(gameSdkInfo, Name),
        SchemaTypeCategory.DeclaredEnum => SanitiseTypeName(gameSdkInfo, Name),
        _ => throw new ArgumentOutOfRangeException(
            nameof(Category),
            Category,
            $"Unsupported type category: {Category}")
    };

    private static string SanitiseTypeName(GameSdkInfo gameSdkInfo, string typeName)
    {
        if (typeName == "CEntityInstance")
            return $"CEntityInstance<{gameSdkInfo.DemoParserClass}>";

        if (SchemaNameMap.TryGetValue(typeName, out var sanitised))
            return sanitised;

        var withoutColons = typeName.Replace(":", "");

        return withoutColons.EndsWith("_t")
            ? withoutColons[..^2]
            : withoutColons;
    }

    private static readonly Dictionary<string, string> SchemaNameMap = new()
    {
        { "shard_model_desc_t", "SharedModelDesc" },
        { "fogplayerparams_t", "FogPlayerParams" },
        { "fogparams_t", "FogParams" },
        { "audioparams_t", "AudioParams" },
        { "loadout_slot_t", "LoadoutSlot" },
        { "attributeprovidertypes_t", "AttributeProviderTypes" },
        { "sky3dparams_t", "Sky3DParams" },
    };
}
