[Obsolete("Removed in v14104")]
public class CDestructiblePartsSystemComponent
{
    // MNetworkChangeCallback "OnDamageLevelTakenByHitGroupChanged"
    public NetworkedVector<UInt16> DamageLevelTakenByHitGroup { get; private set; } = new NetworkedVector<UInt16>();

    public CHandle<CBaseModelEntity, CsDemoParser> OwnerHandle { get; private set; }

    public Int32 LastHitDamageLevel { get; private set; }

    internal static SendNodeDecoder<CDestructiblePartsSystemComponent> CreateFieldDecoder(SerializableField field, DecoderSet decoderSet)
    {
        if (field.VarName == "m_DamageLevelTakenByHitGroup")
        {
            var decoder = FieldDecode.CreateDecoder_UInt16(field.FieldEncodingInfo);
            return (CDestructiblePartsSystemComponent @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                if (path.Length == 1)
                {
                    var newSize = (int)buffer.ReadUVarInt32();
                    @this.DamageLevelTakenByHitGroup.Resize(newSize);
                }
                else
                {
                    Debug.Assert(path.Length == 2);
                    var index = path[1];
                    @this.DamageLevelTakenByHitGroup.EnsureSize(index + 1);
                    var element = decoder(ref buffer);
                    @this.DamageLevelTakenByHitGroup[index] = element;
                }
            };
        }
        if (field.VarName == "m_hOwner")
        {
            var decoder = FieldDecode.CreateDecoder_CHandle<CBaseModelEntity, CsDemoParser>(field.FieldEncodingInfo);
            return (CDestructiblePartsSystemComponent @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.OwnerHandle = decoder(ref buffer);
            };
        }
        if (field.VarName == "m_nLastHitDamageLevel")
        {
            var decoder = FieldDecode.CreateDecoder_Int32(field.FieldEncodingInfo);
            return (CDestructiblePartsSystemComponent @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.LastHitDamageLevel = decoder(ref buffer);
            };
        }
        if (decoderSet.TryCreateFallbackDecoder(field, decoderSet, out var fallback))
        {
            return (CDestructiblePartsSystemComponent @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
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
