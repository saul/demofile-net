using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DemoFile.Sdk;

namespace DemoFile.Game.Cs;

internal partial class CsDecoderSet
{
    public override bool TryCreateFallbackDecoder(
        SerializableField field,
        DecoderSet decoderSet,
        [NotNullWhen(true)] out SendNodeDecoder<FallbackDecoder.Unit>? decoder)
    {
        decoder = null;

        if (field.VarType is { IsPointer: true, Name: "CPlayer_ViewModelServices" })
        {
            Debug.Assert(field.FieldSerializerKey.HasValue);
            var serializerKey = field.FieldSerializerKey.Value;
            var innerDecoder = CPlayer_ViewModelServices.CreateDowncastDecoder(serializerKey, decoderSet, out var factory);
            var value = default(CPlayer_ViewModelServices?);
            decoder = (FallbackDecoder.Unit _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                if (path.Length == 1)
                {
                    var isSet = buffer.ReadOneBit();
                    value = isSet ? factory() : null;
                }
                else
                {
                    var inner = value ??= factory();
                    innerDecoder(inner, path[1..], ref buffer);
                }
            };
            return true;
        }

        return FallbackDecoder.TryCreate(
            field.VarName,
            field.VarType,
            field.FieldEncodingInfo,
            decoderSet,
            out decoder);
    }
}
