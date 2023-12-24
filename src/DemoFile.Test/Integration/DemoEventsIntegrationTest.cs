using System.Text;
using System.Text.Json;

namespace DemoFile.Test.Integration;

[TestFixture(true)]
[TestFixture(false)]
public class DemoEventsIntegrationTest
{
    private readonly bool _readAll;

    public DemoEventsIntegrationTest(bool readAll)
    {
        _readAll = readAll;
    }

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
        if (_readAll)
        {
            await demo.ReadAllAsync(GotvCompetitiveProtocol13963, default);
        }
        else
        {
            await demo.StartReadingAsync(GotvCompetitiveProtocol13963, default);
            while (await demo.MoveNextAsync(default))
            {
            }
        }

        // Assert
        Snapshot.Assert(snapshot.ToString());
    }
}
