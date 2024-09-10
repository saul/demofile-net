namespace DemoFile.Sdk;

public partial class CBasePlayerWeapon
{
    private static FieldDecode.CustomDeserializer<CBasePlayerWeapon, int> CreateDecoder_minusone(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (CBasePlayerWeapon _, ref BitBuffer buffer) => (int)buffer.ReadUVarInt32() - 1;
    }
}
