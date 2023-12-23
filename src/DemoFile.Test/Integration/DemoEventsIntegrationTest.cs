using System.Text;
using System.Text.Json;

namespace DemoFile.Test.Integration;

[TestFixture]
public class DemoEventsIntegrationTest
{
    [Test]
    public async Task DemoFileInfo()
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
}
