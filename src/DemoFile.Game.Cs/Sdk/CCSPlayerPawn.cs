using DemoFile.Extensions;

namespace DemoFile.Sdk;

public partial class CCSPlayerPawn
{
    public override string ToString() =>
        $"{(IsActive ? "[PAWN]" : "[INACTIVE PAWN]")} {Controller?.PlayerName ?? "<no controller>"}";

    public IEnumerable<CCSWeaponBase> Weapons =>
        WeaponServices?.MyWeapons.Select(handle => handle.Get<CCSWeaponBase>(Demo)).WhereNotNull()
        ?? Enumerable.Empty<CCSWeaponBase>();

    public IEnumerable<CBaseCSGrenade> Grenades =>
        Weapons.Select(weapon => weapon as CBaseCSGrenade).Where(x => x != null)!;

    public new CCSPlayerController? Controller => (CCSPlayerController?) base.Controller;

    public InputButtons InputButtons => MovementServices is {} csMovement
        ? (InputButtons) csMovement.ButtonDownMaskPrev
        : default;

    public CCSWeaponBase? ActiveWeapon => WeaponServices?.ActiveWeaponHandle.Get<CCSWeaponBase>(Demo);

    public CCSWeaponBase? LastWeapon => WeaponServices?.LastWeaponHandle.Get<CCSWeaponBase>(Demo);
}
