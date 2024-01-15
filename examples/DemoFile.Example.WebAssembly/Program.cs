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
    public static async Task<string> ParseToEnd(byte[] buffer)
    {
        Console.WriteLine("Parsing started");

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

        var sw = Stopwatch.StartNew();

        await demo.ReadAllAsync(stream);

        sb.Append("<br />");
        sb.AppendLine($"Finished, ticks: {demo.CurrentDemoTick}, elapsed: {sw.ElapsedMilliseconds} ms");
        sb.Append("<br />");

        return sb.ToString();
    }

    [JSExport]
    internal static string Greeting()
    {
        return $"Greetings from C#";
    }

    [JSImport("globalThis.console.log")]
    internal static partial void JsLog([JSMarshalAs<JSType.String>] string message);
}
