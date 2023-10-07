namespace DemoFile.Sdk;

public partial class CGameSceneNode
{
    private static FieldDecode.FieldDecoder<QAngle> CreateDecoder_gameSceneNodeStepSimulationAnglesSerializer(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return FieldDecode.CreateDecoder_QAngle(fieldEncodingInfo);
    }

    private static FieldDecode.FieldDecoder<CGameSceneNodeHandle> CreateDecoder_gameSceneNode(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (ref BitBuffer buffer) => new CGameSceneNodeHandle(buffer.ReadUVarInt32());
    }
}
