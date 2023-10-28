namespace DemoFile.Sdk;

public partial class CNetworkedSequenceOperation
{
    private static FieldDecode.FieldDecoder<HSequence> CreateDecoder_minusone(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (ref BitBuffer buffer) =>
        {
            var read = buffer.ReadUVarInt64() - 1;
            return new HSequence((long)read);
        };
    }
}
