namespace DemoFile.Test.Integration;

[TestFixture]
public class DemoParserTest
{
    private static readonly string DemoBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos");

    [Test]
    public async Task Parse()
    {
        var demo = File.OpenRead(Path.Combine(DemoBase, "space-vs-forward-m1-ancient.dem"));
        var reader = new DemoParser();
        await reader.Start(demo, default);
    }
}
