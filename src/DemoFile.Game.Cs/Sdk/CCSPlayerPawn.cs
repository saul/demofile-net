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

    // m_nButtonDownMaskPrev stopped being networked in CS2 build 14197 (2026-11-11).
    // Still populated for older demos. Use the user commands instead (UserCmd.Buttons), which carry the
    // same mask for every command (delta-encoded from 14197 onwards).
    [Obsolete("Removed in v14197. Use CsDemoParser.UserCmdEvents (UserCmd.Buttons) instead.")]
    public InputButtons InputButtons => MovementServices is {} csMovement
#pragma warning disable CS0618 // Type or member is obsolete
        ? (InputButtons) csMovement.ButtonDownMaskPrev
#pragma warning restore CS0618
        : default;

    public CCSWeaponBase? ActiveWeapon => WeaponServices?.ActiveWeaponHandle.Get<CCSWeaponBase>(Demo);

    public CCSWeaponBase? LastWeapon => WeaponServices?.LastWeaponHandle.Get<CCSWeaponBase>(Demo);
}
