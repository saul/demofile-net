using System.Diagnostics;
using DemoFile;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var url = args.SingleOrDefault() ?? throw new Exception("Expected a single argument: <broadcast URL>");

        // Need a trailing slash on the URL
        if (url[^1] != '/')
            url += "/";

        var demo = new DeadlockDemoParser();

        demo.Source1GameEvents.Source1GameEvent += e =>
        {
            Console.WriteLine($"Source1GameEvent: {e}");
        };

        var tickInterval = TimeSpan.Zero;
        demo.PacketEvents.SvcServerInfo += e =>
        {
            tickInterval = TimeSpan.FromSeconds(e.TickInterval);
            Console.WriteLine($"[*] server info. tick rate = {1 / e.TickInterval:N0}");
        };

        var httpReader = HttpBroadcastReader.Create(demo, new Uri(url));

        Console.WriteLine("Starting stream...");
        await httpReader.StartReadingAsync(default);
        await httpReader.MoveNextAsync(default);

        // Max duration to adjust sleep interval by to correct drift between our clock and the game clock
        const double maxAdjustSecs = 0.200;

        var startTime = Stopwatch.GetTimestamp();
        var firstTick = demo.CurrentDemoTick;

        Console.WriteLine("Playing broadcast in realtime...");
        while (true)
        {
            var prevTick = demo.CurrentDemoTick;

            var sw = Stopwatch.StartNew();
            if (!await httpReader.MoveNextAsync(default))
            {
                break;
            }

            var gameElapsed = (demo.CurrentDemoTick - firstTick).Value * tickInterval;
            var wallClockElapsed = Stopwatch.GetElapsedTime(startTime);

            // Positive -> we're ahead. Negative -> we're behind the stream
            var drift = gameElapsed - wallClockElapsed;

            // Only adjust by maxAdjustSecs every call to MoveNextAsync
            var adjustmentRatio = Math.Clamp(drift.TotalSeconds / maxAdjustSecs, -1.0, 1.0);
            var adjust = TimeSpan.FromSeconds(maxAdjustSecs * adjustmentRatio);

            // Calculate how much game time elapsed during MoveNextAsync
            var ticksAdvanced = (demo.CurrentDemoTick - prevTick).Value;

            var sleepUntilNextTick = tickInterval * ticksAdvanced - sw.Elapsed + adjust;
            if (sleepUntilNextTick > TimeSpan.Zero)
            {
                await Task.Delay(sleepUntilNextTick);
            }
        }

        Console.WriteLine("\nFinished!");
    }
}
