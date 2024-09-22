using DemoFile.GameStatic;

namespace DemoFile.Game.Cs;

public partial class CCSPlayerController
{
    public override string ToString() => $"{(IsActive ? "" : "[INACTIVE] ")}{PlayerName} ({Connected})";

    public string? PawnCharacterName => GameItems.ItemDefinitions.GetValueOrDefault(PawnCharacterDefIndex)?.Name;

    public CMsgPlayerInfo PlayerInfo => Demo.PlayerInfos[(int) EntityIndex.Value - 1]!;
}
