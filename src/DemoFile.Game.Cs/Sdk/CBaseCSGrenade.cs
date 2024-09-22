namespace DemoFile.Game.Cs;

public partial class CBaseCSGrenade
{
    protected virtual int? AmmoIndex { get; }

    /// <summary>
    /// Number of grenades of this type that the owner is holding (including this one).
    /// <c>1</c> if this grenade is not held by anyone.
    /// </summary>
    public int GrenadeCount =>
        (OwnerEntity is CCSPlayerPawn playerPawn && AmmoIndex is { } ammoIndex
            ? playerPawn.WeaponServices?.Ammo[ammoIndex]
            : null)
        ?? 1;
}
