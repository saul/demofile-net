using System.Diagnostics;
using System.Net;
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

        var httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
        {
            BaseAddress = new Uri(url)
        };
        var httpReader = HttpBroadcastReader.Create(demo, httpClient);

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

            // +ve = we're ahead, -ve = we're behind
            var drift = gameElapsed - wallClockElapsed;
            var driftRatio = Math.Clamp(drift.TotalSeconds / maxAdjustSecs, -1.0, 1.0);
            var adjust = TimeSpan.FromSeconds(drift.TotalSeconds * maxAdjustSecs * driftRatio);

            if (adjust != default)
            {
                Console.WriteLine($"  {gameElapsed.TotalSeconds:N3} game secs over {wallClockElapsed.TotalSeconds:N3} secs ({drift.TotalSeconds:N3} secs ahead, adjusting by {adjust.TotalSeconds:N3} secs)");
            }

            var ticksAdvanced = (demo.CurrentDemoTick - prevTick).Value;

            var waitUntilNextTick = tickInterval * ticksAdvanced - sw.Elapsed + adjust;
            if (waitUntilNextTick > TimeSpan.Zero)
            {
                var health = httpReader.BufferTailTick - demo.CurrentDemoTick;
                Console.WriteLine($"[*] advanced {ticksAdvanced} ticks, sleeping.  current = {demo.CurrentDemoTick}, tail = {httpReader.BufferTailTick}, health = {health} ticks ({health.Value * tickInterval.TotalSeconds:N3} secs)");
                await Task.Delay(waitUntilNextTick);
            }
        }

        Console.WriteLine("\nFinished!");
    }
}
