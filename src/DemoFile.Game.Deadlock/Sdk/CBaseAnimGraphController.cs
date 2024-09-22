namespace DemoFile.Game.Deadlock;

public partial class CBaseAnimGraphController
{
    private static FieldDecode.CustomDeserializer<CBaseAnimGraphController, HSequence> CreateDecoder_minusone(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (CBaseAnimGraphController _, ref BitBuffer buffer) =>
        {
            var read = buffer.ReadUVarInt64() - 1;
            return new HSequence((long)read);
        };
    }
}
