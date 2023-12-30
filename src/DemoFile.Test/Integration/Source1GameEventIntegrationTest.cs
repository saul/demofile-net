using System.Text;
using System.Text.Json;

namespace DemoFile.Test.Integration;

[TestFixture(true)]
[TestFixture(false)]
public class Source1GameEventIntegrationTest
{
    private readonly bool _readAll;

    public Source1GameEventIntegrationTest(bool readAll)
    {
        _readAll = readAll;
    }

    [Test]
    public async Task GameEvent()
    {
        // Arrange
        var snapshot = new StringBuilder();
        var demo = new DemoParser();

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
    }
}
