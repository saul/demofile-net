namespace DemoFile.Sdk;

public partial class CBaseEntity
{
    private static FieldDecode.FieldDecoder<float> CreateDecoder_animTimeSerializer(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (ref BitBuffer buffer) => FieldDecode.DecodeSimulationTime(ref buffer);
    }

    private static FieldDecode.FieldDecoder<float> CreateDecoder_simulationTimeSerializer(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (ref BitBuffer buffer) => FieldDecode.DecodeSimulationTime(ref buffer);
    }

    private static FieldDecode.FieldDecoder<int> CreateDecoder_ClampHealth(
        FieldEncodingInfo fieldEncodingInfo) =>
        FieldDecode.CreateDecoder_Int32(fieldEncodingInfo);
}
