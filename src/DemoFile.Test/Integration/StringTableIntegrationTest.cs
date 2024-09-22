using System.Diagnostics;
using System.Text;
using DemoFile.Game.Cs;
using DemoFile.Sdk;

namespace DemoFile.Test.Integration;

[TestFixtureSource(typeof(GlobalUtil), nameof(ParseModes))]
public class StringTableIntegrationTest
{
    private readonly ParseMode _mode;

    public StringTableIntegrationTest(ParseMode mode)
    {
        _mode = mode;
    }

    [Test]
    public async Task PlayerInfo()
    {
        // Arrange
        DemoSnapshot ParseSection(CsDemoParser demo)
        {
            var snapshot = new DemoSnapshot();

            string SnapshotPlayerInfos()
            {
                var sb = new StringBuilder();
                var playerInfos = demo.PlayerInfos.Reverse().SkipWhile(x => x == null).Reverse().ToList();

                for (var index = 0; index < playerInfos.Count; index++)
                {
                    var playerInfo = demo.PlayerInfos[index];
                    sb.AppendLine($"  #{index}: {playerInfo?.ToString() ?? "<null>"}");

                    var controllerIndex = new CEntityIndex((uint) (index + 1));
                    Debug.Assert(ReferenceEquals(
                        demo.GetEntityByIndex<CCSPlayerController>(controllerIndex)?.PlayerInfo,
                        playerInfo));
                }

                return sb.ToString();
            }

            var snapshotInterval = TimeSpan.FromMinutes(1);
            var snapshotIntervalTicks = DemoTick.Zero + snapshotInterval;

            void OnSnapshotTimer()
            {
                if (demo.CurrentDemoTick.Value % snapshotIntervalTicks.Value != 0)
                    return;

                snapshot.Add(demo.CurrentDemoTick, $"Player infos:{Environment.NewLine}{SnapshotPlayerInfos()}");

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
