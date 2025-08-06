using System.Diagnostics.CodeAnalysis;
using DemoFile.Sdk;

namespace DemoFile.Game.Deadlock;

internal partial class DeadlockDecoderSet
{
    public override bool TryCreateFallbackDecoder(
        SerializableField field,
        DecoderSet decoderSet,
        [NotNullWhen(true)] out SendNodeDecoder<object>? decoder)
    {
        if (FallbackDecoder.TryCreate(field.VarName, field.VarType, field.FieldEncodingInfo, decoderSet, out var engineFallbackDecoder))
        {
            decoder = (object _, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                engineFallbackDecoder(default, path, ref buffer);
            };
            return true;
        }

        decoder = null;
        return false;
    }
}
