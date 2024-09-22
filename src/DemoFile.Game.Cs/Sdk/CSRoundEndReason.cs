namespace DemoFile.Game.Cs;

public enum CSRoundEndReason
{
    Invalid = -1,
    StillInProgress = 0,
    TargetBombed = 1,
    VipEscaped = 2,
    VipAssassinated = 3,
    TerroristsEscaped = 4,
    CTsPreventEscape = 5,
    EscapingTerroristsNeutralized = 6,
    BombDefused = 7,
    CTsWin = 8,
    TerroristsWin = 9,
    RoundDraw = 10,
    AllHostagesRescued = 11,
    TargetSaved = 12,
    HostagesNotRescued = 13,
    TerroristsNotEscaped = 14,
    VipNotEscaped = 15,
    GameCommencing = 16,
    TerroristsSurrender = 17,
    CTsSurrender = 18,
    TerroristsPlanted = 19,
    CTsReachedHostage = 20,
}
