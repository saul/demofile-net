﻿using DemoFile;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var path = args.SingleOrDefault() ?? throw new Exception("Expected a single argument: <path to .dem>");

        var demo = new CsDemoParser();
        demo.Source1GameEvents.PlayerDeath += e =>
        {
            Console.WriteLine($"{e.Attacker?.PlayerName} [{e.Weapon}] {e.Player?.PlayerName}");
        };

        // Read 20 minutes of gameplay before stopping
        var readUntilTicks = DemoTick.Zero + TimeSpan.FromMinutes(20);

        var reader = DemoFileReader.Create(demo, File.OpenRead(path));
        await reader.StartReadingAsync(default(CancellationToken));
        while (demo.CurrentDemoTick < readUntilTicks)
        {
            if (!await reader.MoveNextAsync(default(CancellationToken)))
            {
                // We've reached the end of the demo file
                break;
            }
        }

        Console.WriteLine("\nFinished!");
    }
}
