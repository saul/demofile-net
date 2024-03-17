using System.Text;
using System.Text.Json;

namespace DemoFile.Test.Integration;

[TestFixtureSource(typeof(GlobalUtil), nameof(ParseModes))]
public class Source1GameEventIntegrationTest
{
    private readonly ParseMode _mode;

    public Source1GameEventIntegrationTest(ParseMode mode)
    {
        _mode = mode;
    }

    [Test]
    public async Task GameEvent()
    {
        // Arrange
        DemoSnapshot ParseSection(DemoParser demo)
        {
            var snapshot = new DemoSnapshot();

            demo.Source1GameEvents.Source1GameEvent += e =>
            {
                // Ignore very noisy events
                if (e.GameEventName is "player_sound" or "player_footstep")
                    return;

                var sb = new StringBuilder();
                sb.AppendLine($"Event {e.GameEventName}@{demo.CurrentGameTick}:");

                var eventJson = JsonSerializer.Serialize(e, DemoJson.SerializerOptions)
                    .ReplaceLineEndings(Environment.NewLine + "  ");
                sb.AppendLine($"  {eventJson}");

                snapshot.Add(demo.CurrentDemoTick, sb.ToString());
            };

            return snapshot;
        }

        // Act
        var snapshot = await Parse(_mode, GotvCompetitiveProtocol13963, ParseSection);

        // Assert
        Snapshot.Assert(snapshot);
    }

    [Test]
    public async Task PlayerProperties()
    {
        DemoSnapshot ParseSection(DemoParser demo)
        {
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

            return new DemoSnapshot();
        }

        // Act
        await Parse(_mode, GotvCompetitiveProtocol13963, ParseSection);
    }
}
