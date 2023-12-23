namespace DemoFile.Sdk;

public partial class CNetworkedSequenceOperation
{
    private static FieldDecode.CustomDeserializer<CNetworkedSequenceOperation, HSequence> CreateDecoder_minusone(
        FieldEncodingInfo fieldEncodingInfo)
    {
        return (CNetworkedSequenceOperation _, ref BitBuffer buffer) =>
        {
            var read = buffer.ReadUVarInt64() - 1;
            return new HSequence((long)read);
        };
    }
}
