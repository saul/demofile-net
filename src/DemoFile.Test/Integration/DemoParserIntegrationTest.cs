using System.Text;
using System.Text.Json;

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

    private static readonly KeyValuePair<string, Stream>[] CompatibilityCases =
    {
        new("v13978", GotvProtocol13978),
        new("v13980", GotvProtocol13980),
    };

    [TestCaseSource(nameof(CompatibilityCases))]
    public async Task Parse_Compatibility(KeyValuePair<string, Stream> testCase)
    {
        var demo = new DemoParser();
        await demo.Start(testCase.Value, default);
    }

    [Test]
    public async Task Parse_AlternateBaseline()
    {
        var demo = new DemoParser();
        await demo.Start(MatchmakingProtocol13968, default);
    }
}
