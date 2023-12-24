namespace DemoFile.Test.Integration;

[TestFixture]
public class DemoParserIntegrationTest
{
    [Test]
    public async Task ReadAll()
    {
        var demo = new DemoParser();
        await demo.ReadAllAsync(GotvCompetitiveProtocol13963, default);
        Assert.That(demo.CurrentDemoTick.Value, Is.EqualTo(217866));
    }

    [Test]
    public async Task ByTick()
    {
        // Arrange
        var demo = new DemoParser();
        var tick = demo.CurrentDemoTick;

        // Act
        await demo.StartReadingAsync(GotvCompetitiveProtocol13963, default);
        while (await demo.MoveNextAsync(default))
        {
            // Tick is monotonic
            Assert.That(demo.CurrentDemoTick.Value, Is.GreaterThanOrEqualTo(tick.Value));
            tick = demo.CurrentDemoTick;
        }

        // Assert
        Assert.That(demo.CurrentDemoTick.Value, Is.EqualTo(217866));
    }

    private static readonly KeyValuePair<string, Stream>[] CompatibilityCases =
    {
        new("v13978", GotvProtocol13978),
        new("v13980", GotvProtocol13980),
    };

    [TestCaseSource(nameof(CompatibilityCases))]
    public async Task ReadAll_Compatibility(KeyValuePair<string, Stream> testCase)
    {
        var demo = new DemoParser();
        await demo.ReadAllAsync(testCase.Value, default);
    }

    [Test]
    public async Task ReadAll_AlternateBaseline()
    {
        var demo = new DemoParser();
        await demo.ReadAllAsync(MatchmakingProtocol13968, default);
    }
}
