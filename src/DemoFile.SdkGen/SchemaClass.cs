﻿using System.Text.RegularExpressions;

namespace DemoFile.SdkGen;

public enum BoxedPrimitiveType
{
    Integer,
    Float
}

public partial record SchemaClass(
    string? Parent,
    IReadOnlyList<SchemaMetadata> Metadata,
    IReadOnlyList<SchemaField> Fields)
{
    private Dictionary<string, SchemaField[]>? _fieldsByCsPropertyName = null;

    public BoxedPrimitiveType? BoxedPrimitive
    {
        get
        {
            if (Metadata.Any(x => x.Name == "MIsBoxedIntegerType"))
                return BoxedPrimitiveType.Integer;

            if (Metadata.Any(x => x.Name == "MIsBoxedFloatType"))
                return BoxedPrimitiveType.Float;

            return default;
        }
    }

    [GeneratedRegex("^(m_)?(fl|a|n|i|isz|vec|us|u|ub|un|sz|b|f|clr|h|ang|af|ch|q|p|v|arr|ar|bv|e|s|t|rg)(?<firstChar>[A-Z])")]
    private static partial Regex HungarianNotationRegex();

    private static string RemoveMemberPrefix(string fieldName)
    {
        if (fieldName.StartsWith("m_"))
            fieldName = fieldName[2..];

        return $"{char.ToUpper(fieldName[0])}{fieldName[1..]}";
    }

    public string CsPropertyNameForField(GameSdkInfo gameSdkInfo, string className, SchemaField field)
    {
        // When the hungarian notation prefix is removed, some fields shadow their parent's.
        // Because there are only a handful, we have some manual overrides.
        switch (className, field.Name)
        {
            case ("CFish", "m_waterLevel"): return "FishWaterLevel";
            case ("CBaseFire", "m_nFlags"): return "FireFlags";
            case ("CDynamicLight", "m_Flags"): return "DynamicLightFlags";
            case ("CEnvScreenOverlay", "m_bIsActive"): return "IsOverlayActive";
            case ("CPlayerSprayDecal", "m_nEntity"): return "DecalEntity";
            case ("CEnvProjectedTexture", "m_flRotation"): return "TextureRotation";
            case ("CSun", "m_flRotation"): return "SunRotation";
            case ("CTriggerPhysics", "m_gravityScale"): return "TriggerGravityScale";
            case ("CBeam", "m_fSpeed"): return "BeamSpeed";
            case ("CEnvDeferredLight", "m_nFlags"): return "LightFlags";
            case ("CTeamTrackedStatsEntity", "m_nTeam"): return "StatsTeam";
        }

        string CleanFieldName(string fieldName) =>
            RemoveMemberPrefix(HungarianNotationRegex().Replace(fieldName.Replace("_Entity_", "_"), r => r.Groups["firstChar"].Value));

        _fieldsByCsPropertyName ??= Fields
            .GroupBy(x => CleanFieldName(x.Name))
            .ToDictionary(g => g.Key, g => g.ToArray());

        var cleanName = CleanFieldName(field.Name);

        // If removing the hungarian notation prefix causes clashes with other fields,
        // then just remove the `m_` prefix and convert to title case.
        var uniqueName = _fieldsByCsPropertyName[cleanName].Length == 1
            ? cleanName
            : RemoveMemberPrefix(field.Name);

        return field.Type.TryGetEntityHandleType(gameSdkInfo, out _)
            ? uniqueName + "Handle"
            : uniqueName;
    }
}
