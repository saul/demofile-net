namespace DemoFile.Sdk;

public partial class CBasePlayerWeapon
{
    private static FieldDecode.FieldDecoder<int> CreateDecoder_minusone(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (ref BitBuffer buffer) =>
        {
            // todo: this needs verifying. used by m_iClip1/2
            var result = buffer.ReadVarInt32();
            if (result > 0)
                return result - 1;
            else
                return result;
        };
    }
}
