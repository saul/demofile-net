using System.Text;
using System.Text.Json;

namespace DemoFile.Test.Integration;

[TestFixture]
public class DemoParserIntegrationTest
{
    [Test]
    public async Task Parse()
    {
        var demo = new DemoParser();
        await demo.Start(GotvCompetitiveProtocol13963, default);
    }

    [Test]
    public async Task Parse_DemoEvents()
    {
        // Arrange
        var snapshot = new StringBuilder();
        var demo = new DemoParser();

        demo.DemoEvents.DemoFileInfo += e =>
        {
            snapshot.AppendLine($"[{demo.CurrentDemoTick}/{demo.CurrentGameTick}] TickCount={demo.TickCount}");
            snapshot.AppendLine($"[{demo.CurrentDemoTick}/{demo.CurrentGameTick}] DemoFileInfo: {JsonSerializer.Serialize(e, DemoJson.SerializerOptions)}");
        };

        demo.PacketEvents.SvcServerInfo += e =>
        {
            snapshot.AppendLine($"[{demo.CurrentDemoTick}/{demo.CurrentGameTick}] SvcServerInfo: {JsonSerializer.Serialize(e, DemoJson.SerializerOptions)}");
        };

        // Act
        await demo.Start(GotvCompetitiveProtocol13963, default);

        // Assert
        Snapshot.Assert(snapshot.ToString());
    }

    private static readonly KeyValuePair<string, Stream>[] CompatibilityCases =
    {
        new("v13978", GotvProtocol13978),
        new("v13980", GotvProtocol13980),
    };

    [TestCaseSource(nameof(CompatibilityCases))]
    public async Task Parse_Compatibility(KeyValuePair<string, Stream> testCase)
    {
        var demo = new DemoParser();
        await demo.Start(testCase.Value, default);
    }

    [Test]
    public async Task Parse_AlternateBaseline()
    {
        var demo = new DemoParser();
        await demo.Start(MatchmakingProtocol13968, default);
    }
}
