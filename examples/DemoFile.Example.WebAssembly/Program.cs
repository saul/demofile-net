using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Text;

namespace DemoFile.Example.WebAssembly;

public partial class Program
{
    static DemoParser? m_demoParser;
    static StringBuilder m_stringBuilder = new(1024);
    static MemoryStream? m_stream;
    static int m_roundNumber = 0;
    static Stopwatch m_updateStopwatch = new();
    static double m_processingTime = 0;
    static Stopwatch m_totalDurationStopwatch = new();


    [JSExport]
    public static void OpenDemo(byte[] buffer)
    {
        CloseDemo();

        Console.WriteLine("Parsing started");

        m_totalDurationStopwatch.Restart();

        m_demoParser = new DemoParser();
        m_stream = new MemoryStream(buffer);

        var sb = m_stringBuilder;

        m_demoParser.Source1GameEvents.RoundStart += e =>
        {
            m_roundNumber += 1;
            sb.Append("<br />");
            sb.Append("<br />");
            sb.AppendLine($"\n\n>>> Round start [{m_roundNumber}] <<<");
            sb.Append("<br />");
        };

        m_demoParser.Source1GameEvents.RoundFreezeEnd += e =>
        {
            sb.Append("<br />");
            sb.AppendLine("\n  > Round freeze end");
            sb.Append("<br />");
        };

        m_demoParser.Source1GameEvents.RoundEnd += e =>
        {
            sb.Append("<br />");
            sb.AppendLine("\n  > Round end");
            sb.Append("<br />");
        };

        m_demoParser.Source1GameEvents.PlayerDeath += e =>
        {
            sb.AppendLine($"{e.Attacker?.PlayerName} [{e.Weapon}] -> {e.Player?.PlayerName}");
            sb.Append("<br />");
        };
    }

    static void CloseDemo()
    {
        m_demoParser = null;
        m_stringBuilder.Clear();
        m_stream?.Dispose();
        m_stream = null;
        m_roundNumber = 0;
        m_processingTime = 0;
    }

    [JSExport]
    public static async Task UpdateAsync(float maxTimeMs)
    {
        if (m_demoParser is null)
            return;

        m_updateStopwatch.Restart();

        bool endOfDemo = false;

        while (m_updateStopwatch.Elapsed.TotalMilliseconds < maxTimeMs)
        {
            if (!await m_demoParser.MoveNextAsync(default))
            {
                endOfDemo = true;
                break;
            }
        }

        m_processingTime += m_updateStopwatch.Elapsed.TotalMilliseconds;

        if (endOfDemo)
        {
            m_stringBuilder.Append("<br /> <br />");
            m_stringBuilder.Append($"Finished, ticks: {m_demoParser.CurrentDemoTick}, processing time: {m_processingTime} ms, total elapsed: {m_totalDurationStopwatch.Elapsed.TotalMilliseconds} ms");
            m_stringBuilder.Append("<br />");

            CloseDemo();
        }
    }

    [JSExport]
    internal static string Greeting()
    {
        return $"Greetings from C#";
    }

    [JSImport("globalThis.console.log")]
    internal static partial void JsLog([JSMarshalAs<JSType.String>] string message);
}
