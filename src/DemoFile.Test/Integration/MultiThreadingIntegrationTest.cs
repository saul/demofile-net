using System.Text;
using System.Text.Json;

namespace DemoFile.Test.Integration;

[TestFixture]
public class MultiThreadingIntegrationTest
{
    [TestCaseSource(typeof(GlobalUtil), nameof(GetAllFiles))]
    public async Task MTTest(KeyValuePair<string, MemoryStream> testCase)
    {
        byte[] buffer = testCase.Value.ToArray();
        var stream = new MemoryStream(buffer, 0, buffer.Length, false, true);

        // MT parsing
        var mtResult = await DemoParser.ReadAllMultiThreadedAsync(SetupParser, stream, default);

        var mtSB = new StringBuilder(mtResult.Sections.Sum(s => s.StringBuilder.Length));
        foreach (var section in mtResult.Sections)
            mtSB.Append(section.StringBuilder);

        // ST parsing
        var stParser = new DemoParser();
        var stSection = new MultiThreadedDemoParserSection(stParser);
        SetupParser(stSection);
        stream.Position = 0;
        await stParser.ReadAllAsync(stream, default);

        // compare
        Assert.That(stSection.StringBuilder.ToString(), Is.EqualTo(mtSB.ToString()));
    }

    void SetupParser(MultiThreadedDemoParserSection section)
    {
        var demo = section.DemoParser;
        var snapshot = section.StringBuilder;
        snapshot.EnsureCapacity(512);

        demo.Source1GameEvents.RoundStart += e => LogEvent(e.GameEventName, e, snapshot, demo);
        demo.Source1GameEvents.RoundEnd += e => LogEvent(e.GameEventName, e, snapshot, demo);
        demo.Source1GameEvents.PlayerDeath += e => LogEvent(e.GameEventName, e, snapshot, demo);

        // entity access
        demo.Source1GameEvents.PlayerDeath += e =>
        {
            JsonAppend(e.Attacker, snapshot);
            JsonAppend(e.AttackerPawn, snapshot);
            JsonAppend(e.Assister, snapshot);
            JsonAppend(e.AssisterPawn, snapshot);
            JsonAppend(e.Player, snapshot);
            JsonAppend(e.PlayerPawn, snapshot);

            snapshot.AppendLine($"{e.Attacker?.PlayerName} [{e.Assister?.PlayerName}] {e.Player?.PlayerName}");
        };
    }

    static void JsonAppend(object? obj, StringBuilder stringBuilder)
    {
        if (obj == null)
            return;
        var str = JsonSerializer.Serialize(obj, DemoJson.SerializerOptions);
        stringBuilder.AppendLine(str);
    }

    static void LogEvent<T>(string eventName, T evt, StringBuilder snapshot, DemoParser demo)
    {
        snapshot.AppendLine($"[{demo.CurrentDemoTick.Value}] {eventName}:");

        var eventJson = JsonSerializer.Serialize(evt, DemoJson.SerializerOptions);
        snapshot.AppendLine($"  {eventJson}");
    }
}
