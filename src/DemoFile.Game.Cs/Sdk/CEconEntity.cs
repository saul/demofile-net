namespace DemoFile.Game.Cs;

public partial class CEconEntity
{
    public CEconItemView EconItem => AttributeManager.Item;

    public ulong OriginalOwnerXuid => ((ulong)OriginalOwnerXuidHigh << 32) | OriginalOwnerXuidLow;

    public CCSPlayerController? OriginalOwner => Demo.Players.FirstOrDefault(pl => pl.SteamID == OriginalOwnerXuid);
}
