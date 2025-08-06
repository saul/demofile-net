using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DemoFile.Sdk;

namespace DemoFile.Game.Cs;

internal partial class CsDecoderSet
{
    public override bool TryCreateFallbackDecoder(
        SerializableField field,
        DecoderSet decoderSet,
        [NotNullWhen(true)] out SendNodeDecoder<object>? decoder)
    {
        decoder = null;

        if (field.VarType is { IsPointer: true, Name: "CPlayer_ViewModelServices" })
        {
            Debug.Assert(field.FieldSerializerKey.HasValue);
            var serializerKey = field.FieldSerializerKey.Value;
            var innerDecoder = CPlayer_ViewModelServices.CreateDowncastDecoder(serializerKey, decoderSet, out var factory);
            var value = default(CPlayer_ViewModelServices?);
            decoder = (object @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
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

        // Compatibility for pre-v14090 demos
        if (field.VarName == "m_ePlayerFireEvent" && field.VarType.Name == "PlayerAnimEvent_t")
        {
            var fieldDecoder = FieldDecode.CreateDecoder_enum<PlayerAnimEvent>(field.FieldEncodingInfo);
            decoder = (object @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                ((CCSWeaponBase)@this).PlayerFireEvent = fieldDecoder(ref buffer);
            };
            return true;
        }
        if (field.VarName == "m_iState" && field.VarType.Name == "CSWeaponState_t")
        {
            var fieldDecoder = FieldDecode.CreateDecoder_enum<CSWeaponState>(field.FieldEncodingInfo);
            decoder = (object @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                ((CCSWeaponBase)@this).State = fieldDecoder(ref buffer);
            };
            return true;
        }

        if (FallbackDecoder.TryCreate(field.VarName, field.VarType, field.FieldEncodingInfo, decoderSet, out var engineFallbackDecoder))
        {
            decoder = (object _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                engineFallbackDecoder(default, path, ref buffer);
            };
            return true;
        }

        return false;
    }
}
