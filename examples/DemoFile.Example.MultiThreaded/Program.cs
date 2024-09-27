using System.Diagnostics;
using System.Text;
using DemoFile.Game.Cs;

namespace DemoFile.Example.MultiThreaded;

class Program
{
    static async Task Main(string[] args)
    {
        var path = args.SingleOrDefault() ?? throw new Exception("Expected a single argument: <path to .dem>");

        var sw = Stopwatch.StartNew();

        // When reading a demo in parallel, it is split into `n` sections,
        // where `n` is roughly equal to Environment.ProcessorCount.
        // Each section is read concurrently, where the event callbacks
        // are setup in `SetupSection`.
        // The result of each section (i.e. the value returned by `SetupSection`)
        // are returned as a list from `ReadAllParallelAsync`.
        // The results are always returned in demo time order.
        var results = await DemoFileReader<CsDemoParser>.ReadAllParallelAsync(
            File.ReadAllBytes(path),
            SetupSection,
            default);

        foreach (var result in results)
        {
            Console.Write(result.ToString());
        }

        Console.WriteLine($"Finished in {sw.Elapsed.TotalSeconds:N3} seconds");
    }

    private static StringBuilder SetupSection(CsDemoParser demo)
    {
        var result = new StringBuilder();

        demo.Source1GameEvents.PlayerDeath += e =>
        {
            // Write attacker name and their team
            result.Append($"{e.Attacker?.PlayerName} [{e.Attacker?.Team.Teamname}]");

            // Write the weapon
            result.Append($" <{e.Weapon}");
            if (e.Headshot)
                result.Append(" HS");
            result.Append("> ");

            // Write the victim's name and their team
            result.AppendLine($"{e.Player?.PlayerName} [{e.Attacker?.Team.Teamname}]");
        };

        demo.Source1GameEvents.RoundEnd += e =>
        {
            var roundEndReason = (CSRoundEndReason) e.Reason;
            var winningTeam = (CSTeamNumber) e.Winner switch
            {
                CSTeamNumber.Terrorist => demo.TeamTerrorist,
                CSTeamNumber.CounterTerrorist => demo.TeamCounterTerrorist,
                _ => null
            };

            result.AppendLine($"\n>>> Round end: {roundEndReason}");
            result.AppendLine($"  Winner: ({winningTeam?.Teamname}) {winningTeam?.ClanTeamname}");
            result.AppendLine($"  {demo.GameRules.RoundsPlayedThisPhase} rounds played in {demo.GameRules.CSGamePhase}");
            result.AppendLine($"  Scores: {demo.TeamTerrorist.ClanTeamname} {demo.TeamTerrorist.Score} - {demo.TeamCounterTerrorist.Score} {demo.TeamCounterTerrorist.ClanTeamname}");
            result.AppendLine("");
        };

        // The result/return value is returned immediately, before
        // the section is parsed. As the result value is returned
        // immediately, it must be mutated as the section is parsed.
        return result;
    }
}