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
        if (field.VarName == "m_vecNetworkableLoadout")
        {
            var dummyLoadout = new NetworkedVector<CSNetworkableLoadout>();
            var innerDecoder = decoderSet.GetDecoder<CSNetworkableLoadout>(field.FieldSerializerKey!.Value);
            decoder = (object @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                //@this = CCSPlayerController_InventoryServices

                if (path.Length == 1)
                {
                    var newSize = (int)buffer.ReadUVarInt32();
                    dummyLoadout.Resize(newSize);
                }
                else
                {
                    Debug.Assert(path.Length > 2);
                    var index = path[1];
                    dummyLoadout.EnsureSize(index + 1);
                    var element = dummyLoadout[index] ??= new CSNetworkableLoadout();
                    innerDecoder(element, path[2..], ref buffer);
                }
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
