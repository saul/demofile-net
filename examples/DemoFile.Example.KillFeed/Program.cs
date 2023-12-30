using System.Diagnostics;
using DemoFile;
using DemoFile.Sdk;
using Spectre.Console;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var path = args.SingleOrDefault() ?? throw new Exception("Expected a single argument: <path to .dem>");

        var demo = new DemoParser();

        demo.Source1GameEvents.PlayerDeath += e =>
        {
            // Write attacker name in the colour of their team
            AnsiConsole.Markup($"[{TeamNumberToString(e.Attacker?.CSTeamNum)}]{e.Attacker?.PlayerName}[/]");

            // Write the weapon
            AnsiConsole.Markup(" <");
            AnsiConsole.Markup(e.Weapon);
            if (e.Headshot)
                AnsiConsole.Markup(" HS");
            AnsiConsole.Markup("> ");

            // Write the victim's name in the colour of their team
            AnsiConsole.MarkupLine($"[{TeamNumberToString(e.Player?.CSTeamNum)}]{e.Player?.PlayerName}[/]");
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

            AnsiConsole.MarkupLine($"\n>>> Round end: [bold white]{roundEndReason}[/]");
            AnsiConsole.MarkupLine($"  Winner: [{TeamNumberToString((CSTeamNumber) e.Winner)}]({winningTeam?.Teamname}) {winningTeam?.ClanTeamname}[/]");
            AnsiConsole.MarkupLine($"  {demo.GameRules.RoundsPlayedThisPhase} rounds played in {demo.GameRules.CSGamePhase}");
            AnsiConsole.MarkupLine($"  Scores: [red]{demo.TeamTerrorist.ClanTeamname}[/] {demo.TeamTerrorist.Score} - {demo.TeamCounterTerrorist.Score} [blue]{demo.TeamCounterTerrorist.ClanTeamname}[/]");
            AnsiConsole.WriteLine("");
        };

        // Now that we've attached the event listeners, start reading the demo
        var sw = Stopwatch.StartNew();
        await demo.ReadAllAsync(File.OpenRead(path));
        sw.Stop();

        var ticks = demo.CurrentDemoTick.Value;
        AnsiConsole.MarkupLine($"\n[bold green]Finished![/] Parsed [bold white]{ticks:N0} ticks[/] ({demo.CurrentGameTime.Value:N1} game secs) in [bold white]{sw.Elapsed.TotalSeconds:0.000} secs[/] ({ticks * 1000 / sw.Elapsed.TotalMilliseconds:N1} ticks/sec)");
    }

    private static string TeamNumberToString(CSTeamNumber? csTeamNumber) => csTeamNumber switch
    {
        CSTeamNumber.Terrorist => "bold red",
        CSTeamNumber.CounterTerrorist => "bold blue",
        _ => "bold white",
    };
}
