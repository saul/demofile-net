using System.Text.Json;

namespace DemoFile.Test.Integration;

[TestFixtureSource(typeof(GlobalUtil), nameof(ParseModes))]
public class DemoEventsIntegrationTest
{
    private readonly ParseMode _mode;

    public DemoEventsIntegrationTest(ParseMode mode)
    {
        _mode = mode;
    }

    [Test]
    public async Task DemoFileInfo()
    {
        // Arrange
        DemoSnapshot ParseSection(CsDemoParser demo)
        {
            var snapshot = new DemoSnapshot();

            demo.DemoEvents.DemoFileInfo += e =>
            {
                snapshot.Add(demo.CurrentDemoTick, $"GameTick: {demo.CurrentGameTick}, TickCount: {demo.TickCount}, DemoFileInfo: {JsonSerializer.Serialize(e, DemoJson.SerializerOptions)}");
            };

            demo.PacketEvents.SvcServerInfo += e =>
            {
                snapshot.Add(demo.CurrentDemoTick, $"GameTick: {demo.CurrentGameTick}, SvcServerInfo: {JsonSerializer.Serialize(e, DemoJson.SerializerOptions)}");
            };

            return snapshot;
        }

        // Act
        var snapshot = await Parse(_mode, GotvCompetitiveProtocol13963, ParseSection);

        // Assert
        Snapshot.Assert(snapshot);
    }
}
