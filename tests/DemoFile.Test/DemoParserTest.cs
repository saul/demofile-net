namespace DemoFile.Net.Test;

[TestFixture]
public class DemoParserTest
{
    [Test]
    public async Task Parse()
    {
        var demo = File.OpenRead(@"C:\Code\demofile-net\demos\space-vs-forward-m1-ancient.dem");
        var reader = new DemoParser();
        await reader.Start(demo, default);
    }
}
