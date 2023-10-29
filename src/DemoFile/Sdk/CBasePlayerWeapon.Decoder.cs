namespace DemoFile.Sdk;

public partial class CBasePlayerWeapon
{
    private static FieldDecode.FieldDecoder<int> CreateDecoder_minusone(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (ref BitBuffer buffer) => (int)buffer.ReadUVarInt32() - 1;
    }
}
