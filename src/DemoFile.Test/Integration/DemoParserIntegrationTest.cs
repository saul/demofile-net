namespace DemoFile.Test.Integration;

[TestFixture]
public class DemoParserIntegrationTest
{
    [Test]
    public async Task Parse()
    {
        var reader = new DemoParser();
        await reader.Start(GotvCompetitiveProtocol13963, default);
    }

    [Test, Explicit]
    public async Task GenerateClasses()
    {
        var cts = new CancellationTokenSource();
        var demo = new DemoParser();

        demo.DemoEvents.DemoFileHeader += msg =>
        {
            Console.WriteLine($"// Network protocol {msg.NetworkProtocol}");
        };

        demo.DemoEvents.DemoClassInfo += msg =>
        {
            foreach (var classInfo in msg.Classes)
            {
                Console.WriteLine($"\"{classInfo.NetworkName}\",");
            }
            cts.Cancel();
        };

        try
        {
            await demo.Start(GotvCompetitiveProtocol13963, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
