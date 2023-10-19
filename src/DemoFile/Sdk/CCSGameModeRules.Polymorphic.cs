using System.Diagnostics.CodeAnalysis;

namespace DemoFile.Sdk;

public partial class CCSGameModeRules
{
    internal static bool TryCreateDowncastDecoderById(
        DecoderSet decoderSet,
        uint childClassId,
        [NotNullWhen(true)] out Func<CCSGameModeRules>? factory,
        [NotNullWhen(true)] out SendNodeDecoder<CCSGameModeRules>? innerDecoder)
    {
        // todo: still need to reverse engineer this. here's what I've seen so far:
        // - 3: Deathmatch (casual)
        // - 5: Competitive

        if (childClassId == 3)
        {
            innerDecoder = CreateDowncastDecoder(
                new SerializerKey(nameof(CCSGameModeRules_Deathmatch), 0),
                decoderSet,
                out factory);
        }
        else
        {
            innerDecoder = CreateDowncastDecoder(
                new SerializerKey(nameof(CCSGameModeRules_Noop), 0),
                decoderSet,
                out factory);
        }

        return true;
    }
}
