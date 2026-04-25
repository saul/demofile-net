using DemoFile.Extensions;

namespace DemoFile.Game.Cs;

public partial class CCSPlayerPawn
{
    public new CCSPlayer_WeaponServices? WeaponServices => (CCSPlayer_WeaponServices?) base.WeaponServices;

    public new CCSPlayer_ItemServices? ItemServices => (CCSPlayer_ItemServices?) base.ItemServices;

    public new CCSPlayer_UseServices? UseServices => (CCSPlayer_UseServices?) base.UseServices;

    public new CCSPlayer_WaterServices? WaterServices => (CCSPlayer_WaterServices?) base.WaterServices;

    public new CCSPlayer_MovementServices? MovementServices => (CCSPlayer_MovementServices?) base.MovementServices;

    public new CCSPlayer_CameraServices? CameraServices => (CCSPlayer_CameraServices?) base.CameraServices;

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
