using DemoFile.Sdk;

namespace DemoFile.Game.Cs;

public partial class PredictedDamageTag
{
    public GameTick TagTick { get; private set; } = new();

    public float FlinchModSmall { get; private set; }

    public float FlinchModLarge { get; private set; }

    public float FriendlyFireDamageReductionRatio { get; private set; }

    internal static SendNodeDecoder<PredictedDamageTag> CreateFieldDecoder(SerializableField field, DecoderSet decoderSet)
    {
        if (field.VarName == "nTagTick")
        {
            var decoder = FieldDecode.CreateDecoder_GameTick(field.FieldEncodingInfo);
            return (PredictedDamageTag @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.TagTick = decoder(ref buffer);
            };
        }
        if (field.VarName == "flFlinchModSmall")
        {
            var decoder = FieldDecode.CreateDecoder_float(field.FieldEncodingInfo);
            return (PredictedDamageTag @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.FlinchModSmall = decoder(ref buffer);
            };
        }
        if (field.VarName == "flFlinchModLarge")
        {
            var decoder = FieldDecode.CreateDecoder_float(field.FieldEncodingInfo);
            return (PredictedDamageTag @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.FlinchModLarge = decoder(ref buffer);
            };
        }
        if (field.VarName == "flFriendlyFireDamageReductionRatio")
        {
            var decoder = FieldDecode.CreateDecoder_float(field.FieldEncodingInfo);
            return (PredictedDamageTag @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                @this.FriendlyFireDamageReductionRatio = decoder(ref buffer);
            };
        }
        if (decoderSet.TryCreateFallbackDecoder(field, decoderSet, out var fallback))
        {
            return (PredictedDamageTag @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
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
