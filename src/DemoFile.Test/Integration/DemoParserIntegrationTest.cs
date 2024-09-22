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

    private static readonly KeyValuePair<string, byte[]>[] CompatibilityCases =
    {
        new("v13978", GotvProtocol13978),
        new("v13980", GotvProtocol13980),
        new("v13987", GotvProtocol13987),
        new("v13990_armsrace", GotvProtocol13990ArmsRace),
        new("v13990_dm", GotvProtocol13990Deathmatch),
        new("v14005", GotvProtocol14005),
        new("v14011", GotvProtocol14011),
        new("pov_14000", Pov14000),
    };

    [Test]
    public async Task Compatibility(
        [Values] ParseMode mode,
        [ValueSource(nameof(CompatibilityCases))] KeyValuePair<string, byte[]> testCase)
    {
        DemoSnapshot ParseSection(CsDemoParser demo)
        {
            // no-op - we're just parsing the demo to the end
            return new DemoSnapshot();
        }

        await Parse(mode, testCase.Value, ParseSection);
    }

    [Test]
    public async Task ReadAll_AlternateBaseline()
    {
        var demo = new CsDemoParser();
        var reader = DemoFileReader.Create(demo, new MemoryStream(MatchmakingProtocol13968));
        await reader.ReadAllAsync(default);
    }
}
