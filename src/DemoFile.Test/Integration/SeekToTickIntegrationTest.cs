using System.Text;

namespace DemoFile.Test.Integration;

[TestFixture]
public class SeekToTickIntegrationTest
{
    private static readonly DemoTick[] SeekToTickStartTicks = Enumerable.Range(1, 8)
        .Select(i => new DemoTick(i * 3840 * 8))
        .ToArray();

    [TestCaseSource(nameof(SeekToTickStartTicks))]
    public async Task SeekToTick_StartAtTick(DemoTick startTick)
    {
        // Arrange
        var demo = new DemoParser();

        // Act
        await demo.StartReadingAsync(new MemoryStream(GotvCompetitiveProtocol13992), default);
        await demo.SeekToTickAsync(startTick, default);

        while (await demo.MoveNextAsync(default))
        {
        }

        // Assert
        Assert.That(demo.CurrentDemoTick.Value, Is.EqualTo(251327));
    }

    [Test]
    public async Task SeekToTick_ReadToEndThenRestart()
    {
        // Arrange
        var phase = 0;
        var demo = new DemoParser();

        var snapshot = new StringBuilder[] {new(), new()};

        // Smallest prime below 38400 ticks (10 mins)
        var snapshotTickInterval = 38393;

        IDisposable? timer = null;

        void OnSnapshotTimer()
        {
            SnapshotPlayerInfos();
            SnapshotEntities();

            timer = demo.CreateTimer(
                demo.CurrentDemoTick + snapshotTickInterval,
                OnSnapshotTimer);
        }

        // Act
        await demo.StartReadingAsync(new MemoryStream(GotvCompetitiveProtocol13992), default);

        phase = 0;
        timer = demo.CreateTimer(new DemoTick(1), OnSnapshotTimer);

        while (await demo.MoveNextAsync(default))
        {
        }

        // Seek back to beginning
        timer?.Dispose();
        phase = 1;

        await demo.SeekToTickAsync(DemoTick.Zero, default(CancellationToken));
        Assert.That(demo.CurrentDemoTick.Value, Is.EqualTo(0));
        timer = demo.CreateTimer(new DemoTick(1), OnSnapshotTimer);

        while (await demo.MoveNextAsync(default))
        {
        }

        // Assert
        Snapshot.Assert(snapshot[1].ToString());

        Assert.That(
            snapshot[1].ToString(),
            Is.EqualTo(snapshot[0].ToString()),
            "Expected second play through to match first");

        Assert.That(demo.CurrentDemoTick.Value, Is.EqualTo(251327));


        void SnapshotPlayerInfos()
        {
            snapshot[phase].AppendLine($"[{demo.CurrentDemoTick.Value}] Player infos:");
            var playerInfos = demo.PlayerInfos.Reverse().SkipWhile(x => x == null).Reverse().ToList();
            for (var index = 0; index < playerInfos.Count; index++)
            {
                var playerInfo = demo.PlayerInfos[index];
                snapshot[phase].AppendLine($"  #{index}: {playerInfo?.ToString() ?? "<null>"}");
            }
        }

        void SnapshotEntities()
        {
            snapshot[phase].AppendLine($"[{demo.CurrentDemoTick.Value}] Entities:");
            foreach (var entity in demo.Entities)
            {
                snapshot[phase].AppendLine($"  #{entity.EntityIndex.Value}: {entity.ToString()} {{ Active = {entity.IsActive}, {entity.EntityHandle} }}");
            }
        }
    }

    [Test]
    public async Task SeekToTick_ForwardBackwards()
    {
        // Arrange
        var demo = new DemoParser();
        var skipInterval = TimeSpan.FromSeconds(77);

        // Act
        await demo.StartReadingAsync(new MemoryStream(GotvCompetitiveProtocol13992), default);
        demo.DemoEvents.DemoFileInfo += e =>
        {
            var x = demo;
            Console.WriteLine(e);
        };


        var nextSkipTick = DemoTick.Zero + skipInterval;
        DemoTick? nextSkipBackTick = DemoTick.Zero + skipInterval.Divide(2);

        while (await demo.MoveNextAsync(default))
        {
            if (nextSkipTick <= demo.CurrentDemoTick && demo.CurrentDemoTick + skipInterval < demo.TickCount)
            {
                Console.WriteLine($"Fast forward to {demo.CurrentDemoTick + skipInterval}...");
                await demo.SeekToTickAsync(demo.CurrentDemoTick + skipInterval, default);
                nextSkipTick = demo.CurrentDemoTick + skipInterval;
                nextSkipBackTick = demo.CurrentDemoTick + skipInterval.Divide(2);
            }

            if (nextSkipBackTick <= demo.CurrentDemoTick)
            {
                Console.WriteLine($"Rewind to {demo.CurrentDemoTick - skipInterval.Divide(4)}...");
                await demo.SeekToTickAsync(demo.CurrentDemoTick - skipInterval.Divide(4), default);
                nextSkipBackTick = default(DemoTick?);
            }
        }

        // Assert
        Assert.That(demo.CurrentDemoTick.Value, Is.EqualTo(251327));
    }
}
