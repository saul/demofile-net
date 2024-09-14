namespace DemoFile.Sdk;

public partial class CBaseEntity
{
    private static FieldDecode.CustomDeserializer<CBaseEntity, float> CreateDecoder_animTimeSerializer(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (CBaseEntity _, ref BitBuffer buffer) => FieldDecode.DecodeSimulationTime(ref buffer);
    }

    private static FieldDecode.CustomDeserializer<CBaseEntity, float> CreateDecoder_simulationTimeSerializer(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (CBaseEntity _, ref BitBuffer buffer) => FieldDecode.DecodeSimulationTime(ref buffer);
    }

    private static FieldDecode.CustomDeserializer<CBaseEntity, int> CreateDecoder_ClampHealth(
        FieldEncodingInfo fieldEncodingInfo)
    {
        var decoder = FieldDecode.CreateDecoder_Int32(fieldEncodingInfo);
        return (CBaseEntity _, ref BitBuffer buffer) => decoder(ref buffer);
    }
}
