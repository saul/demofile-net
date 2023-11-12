using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DemoFile.Sdk;
using Google.Protobuf;

namespace DemoFile.Test.Integration;

[TestFixture]
public class Source1GameEventIntegrationTest
{
    // todo: finish this test
    [Test, Explicit]
    public async Task Position()
    {
        // Arrange
        var snapshot = new StringBuilder();
        var demo = new DemoParser();

        demo.PacketEvents.SvcServerInfo += e =>
        {
            e.GameSessionManifest = ByteString.Empty;
            snapshot.AppendLine($"[{demo.CurrentGameTick.Value}] {JsonSerializer.Serialize(e, DemoJson.SerializerOptions)}");
        };

        demo.Source1GameEvents.RoundEnd += e =>
        {
            snapshot.AppendLine($"[{demo.CurrentGameTick.Value}] Round end snapshot:");
            SnapshotPlayerState();
        };

        var playerSnapshotInterval = TimeSpan.FromMinutes(10);

        void SnapshotPlayerState()
        {
            foreach (var player in demo.Players)
            {
                var pawn = player.PlayerPawn;

                var weapons = pawn?.Weapons.Select(wep => new
                {
                    Weapon = wep.AttributeManager.Item.Name,
                    PaintKit = wep.AttributeManager.Item.PaintKit,
                    Quality = wep.AttributeManager.Item.Quality,
                    Rarity = wep.AttributeManager.Item.Rarity,
                    CustomName = wep.AttributeManager.Item.CustomName,
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

        void TimerCallback()
        {
            snapshot.AppendLine($"[{demo.CurrentGameTick.Value}] Timer snapshot:");
            SnapshotPlayerState();
            //demo.CreateTimer(playerSnapshotInterval, TimerCallback);
        }

        //demo.CreateTimer(playerSnapshotInterval, TimerCallback);

        // Act
        await demo.Start(GotvCompetitiveProtocol13963, default);

        // Assert
        File.WriteAllText(@"C:\Scratch\snapshot_position.txt", snapshot.ToString());
    }
}
