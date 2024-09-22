using DemoFile.Extensions;

namespace DemoFile.Game.Cs;

public partial class CCSTeam
{
    public override string ToString() => string.IsNullOrEmpty(ClanTeamname) ? Teamname : $"{Teamname} ({ClanTeamname})";

    public IEnumerable<CCSPlayerController> CSPlayerControllers => PlayerControllers
        .Select(baseControllerHandle => baseControllerHandle.Get<CCSPlayerController>(Demo))
        .WhereNotNull();

    public IEnumerable<CCSPlayerPawn> CSPlayers => Players
        .Select(basePawnHandle => basePawnHandle.Get<CCSPlayerPawn>(Demo))
        .WhereNotNull();
}
