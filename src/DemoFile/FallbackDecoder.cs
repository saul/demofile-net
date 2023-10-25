using System.Diagnostics.CodeAnalysis;
using DemoFile.Sdk;

namespace DemoFile;

internal static class FallbackDecoder
{
    public readonly record struct Unit;

    public static bool TryCreate(
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

            if (!TryCreate(fieldType with {ArrayLength = 0}, encodingInfo, decoderSet, out var innerDecoder))
                return false;

            decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                var elementIdx = path[1];
                innerDecoder(default, path[2..], ref buffer);
            };
            return true;
        }

        if (fieldType.Name is "CNetworkUtlVectorBase" or "CUtlVectorEmbeddedNetworkVar" or "CUtlVector")
        {
            if (!TryCreate(fieldType.GenericParameter!, encodingInfo, decoderSet, out var innerDecoder))
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
            case "Vector":
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
            case "float32" or "CNetworkQuantizedFloat" or "float64":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_float(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "bool":
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    buffer.ReadOneBit();
                return true;
            case "Color":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_Color(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            case "uint8" or "int8" or "int16" or "uint16" or "int32" or "uint32" or "int64" or "uint64" or "CStrongHandle" or "CEntityHandle":
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    buffer.ReadUVarInt64();
                return true;
            case "CUtlString" or "CUtlSymbolLarge":
            {
                var fieldDecoder = FieldDecode.CreateDecoder_string(encodingInfo);
                decoder = (Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
                    fieldDecoder(ref buffer);
                return true;
            }
            default:
                return TryCreateFallbackClassDecoder(fieldType, decoderSet, out decoder);
        }
    }

    private static bool TryCreateFallbackClassDecoder(
        FieldType fieldType,
        DecoderSet decoderSet,
        [NotNullWhen(true)] out SendNodeDecoder<Unit>? decoder)
    {
        if (!decoderSet.TryGetDecoder(fieldType.Name, out var classType, out var innerDecoder))
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
