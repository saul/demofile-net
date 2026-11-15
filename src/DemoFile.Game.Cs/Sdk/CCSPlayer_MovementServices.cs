namespace DemoFile.Game.Cs;

public partial class CCSPlayer_MovementServices
{
    // Older demos still send this, decoded in CsDecoderSet.TryCreateFallbackDecoder
    [Obsolete("Removed in v14197")]
    public UInt64 ButtonDownMaskPrev { get; internal set; }
}
