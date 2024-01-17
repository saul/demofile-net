using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Text;

namespace DemoFile.Example.WebAssembly;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Main()");
    }

    [JSExport]
    public static async Task ParseToEnd(byte[] buffer)
    {
        Console.WriteLine($"Parsing started, buffer size {buffer.Length}");

        SetDemoParseResult(string.Empty);
        SetDemoParseProgress(0);

        var demo = new DemoParser();
        using var stream = new MemoryStream(buffer);
        var sb = new StringBuilder(1024);

        var roundNum = 0;
        demo.Source1GameEvents.RoundStart += e =>
        {
            roundNum += 1;
            sb.Append("<br />");
            sb.Append("<br />");
            sb.AppendLine($"\n\n>>> Round start [{roundNum}] <<<");
            sb.Append("<br />");
        };

        demo.Source1GameEvents.RoundFreezeEnd += e =>
        {
            sb.Append("<br />");
            sb.AppendLine("\n  > Round freeze end");
            sb.Append("<br />");
        };

        demo.Source1GameEvents.RoundEnd += e =>
        {
            sb.Append("<br />");
            sb.AppendLine("\n  > Round end");
            sb.Append("<br />");
        };

        demo.Source1GameEvents.PlayerDeath += e =>
        {
            sb.AppendLine($"{e.Attacker?.PlayerName} [{e.Weapon}] -> {e.Player?.PlayerName}");
            sb.Append("<br />");
        };

        var totalTimeStopwatch = Stopwatch.StartNew();
        var delayStopwatch = Stopwatch.StartNew();
        var processingTimeStopwatch = Stopwatch.StartNew();

        await demo.StartReadingAsync(stream, default);

        while (await demo.MoveNextAsync(default))
        {
            if (delayStopwatch.Elapsed.TotalMilliseconds > 50) // 20 FPS
            {
                processingTimeStopwatch.Stop();

                if (sb.Length > 0)
                    AppendDemoParseResult(sb.ToString());
                sb.Clear();
                SetDemoParseProgress(stream.Position / (float)stream.Length * 100f);
                await Task.Delay(1); // has to be at least 1
                delayStopwatch.Restart();

                processingTimeStopwatch.Start();
            }
        }

        sb.Append("<br /> <br />");
        sb.Append($"Finished, ticks: {demo.CurrentDemoTick}, processing time: {processingTimeStopwatch.ElapsedMilliseconds} ms, elapsed: {totalTimeStopwatch.ElapsedMilliseconds} ms");
        sb.Append("<br />");

        AppendDemoParseResult(sb.ToString());
        SetDemoParseProgress(100);
    }

    [JSExport]
    internal static string Greeting()
    {
        return $"Greetings from C#";
    }

    [JSImport("cs_setDemoParseResult", "main.js")]
    internal static partial void SetDemoParseResult([JSMarshalAs<JSType.String>] string result);

    [JSImport("cs_appendDemoParseResult", "main.js")]
    internal static partial void AppendDemoParseResult([JSMarshalAs<JSType.String>] string result);

    [JSImport("cs_setDemoParseProgress", "main.js")]
    internal static partial void SetDemoParseProgress(float progress);

    [JSImport("globalThis.console.log")]
    internal static partial void JsLog([JSMarshalAs<JSType.String>] string message);
}
