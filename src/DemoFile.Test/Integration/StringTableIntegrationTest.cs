using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DemoFile.Sdk;

namespace DemoFile.Test.Integration;

[TestFixture(true)]
[TestFixture(false)]
public class StringTableIntegrationTest
{
    private readonly bool _readAll;

    public StringTableIntegrationTest(bool readAll)
    {
        _readAll = readAll;
    }

    [Test]
    public async Task PlayerInfo()
    {
        // Arrange
        var snapshot = new StringBuilder();
        var demo = new DemoParser();

        void SnapshotPlayerInfos()
        {
            var playerInfos = demo.PlayerInfos.Reverse().SkipWhile(x => x == null).Reverse().ToList();
            for (var index = 0; index < playerInfos.Count; index++)
            {
                var playerInfo = demo.PlayerInfos[index];
                snapshot.AppendLine($"  #{index}: {playerInfo?.ToString() ?? "<null>"}");

                var controllerIndex = new CEntityIndex((uint)(index + 1));
                Debug.Assert(ReferenceEquals(
                    demo.GetEntityByIndex<CCSPlayerController>(controllerIndex)?.PlayerInfo,
                    playerInfo));
            }
        }

        var playerSnapshotInterval = TimeSpan.FromMinutes(1);
        void OnSnapshotTimer()
        {
            snapshot.AppendLine($"[{demo.CurrentGameTick.Value}] Player infos:");
            SnapshotPlayerInfos();

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
