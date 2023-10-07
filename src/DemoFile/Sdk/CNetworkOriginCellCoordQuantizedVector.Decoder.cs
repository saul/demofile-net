namespace DemoFile.Sdk;

public partial class CNetworkOriginCellCoordQuantizedVector
{
    private static FieldDecode.FieldDecoder<UInt16> CreateDecoder_cellx(
        FieldEncodingInfo fieldEncodingInfo)
    {
        // todo: untested
        return FieldDecode.CreateDecoder_UInt16(fieldEncodingInfo);
    }

    private static FieldDecode.FieldDecoder<UInt16> CreateDecoder_celly(
        FieldEncodingInfo fieldEncodingInfo)
    {
        // todo: untested
        return FieldDecode.CreateDecoder_UInt16(fieldEncodingInfo);
    }

    private static FieldDecode.FieldDecoder<UInt16> CreateDecoder_cellz(
        FieldEncodingInfo fieldEncodingInfo)
    {
        // todo: untested
        return FieldDecode.CreateDecoder_UInt16(fieldEncodingInfo);
    }

    private static FieldDecode.FieldDecoder<float> CreateDecoder_posx(
        FieldEncodingInfo fieldEncodingInfo)
    {
        // todo: untested
        return FieldDecode.CreateDecoder_float(fieldEncodingInfo);
    }

    private static FieldDecode.FieldDecoder<float> CreateDecoder_posy(
        FieldEncodingInfo fieldEncodingInfo)
    {
        // todo: untested
        return FieldDecode.CreateDecoder_float(fieldEncodingInfo);
    }

    private static FieldDecode.FieldDecoder<float> CreateDecoder_posz(
        FieldEncodingInfo fieldEncodingInfo)
    {
        // todo: untested
        return FieldDecode.CreateDecoder_float(fieldEncodingInfo);
    }
}
