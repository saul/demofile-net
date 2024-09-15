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
            Console.WriteLine(e.GameEventName);
        };

        var httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
        {
            BaseAddress = new Uri(url)
        };
        var broadcastClient = HttpBroadcastReader.Create(demo, httpClient);
        await broadcastClient.StartReadingAsync(default);

        const int tickRate = 60; // todo: replace fixed tickrate
        var tickInterval = TimeSpan.FromSeconds(1.0 / tickRate);
        var tickTimer = Stopwatch.StartNew();

        var bufferTicks = tickRate * 6;
        while (broadcastClient.BufferTailTick <= demo.CurrentDemoTick + bufferTicks)
        {
            // Ensure we have at least `bufferTicks` worth of data buffered,
            // to avoid stalls during reading
            Console.WriteLine("[*] buffering...");
            await Task.Delay(1000);
        }

        // Read a tick every `tickInterval`
        while (true)
        {
            tickTimer.Restart();
            if (!await broadcastClient.MoveNextAsync(default))
            {
                break;
            }

            var waitUntilNextTick = tickInterval - tickTimer.Elapsed;
            if (waitUntilNextTick > TimeSpan.Zero)
            {
                Console.WriteLine($"[*] sleeping for {waitUntilNextTick} (buffer health = {broadcastClient.BufferTailTick - demo.CurrentDemoTick})");
                await Task.Delay(waitUntilNextTick);
            }
        }

        Console.WriteLine("\nFinished!");
    }
}
