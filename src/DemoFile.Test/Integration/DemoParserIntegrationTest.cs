namespace DemoFile.Test.Integration;

[TestFixture]
public class DemoParserIntegrationTest
{
    [Test]
    public async Task Parse()
    {
        var reader = new DemoParser();
        await reader.Start(SpaceVsForwardM1Stream, default);
    }
}
