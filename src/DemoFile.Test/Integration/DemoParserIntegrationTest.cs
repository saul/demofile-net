using System.Text;

namespace DemoFile.Test.Integration;

[TestFixture]
public class DemoParserIntegrationTest
{
    [Test]
    public async Task ReadAll()
    {
        var demo = new CsDemoParser();
        var reader = DemoFileReader.Create(demo, new MemoryStream(GotvCompetitiveProtocol13963));
        await reader.ReadAllAsync(default);
        Assert.That(demo.CurrentDemoTick.Value, Is.EqualTo(217866));
    }

    [Test]
    public async Task ByTick()
    {
        // Arrange
        var demo = new CsDemoParser();
        var tick = demo.CurrentDemoTick;

        // Act
        var reader = DemoFileReader.Create(demo, new MemoryStream(GotvCompetitiveProtocol13963));
        await reader.StartReadingAsync(default);
        while (await reader.MoveNextAsync(default))
        {
            // Tick is monotonic
            Assert.That(demo.CurrentDemoTick.Value, Is.GreaterThanOrEqualTo(tick.Value));
            tick = demo.CurrentDemoTick;
        }

        // Assert
        Assert.That(demo.CurrentDemoTick.Value, Is.EqualTo(217866));
    }

    public record CompatibilityTestCase(
        string Name,
        byte[] DemoFileBytes,
        DemoTick ExpectedLastTick)
    {
        public override string ToString() => Name;
    }

    private static readonly CompatibilityTestCase[] CompatibilityCases =
    {
        new("v13978", GotvProtocol13978, new DemoTick(164)),
        new("v13980", GotvProtocol13980, new DemoTick(134)),
        new("v13987", GotvProtocol13987, new DemoTick(1106)),
        new("v13990_armsrace", GotvProtocol13990ArmsRace, new DemoTick(219)),
        new("v13990_dm", GotvProtocol13990Deathmatch, new DemoTick(303)),
        new("v14005", GotvProtocol14005, new DemoTick(293)),
        new("v14011", GotvProtocol14011, new DemoTick(391)),
        new("pov_14000", Pov14000, new DemoTick(127743)),
    };

    [Test]
    public async Task Compatibility(
        [Values] ParseMode mode,
        [ValueSource(nameof(CompatibilityCases))] CompatibilityTestCase testCase)
    {
        Func<DemoTick> ParseSection(CsDemoParser demo)
        {
            var lastTick = DemoTick.PreRecord;
            demo.OnCommandFinish += OnCommandFinish;

            return () => lastTick;

            void OnCommandFinish()
            {
                demo.OnCommandFinish += OnCommandFinish;
                lastTick = demo.CurrentDemoTick;
            }
        }

        DemoTick lastTick;
        if (mode == ParseMode.ReadAll)
        {
            var demo = new CsDemoParser();
            var stream = new MemoryStream(testCase.DemoFileBytes);

            var getLastTick = ParseSection(demo);

            var reader = DemoFileReader.Create(demo, stream);
            await reader.ReadAllAsync(default);

            lastTick = getLastTick();
        }
        else if (mode == ParseMode.ByTick)
        {
            var demo = new CsDemoParser();
            var stream = new MemoryStream(testCase.DemoFileBytes);

            var getLastTick = ParseSection(demo);

            var reader = DemoFileReader.Create(demo, stream);
            await reader.StartReadingAsync(default);
            while (await reader.MoveNextAsync(default))
            {
            }

            lastTick = getLastTick();
        }
        else if (mode == ParseMode.ReadAllParallel)
        {
            var results = await DemoFileReader<CsDemoParser>.ReadAllParallelAsync(testCase.DemoFileBytes, ParseSection, default);

            lastTick = results.Select(x => x()).Max();
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown parse mode");
        }

        Assert.That(lastTick, Is.EqualTo(testCase.ExpectedLastTick));
    }

    [TestCase]
    public async Task CreateDeleteEvents()
    {
        var demo = new CsDemoParser();
        var snapshot = new DemoSnapshot();
        var cts = new CancellationTokenSource();

        demo.EntityEvents.CEntityInstance.Create += e =>
        {
            snapshot.Add(demo.CurrentDemoTick, $"Create: {e.ServerClass.Name} - {e.EntityHandle}");
        };
        demo.EntityEvents.CEntityInstance.Delete += e =>
        {
            snapshot.Add(demo.CurrentDemoTick, $"Delete: {e.ServerClass.Name} -{e.EntityHandle}");
        };

        var roundEnds = 0;
        demo.Source1GameEvents.RoundEnd += e =>
        {
            if (roundEnds++ == 4)
                cts.Cancel();
        };

        var reader = DemoFileReader.Create(demo, new MemoryStream(Pov14000));
        try
        {
            await reader.ReadAllAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }

        Snapshot.Assert(snapshot.ToString());
    }

    [Test]
    public async Task ReadAll_AlternateBaseline()
    {
        var demo = new CsDemoParser();
        var reader = DemoFileReader.Create(demo, new MemoryStream(MatchmakingProtocol13968));
        await reader.ReadAllAsync(default);
    }

    [Test]
    public async Task POV_Parallel_InitialSectionProcessed()
    {
        var list = await DemoFileReader<CsDemoParser>.ReadAllParallelAsync(
            Pov14000,
            demo =>
            {
                var sb = new StringBuilder();
                demo.DemoEvents.DemoFileInfo += e => sb.Append(e.PlaybackTicks + " ");
                demo.DemoEvents.DemoFileHeader += e => sb.Append(e.NetworkProtocol + " ");
                demo.PacketEvents.SvcServerInfo += e => sb.Append(e.MapName + " ");
                return sb;
            },
            CancellationToken.None);
        
        Assert.That(list, Has.Count.EqualTo(2));
        Assert.That(list[0].ToString(), Is.EqualTo("127743 "));
        Assert.That(list[1].ToString(), Is.EqualTo("14000 de_inferno "));
    }
}
