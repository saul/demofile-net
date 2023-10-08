using DemoFile.Extensions;

namespace DemoFile.Sdk;

public partial class CCSTeam
{
    public override string ToString() => string.IsNullOrEmpty(ClanTeamname) ? Teamname : $"{Teamname} ({ClanTeamname})";

    public IEnumerable<CCSPlayerController> CSPlayerControllers => PlayerControllers
        .Select(baseControllerHandle =>
            Demo.GetEntityByHandle(baseControllerHandle) as CCSPlayerController)
        .WhereNotNull();

    public IEnumerable<CCSPlayerPawn> CSPlayers => Players
        .Select(basePawnHandle =>
            Demo.GetEntityByHandle(basePawnHandle) as CCSPlayerPawn)
        .WhereNotNull();
}