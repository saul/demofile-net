namespace DemoFile.Sdk;

// Backwards compatability for pre v13987 demos

public class CCSGameModeRules_Scripted : CCSGameModeRules
{
    internal new static SendNodeDecoder<CCSGameModeRules_Scripted> CreateFieldDecoder(SerializableField field, DecoderSet decoderSet)
    {
        return CCSGameModeRules.CreateFieldDecoder(field, decoderSet);
    }
}
