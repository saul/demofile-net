using System.Diagnostics.CodeAnalysis;
using DemoFile.Sdk;

namespace DemoFile.Game.Deadlock;

internal partial class DeadlockDecoderSet
{
    public override bool TryCreateFallbackDecoder(
        SerializableField field,
        DecoderSet decoderSet,
        [NotNullWhen(true)] out SendNodeDecoder<FallbackDecoder.Unit>? decoder)
    {
        return FallbackDecoder.TryCreate(
            field.VarName,
            field.VarType,
            field.FieldEncodingInfo,
            decoderSet,
            out decoder);
    }
}
