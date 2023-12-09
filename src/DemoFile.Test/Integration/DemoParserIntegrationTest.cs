namespace DemoFile.Test.Integration;

[TestFixture]
public class DemoParserIntegrationTest
{
    [Test]
    public async Task Parse()
    {
        var demo = new DemoParser();
        await demo.Start(GotvCompetitiveProtocol13963, default);
    }

    [Test]
    public async Task Parse_AlternateBaseline()
    {
        var demo = new DemoParser();
        await demo.Start(MatchmakingProtocol13968, default);
    }

    [Test]
    public void ParseNonAsync()
    {
        var demo = new DemoParser();
        demo.StartNonAsync(GotvCompetitiveProtocol13963);
        while (!demo.ReachedEndOfFile)
            demo.ReadNext();

        Assert.That(demo.CurrentDemoTick.Value, Is.EqualTo(217866));
        Assert.That(demo.CurrentGameTick.Value, Is.EqualTo(337402));
    }

    [Test]
    public void ParseNonAsync_AlternateBaseline()
    {
        var demo = new DemoParser();
        demo.StartNonAsync(MatchmakingProtocol13968);
        while (!demo.ReachedEndOfFile)
            demo.ReadNext();

        Assert.That(demo.CurrentDemoTick.Value, Is.EqualTo(78460));
        Assert.That(demo.CurrentGameTick.Value, Is.EqualTo(86245));
    }
}
