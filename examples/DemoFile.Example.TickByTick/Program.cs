using DemoFile;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var path = args.SingleOrDefault() ?? throw new Exception("Expected a single argument: <path to .dem>");

        var demo = new DemoParser();
        demo.Source1GameEvents.PlayerDeath += e =>
        {
            Console.WriteLine($"{e.Attacker?.PlayerName} [{e.Weapon}] {e.Player?.PlayerName}");
        };

        // Read 20 minutes of gameplay before stopping
        var readUntilTicks = DemoTick.Zero + TimeSpan.FromMinutes(20);

        await demo.StartReadingAsync(File.OpenRead(path), default(CancellationToken));
        while (demo.CurrentDemoTick < readUntilTicks)
        {
            if (!await demo.MoveNextAsync(default(CancellationToken)))
            {
                // We've reached the end of the demo file
                break;
            }
        }

        Console.WriteLine("\nFinished!");
    }
}
