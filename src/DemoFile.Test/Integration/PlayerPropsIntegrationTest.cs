using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace DemoFile.Test.Integration;

[TestFixtureSource(typeof(GlobalUtil), nameof(ParseModes))]
public class PlayerPropsIntegrationTest
{
    private readonly ParseMode _mode;

    public PlayerPropsIntegrationTest(ParseMode mode)
    {
        _mode = mode;
    }

    [Test]
    public async Task Position()
    {
        // Arrange
        DemoSnapshot ParseSection(CsDemoParser demo)
        {
            var snapshot = new DemoSnapshot();

            demo.Source1GameEvents.RoundEnd += e =>
            {
                snapshot.Add(demo.CurrentDemoTick, $"GameTick: {demo.CurrentGameTick}, Round end snapshot:{Environment.NewLine}{SnapshotPlayerState()}");
            };

            string SnapshotPlayerState()
            {
                var sb = new StringBuilder();

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

                    sb.AppendLine($"  {playerJson}");
                }

                return sb.ToString();
            }

            var snapshotInterval = TimeSpan.FromMinutes(10);
            var snapshotIntervalTicks = DemoTick.Zero + snapshotInterval;

            void OnSnapshotTimer()
            {
                Assert.That(demo.CurrentDemoTick.Value % snapshotIntervalTicks.Value, Is.EqualTo(0));

                snapshot.Add(demo.CurrentDemoTick, $"Interval snapshot:{Environment.NewLine}{SnapshotPlayerState()}");

                demo.CreateTimer(
                    demo.CurrentDemoTick + snapshotIntervalTicks,
                    OnSnapshotTimer);
            }

            var startTick = demo.CurrentDemoTick == DemoTick.PreRecord ? DemoTick.Zero : demo.CurrentDemoTick;
            var clampedStartTick = new DemoTick(startTick.Value / (int)(snapshotInterval.TotalSeconds * CsDemoParser.TickRate) * (int)(snapshotInterval.TotalSeconds * CsDemoParser.TickRate));
            demo.CreateTimer(clampedStartTick + snapshotInterval, OnSnapshotTimer);

            return snapshot;
        }

        // Act
        var snapshot = await Parse(_mode, GotvCompetitiveProtocol13963, ParseSection);

        // Assert
        Snapshot.Assert(snapshot);
    }
}
