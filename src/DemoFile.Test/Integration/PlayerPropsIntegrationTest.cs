using System.Text;
using System.Text.Json;

namespace DemoFile.Test.Integration;

[TestFixture(true)]
[TestFixture(false)]
public class PlayerPropsIntegrationTest
{
    private readonly bool _readAll;

    public PlayerPropsIntegrationTest(bool readAll)
    {
        _readAll = readAll;
    }

    [Test]
    public async Task Position()
    {
        // Arrange
        var snapshot = new StringBuilder();
        var demo = new DemoParser();

        demo.PacketEvents.SvcServerInfo += e =>
        {
            snapshot.AppendLine($"[{demo.CurrentGameTick.Value}] {JsonSerializer.Serialize(e, DemoJson.SerializerOptions)}");
        };

        demo.Source1GameEvents.RoundEnd += e =>
        {
            snapshot.AppendLine($"[{demo.CurrentGameTick.Value}] Round end snapshot:");
            SnapshotPlayerState();
        };

        void SnapshotPlayerState()
        {
            foreach (var player in demo.Players)
            {
                var pawn = player.PlayerPawn;

                var weapons = pawn?.Weapons.Select(wep => new
                {
                    Class = wep.ServerClass.Name,
                    Weapon = wep.EconItem.Name,
                    PaintKit = wep.EconItem.PaintKit,
                    Quality = wep.EconItem.Quality,
                    Rarity = wep.EconItem.Rarity,
                    CustomName = wep.EconItem.CustomName,
                    Clip1 = wep.Clip1,
                    Clip2 = wep.Clip2,
                    Reserve = wep.ReserveAmmo,
                });

                var playerJson = JsonSerializer.Serialize(
                        new
                        {
                            player.IsActive,
                            PlayerName = player.PlayerName,
                            Team = player.Team.ToString(),
                            IsAlive = pawn?.IsAlive,
                            ControllerHandle = player.EntityHandle,
                            PawnHandle = player.PawnHandle,
                            Kills = player.ActionTrackingServices?.MatchStats.Kills,
                            Deaths = player.ActionTrackingServices?.MatchStats.Deaths,
                            LastPlaceName = pawn?.LastPlaceName,
                            ActiveWeapon = pawn?.ActiveWeapon?.ServerClass.Name,
                            Origin = pawn?.Origin,
                            Rotation = pawn?.Rotation,
                            EyeAngles = pawn?.EyeAngles,
                            Weapons = weapons
                        },
                        DemoJson.SerializerOptions)
                    .ReplaceLineEndings(Environment.NewLine + "  ");

                snapshot.AppendLine($"  {playerJson}");
            }
        }

        var playerSnapshotInterval = TimeSpan.FromMinutes(10);
        void OnSnapshotTimer()
        {
            snapshot.AppendLine($"[{demo.CurrentGameTick.Value}] Interval snapshot:");
            SnapshotPlayerState();

            demo.CreateTimer(
                demo.CurrentDemoTick + playerSnapshotInterval,
                OnSnapshotTimer);
        }

        demo.CreateTimer(DemoTick.Zero + playerSnapshotInterval, OnSnapshotTimer);

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
