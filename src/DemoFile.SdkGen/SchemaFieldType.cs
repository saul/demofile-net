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

    public bool TryGetEntityHandleType([NotNullWhen(true)] out string? entityType)
    {
        entityType = null;

        if (Category != SchemaTypeCategory.Atomic)
            return false;

        if (Atomic == SchemaAtomicCategory.Basic && Name == "CEntityHandle")
        {
            entityType = "CEntityInstance";
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

    private static string AtomicToCsTypeName(string name, SchemaAtomicCategory atomic, SchemaFieldType? inner) => atomic switch
    {
        SchemaAtomicCategory.Basic => name switch
        {
            "CUtlString" or "CUtlSymbolLarge" => "NetworkedString",
            "CEntityHandle" => "CHandle<CEntityInstance>",
            "CNetworkedQuantizedFloat" => "float",
            _ => SanitiseTypeName(name)
        },
        SchemaAtomicCategory.T => $"{name.Split('<')[0]}<{inner!.CsTypeName}>",
        SchemaAtomicCategory.Collection => $"NetworkedVector<{inner!.CsTypeName}>",
        _ => throw new ArgumentOutOfRangeException(nameof(atomic), atomic, $"Unsupported atomic: {atomic}")
    };

    public string CsTypeName => Category switch
    {
        SchemaTypeCategory.Builtin => BuiltinToCsKeyword(Name),
        SchemaTypeCategory.Ptr => $"{Inner!.CsTypeName}?",
        SchemaTypeCategory.FixedArray => IsString
            ? "string"
            : $"{Inner!.CsTypeName}[]",
        SchemaTypeCategory.Atomic => AtomicToCsTypeName(Name, Atomic!.Value, Inner),
        SchemaTypeCategory.DeclaredClass => SanitiseTypeName(Name),
        SchemaTypeCategory.DeclaredEnum => SanitiseTypeName(Name),
        _ => throw new ArgumentOutOfRangeException(nameof(Category), Category, $"Unsupported type category: {Category}")
    };

    private static string SanitiseTypeName(string typeName)
    {
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
