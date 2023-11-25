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
}
