using System.Diagnostics.CodeAnalysis;
using DemoFile.Sdk;

namespace DemoFile;

public static class FallbackDecoder
{
    public readonly record struct Unit;

    public static bool TryCreate(
        string fieldName,
        FieldType fieldType,
        FieldEncodingInfo encodingInfo,
        DecoderSet decoderSet,
        [NotNullWhen(true)] out SendNodeDecoder<Unit>? decoder)
    {
        decoder = null;

        // Fallback array
        if (fieldType.ArrayLength != 0)
        {
            // Special case strings
            if (fieldType.Name == "char")
            {
                var fieldDecoder = FieldDecode.CreateDecoder_string(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }

            if (!TryCreate(fieldName, fieldType with {ArrayLength = 0}, encodingInfo, decoderSet, out var innerDecoder))
                return false;

            decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                var elementIdx = path[1];
                innerDecoder(default, path[2..], ref buffer);
            };
            return true;
        }

        // Fallback pointer
        if (fieldType.IsPointer)
        {
            decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                if (path.Length == 1)
                {
                    var isSet = buffer.ReadOneBit();
                    if (isSet)
                    {
                        throw new NotImplementedException($"Cannot decode pointer field ({fieldType} {fieldName}) set to a non-null value");
                    }
                }
                else
                {
                    throw new NotImplementedException($"Cannot decode inner field for fallback pointer ({fieldType} {fieldName})");
                }
            };
            return true;
        }

        if (fieldType.Name is "CNetworkUtlVectorBase" or "CUtlVectorEmbeddedNetworkVar" or "CUtlVector")
        {
            if (!TryCreate(fieldName, fieldType.GenericParameter!, encodingInfo, decoderSet, out var innerDecoder))
                return false;

            decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                // New size of vector
                if (path.Length == 1)
                {
                    buffer.ReadUVarInt32();
                }
                else
                {
                    var elementIdx = path[1];
                    innerDecoder(default, path[2..], ref buffer);
                }
            };
            return true;
        }

        switch (fieldType.Name)
        {
            case "Vector" or "VectorWS":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_Vector(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "Vector2D":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_Vector2D(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "Vector4D":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_Vector4D(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "QAngle":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_QAngle(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "float32" or "CNetworkedQuantizedFloat" or "float64":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_float(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "bool":
            {
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    buffer.ReadOneBit();
                return true;
            }
            case "Color":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_Color(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "uint8" or "int8" or "int16" or "uint16" or "int32" or "uint32" or "int64" or "uint64" or "CStrongHandle" or "CEntityHandle" or "CHandle" or "HSequence" or "CSPlayerBlockingUseAction_t" or "BloodType" or "CGameSceneNodeHandle" or "ShatterPanelMode" or "CSWeaponState_t" or "WorldGroupId_t" or "attributeprovidertypes_t":
            {
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    buffer.ReadUVarInt64();
                return true;
            }
            case "CUtlString" or "CUtlSymbolLarge":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_string(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "CUtlStringToken":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_CUtlStringToken(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "CGlobalSymbol":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_CGlobalSymbol(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "CPlayerSlot":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_CPlayerSlot(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "GameTime_t":
            {
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    FieldDecode.DecodeFloatNoscale(ref buffer);
                return true;
            }
            case "GameTick_t":
            {
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    buffer.ReadUVarInt32();
                return true;
            }
            case "CEntityIndex":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_CEntityIndex(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "CTransform":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_CTransform(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            default:
                return TryCreateFallbackClassDecoder(fieldType, decoderSet, out decoder)
                       || TryCreateHeuristicDecoder(fieldName, fieldType, out decoder);
        }
    }

    private static bool TryCreateHeuristicDecoder(
        string fieldName,
        FieldType fieldType,
        out SendNodeDecoder<Unit>? decoder)
    {
        // Heuristic: enum types. Example: EGrenadeThrowState, EFoo
        if (fieldType.Name.Length > 2
            && fieldType.Name[0] == 'E'
            && char.IsUpper(fieldType.Name[1]))
        {
            // See also FieldDecode.CreateDecoder_enum
            decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                buffer.ReadUVarInt64();
            return true;
        }

        // Heuristic: enum fields. Example: m_ePlayerFireEvent, m_eDoorState
        if (fieldName.Length > 4
            && fieldName.StartsWith("m_e")
            && char.IsUpper(fieldName[3]))
        {
            decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                buffer.ReadUVarInt64();
            return true;
        }

        decoder = null;
        return false;
    }

    private static bool TryCreateFallbackClassDecoder(
        FieldType fieldType,
        DecoderSet decoderSet,
        [NotNullWhen(true)] out SendNodeDecoder<Unit>? decoder)
    {
        if (!decoderSet.TryGetDecoderByName(fieldType.Name, out var classType, out var innerDecoder))
        {
            decoder = null;
            return false;
        }

        var dummyInstance = Activator.CreateInstance(classType)!;

        decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            innerDecoder(dummyInstance, path, ref buffer);

        return true;
    }
}
