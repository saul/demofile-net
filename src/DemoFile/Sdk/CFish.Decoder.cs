namespace DemoFile.Sdk;

public partial class CFish
{
    private static FieldDecode.FieldDecoder<float> CreateDecoder_fish_pos_x(
        FieldEncodingInfo fieldEncodingInfo)
    {
        // Offset relative to m_poolOrigin
        return (ref BitBuffer buffer) => FieldDecode.DecodeFloatNoscale(ref buffer);
    }

    private static FieldDecode.FieldDecoder<float> CreateDecoder_fish_pos_y(
        FieldEncodingInfo fieldEncodingInfo)
    {
        // Offset relative to m_poolOrigin
        return (ref BitBuffer buffer) => FieldDecode.DecodeFloatNoscale(ref buffer);
    }

    private static FieldDecode.FieldDecoder<float> CreateDecoder_fish_pos_z(
        FieldEncodingInfo fieldEncodingInfo)
    {
        // Offset relative to m_poolOrigin
        return (ref BitBuffer buffer) => FieldDecode.DecodeFloatNoscale(ref buffer);
    }

    private static FieldDecode.FieldDecoder<float> CreateDecoder_angle_normalize_positive(
        FieldEncodingInfo fieldEncodingInfo)
    {
        // Angle in the range of [0..360]
        return (ref BitBuffer buffer) => FieldDecode.DecodeFloatNoscale(ref buffer);
    }

}
