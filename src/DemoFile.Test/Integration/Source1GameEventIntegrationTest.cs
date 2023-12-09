using System.Text;
using System.Text.Json;

namespace DemoFile.Test.Integration;

[TestFixture]
public class Source1GameEventIntegrationTest
{
    private static void SetupGameEvent(DemoParser demo, StringBuilder snapshot)
    {
        demo.Source1GameEvents.Source1GameEvent += e =>
        {
            // Ignore very noisy events
            if (e.GameEventName is "player_sound" or "player_footstep")
                return;

            snapshot.AppendLine($"[{demo.CurrentGameTick.Value}] Event {e.GameEventName}:");

            var eventJson = JsonSerializer.Serialize(e, DemoJson.SerializerOptions)
                .ReplaceLineEndings(Environment.NewLine + "  ");
            snapshot.AppendLine($"  {eventJson}");
        };
    }

    [Test]
    public async Task GameEvent()
    {
        // Arrange
        var snapshot = new StringBuilder();
        var demo = new DemoParser();

        SetupGameEvent(demo, snapshot);

        // Act
        await demo.Start(GotvCompetitiveProtocol13963, default);

        // Assert
        Snapshot.Assert(snapshot.ToString());
    }

    [Test]
    public void GameEventNonAsync()
    {
        // Arrange
        var snapshot = new StringBuilder();
        var demo = new DemoParser();

        SetupGameEvent(demo, snapshot);

        // Act
        demo.StartNonAsync(GotvCompetitiveProtocol13963);
        while (!demo.ReachedEndOfFile)
            demo.ReadNext();

        // Assert
        Snapshot.Assert(snapshot.ToString(), "GameEvent");
    }

    [Test]
    public async Task PlayerProperties()
    {
        var demo = new DemoParser();

        demo.Source1GameEvents.PlayerHurt += e =>
        {
            Assert.That(e.Player, Is.Not.Null);
            Assert.That(e.PlayerPawn, Is.Not.Null);
        };

        demo.Source1GameEvents.PlayerDeath += e =>
        {
            Assert.That(e.Player, Is.Not.Null);
            Assert.That(e.PlayerPawn, Is.Not.Null);
        };

        demo.Source1GameEvents.WeaponFire += e =>
        {
            Assert.That(e.Player, Is.Not.Null);
            Assert.That(e.PlayerPawn, Is.Not.Null);
        };

        await demo.Start(GotvCompetitiveProtocol13963, default);
    }
}
