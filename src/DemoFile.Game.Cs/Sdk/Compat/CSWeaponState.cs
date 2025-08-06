namespace DemoFile.Game.Cs;

[Obsolete("Removed in CS2 protocol v14090")]
public enum CSWeaponState : int
{
    WEAPON_NOT_CARRIED = 0x0,
    WEAPON_IS_CARRIED_BY_PLAYER = 0x1,
    WEAPON_IS_ACTIVE = 0x2,
}
