using DemoFile;
using DemoFile.Sdk;
using Spectre.Console;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var path = args.SingleOrDefault() ?? throw new Exception("Expected a single argument: <path to .dem>");

        var demo = new DemoParser();

        demo.Source1GameEvents.RoundEnd += e =>
        {
            if (demo.GameRules.CSGamePhase != CSGamePhase.MatchEnded)
                return;

            var table = new Table();

            table.AddColumn("Ping", column => column.Alignment = Justify.Right);
            table.AddColumn("Name");
            table.AddColumn("Money", column => column.Alignment = Justify.Right);
            table.AddColumn("Kills");
            table.AddColumn("Deaths");
            table.AddColumn("Assists");
            table.AddColumn("HS%");
            table.AddColumn("DMG");

            AddTeamRows(demo, table, demo.TeamCounterTerrorist);
            table.AddEmptyRow();
            AddTeamRows(demo, table, demo.TeamTerrorist);
            table.AddEmptyRow();
            AddTeamRows(demo, table, demo.TeamSpectator);

            AnsiConsole.Write(table);
        };

        await demo.ReadAllAsync(File.OpenRead(path));
    }

    private static void AddTeamRows(DemoParser demo, Table table, CCSTeam team)
    {
        var orderedPlayers = team.CSPlayerControllers
            .OrderByDescending(player => player.ActionTrackingServices!.MatchStats.Damage)
            .ToArray();

        foreach (var player in orderedPlayers)
        {
            var matchStats = player.ActionTrackingServices?.MatchStats ?? new CSMatchStats();
            var teamColour = player.CSTeamNum switch
            {
                CSTeamNumber.Terrorist => "bold red",
                CSTeamNumber.CounterTerrorist => "bold blue",
                _ => "grey"
            };

            // Render the player's name in their team colour, with a link to the SteamID
            var playerNameMarkup = new Markup(
                player.PlayerName,
                Style.Parse(teamColour)
                    .Link("https://steamcommunity.com/profiles/" + player.SteamID));

            if (player.CSTeamNum == CSTeamNumber.Spectator)
            {
                table.AddRow(new Markup("SPEC"), playerNameMarkup);
                continue;
            }

            table.AddRow(
                new Markup(player.Ping.ToString()),
                playerNameMarkup,
                new Markup("$" + (player.InGameMoneyServices?.Account ?? 0)),
                new Markup(matchStats.Kills.ToString()),
                new Markup(matchStats.Deaths.ToString()),
                new Markup(matchStats.Assists.ToString()),
                new Markup((matchStats.Kills > 0 ? matchStats.HeadShotKills * 100 / matchStats.Kills : 0).ToString()),
                new Markup(matchStats.Damage.ToString(), Style.Parse("bold")));
        }
    }
}
