# DemoFile.Game.Cs ![NuGet](https://img.shields.io/nuget/v/DemoFile.Game.Cs)

DemoFile.Net is a blazing fast demo parser library for Source 2 games, written in C#. It is cross-platform, and can be
used on Windows, Mac or Linux.

> [!IMPORTANT]
> This `DemoFile.Game.Cs` package provides support for parsing Counter-Strike 2 demos.
> See [DemoFile on NuGet](https://www.nuget.org/packages/DemoFile) for more information about the base library.

## Features

| Feature                                           | Availability   |
|---------------------------------------------------|----------------|
| CSTV / GOTV demos                                 | ✅ Full support |
| POV demos                                         | ✅ Full support |
| HTTP broadcasts                                   | ✅ Full support |
| Game events (e.g. `player_death`)                 | ✅ Full support |
| Entity updates (player positions, grenades, etc.) | ✅ Full support |
| Seeking forwards/backwards through the demo       | ✅ Full support |

## Examples

```c#
using DemoFile;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var path = args.SingleOrDefault() ?? throw new Exception("Expected a single argument: <path to .dem>");

        var demo = new CsDemoParser();
        demo.Source1GameEvents.PlayerDeath += e =>
        {
            Console.WriteLine($"{e.Attacker?.PlayerName} [{e.Weapon}] {e.Player?.PlayerName}");
        };

        var reader = DemoFileReader.Create(demo, File.OpenRead(path));
        await reader.ReadAllAsync();

        Console.WriteLine("\nFinished!");
    }
}
```
