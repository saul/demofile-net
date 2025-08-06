namespace DemoFile.Game.Cs;

public partial class CCSWeaponBase
{
    [Obsolete("Removed in CS2 protocol v14090")]
    public PlayerAnimEvent PlayerFireEvent { get; internal set; }

    [Obsolete("Removed in CS2 protocol v14090")]
    public CSWeaponState State { get; internal set; }
}