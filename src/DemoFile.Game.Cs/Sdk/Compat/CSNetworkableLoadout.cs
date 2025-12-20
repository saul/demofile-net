using DemoFile.Sdk;

namespace DemoFile.Game.Cs;

public class CSNetworkableLoadout
{
    public CEconItemView Item { get; private set; } = new();

    public UInt16 Team { get; private set; }

    public UInt16 Slot { get; private set; }

    internal static SendNodeDecoder<CSNetworkableLoadout> CreateFieldDecoder(SerializableField field, DecoderSet decoderSet)
    {
        if (field.SendNode.Length >= 1 && field.SendNode.Span[0] == "m_Item")
        {
            var innerDecoder = CEconItemView.CreateFieldDecoder(field with {SendNode = field.SendNode[1..]}, decoderSet);
            return (CSNetworkableLoadout @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                innerDecoder(@this.Item, path, ref buffer);
            };
        }
        if (field.VarName == "m_unTeam")
        {
            var decoder = FieldDecode.CreateDecoder_UInt16(field.FieldEncodingInfo);
            return (CSNetworkableLoadout @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.Team = decoder(ref buffer);
            };
        }
        if (field.VarName == "m_unSlot")
        {
            var decoder = FieldDecode.CreateDecoder_UInt16(field.FieldEncodingInfo);
            return (CSNetworkableLoadout @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.Slot = decoder(ref buffer);
            };
        }
        if (decoderSet.TryCreateFallbackDecoder(field, decoderSet, out var fallback))
        {
            return (CSNetworkableLoadout @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
#if DEBUG
                var _field = field;
#endif
                fallback(@this, path, ref buffer);
            };
        }
        throw new NotSupportedException($"Unrecognised serializer field: {field}");
    }
}