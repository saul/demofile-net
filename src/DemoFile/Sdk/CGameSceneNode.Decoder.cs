namespace DemoFile.Sdk;

public partial class CGameSceneNode
{
    private static FieldDecode.CustomDeserializer<CGameSceneNode, QAngle> CreateDecoder_gameSceneNodeStepSimulationAnglesSerializer(
        FieldEncodingInfo fieldEncodingInfo)
    {
        var decoder = FieldDecode.CreateDecoder_QAngle(fieldEncodingInfo);
        return (CGameSceneNode _, ref BitBuffer buffer) => decoder(ref buffer);
    }

    private static FieldDecode.CustomDeserializer<CGameSceneNode, CGameSceneNodeHandle> CreateDecoder_gameSceneNode(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (CGameSceneNode _, ref BitBuffer buffer) => new CGameSceneNodeHandle(buffer.ReadUVarInt32());
    }
}
