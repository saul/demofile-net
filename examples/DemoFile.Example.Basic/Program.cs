using DemoFile;
using DemoFile.Sdk;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var path = args.SingleOrDefault() ?? throw new Exception("Expected a single argument: <path to .dem>");

        var demo = new DemoParser();
        demo.Source1GameEvents.PlayerDeath += e =>
        {
            var attacker = demo.GetEntityByIndex<CCSPlayerController>(e.Attacker);
            var victim = demo.GetEntityByIndex<CCSPlayerController>(e.Userid);

            Console.WriteLine($"{attacker?.m_iszPlayerName} [{e.Weapon}] {victim?.m_iszPlayerName}");
        };

        await demo.Start(File.OpenRead(path));

        Console.WriteLine("\nFinished!");
    }
}
