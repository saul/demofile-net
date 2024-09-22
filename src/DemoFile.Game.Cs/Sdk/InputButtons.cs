namespace DemoFile.Game.Cs;

// Ripped from the InputBitMask_t schema enum

[Flags]
public enum InputButtons : ulong
{
    None = 0UL,
    Attack = 1UL << 0,
    Jump = 1UL << 1,
    Duck = 1UL << 2,
    Forward = 1UL << 3,
    Back = 1UL << 4,
    Use = 1UL << 5,
    //TurnLeft = 1UL << 7,
    //TurnRight = 1UL << 8,
    MoveLeft = 1UL << 9,
    MoveRight = 1UL << 10,
    AttackSecondary = 1UL << 11,
    Reload = 1UL << 13,
    Walk = 1UL << 16,
    //JoyAutoSprint = 1UL << 17,
    UseOrReload = 1UL << 32,
    Score = 1UL << 33,
    Zoom = 1UL << 34,
    LookAtWeapon = 1UL << 35
}
