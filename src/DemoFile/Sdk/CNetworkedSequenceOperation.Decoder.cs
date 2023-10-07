namespace DemoFile.Sdk;

public partial class CNetworkedSequenceOperation
{
    private static FieldDecode.FieldDecoder<HSequence> CreateDecoder_minusone(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (ref BitBuffer buffer) =>
        {
            return new HSequence(buffer.ReadUVarInt64());
        };
    }
}
