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
        var cts = new CancellationTokenSource();

        var roundNum = 0;
        demo.Source1GameEvents.RoundStart += e =>
        {
            roundNum += 1;
            Console.WriteLine($"\n\n>>> Round start [{roundNum}] <<<");
        };

        demo.EntityEvents.CCSPlayerPawn.AddCollectionChangeCallback(pawn => pawn.Grenades, (pawn, oldGrenades, newGrenades) =>
        {
            Console.Write($"  [Tick {demo.CurrentGameTick.Value}] ");
            MarkupPlayerName(pawn.Controller);
            AnsiConsole.MarkupLine($" grenades changed [grey]{string.Join(", ", oldGrenades.Select(x => x.ServerClass.Name))}[/] => [bold]{string.Join(", ", newGrenades.Select(x => x.ServerClass.Name))}[/]");
        });

        demo.Source1GameEvents.RoundFreezeEnd += e =>
        {
            Console.WriteLine("\n  > Round freeze end");
            DumpGrenadeInventory();
        };

        demo.Source1GameEvents.WeaponFire += e =>
        {
            if (!e.Weapon.Contains("nade") && !e.Weapon.Contains("molotov"))
                return;

            Console.Write($"  [Tick {demo.CurrentGameTick.Value}] ");
            MarkupPlayerName(e.Player);
            AnsiConsole.MarkupLine($" [bold]threw a {e.Weapon}[/]");
        };

        demo.Source1GameEvents.RoundEnd += e =>
        {
            Console.WriteLine("\n  > Round end");
            DumpGrenadeInventory();

            if (roundNum == 2)
                cts.Cancel();
        };

        void DumpGrenadeInventory()
        {
            foreach (var player in demo.Players)
            {
                Console.Write("    ");
                MarkupPlayerName(player);
                Console.Write(" - ");

                if (player.PlayerPawn is not {} pawn)
                {
                    Console.WriteLine("<no pawn>");
                    continue;
                }

                if (!pawn.IsAlive)
                {
                    Console.WriteLine("<dead>");
                    continue;
                }

                var grenades = pawn.Grenades;
                foreach (var grenade in grenades)
                {
                    Console.Write($"{grenade.ServerClass.Name} x {grenade.GrenadeCount}, ");
                }

                Console.WriteLine("");
            }
        }

        // Now that we've attached the event listeners, start reading the demo
        var sw = Stopwatch.StartNew();
        try
        {
            await demo.ReadAllAsync(File.OpenRead(path), cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }
        sw.Stop();

        var ticks = demo.CurrentDemoTick.Value;
        AnsiConsole.MarkupLine($"\n[bold green]Finished![/] Parsed [bold white]{ticks:N0} ticks[/] ({demo.CurrentGameTime.Value:N1} game secs) in [bold white]{sw.Elapsed.TotalSeconds:0.000} secs[/] ({ticks * 1000 / sw.Elapsed.TotalMilliseconds:N1} ticks/sec)");
    }

    private static readonly string[] PlayerColours =
    {
        "#FF0000",
        "#FF7F00",
        "#FFD700",
        "#7FFF00",
        "#00FF00",
        "#00FF7F",
        "#00FFFF",
        "#007FFF",
        "#0000FF",
        "#7F00FF",
        "#FF00FF",
        "#FF007F",
    };

    private static void MarkupPlayerName(CBasePlayerController? player)
    {
        if (player == null)
        {
            AnsiConsole.Markup("[grey](unknown)[/]");
            return;
        }

        AnsiConsole.Markup($"[{PlayerColours[player.EntityIndex.Value % PlayerColours.Length]}]{player.PlayerName}[/]");
    }
}
