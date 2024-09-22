# DemoFile.Game.Deadlock ![NuGet](https://img.shields.io/nuget/v/DemoFile.Game.Deadlock)

DemoFile.Net is a blazing fast demo parser library for Source 2 games, written in C#. It is cross-platform, and can be
used on Windows, Mac or Linux.

> [!IMPORTANT]
> This `DemoFile.Game.Deadlock` package provides support for parsing Deadlock demos.
> See [DemoFile on NuGet](https://www.nuget.org/packages/DemoFile) for more information about the base library.

## Features

| Feature                                            | Availability   |
|----------------------------------------------------|----------------|
| TV demos                                           | ✅ Full support |
| POV demos                                          | ✅ Full support |
| HTTP broadcasts                                    | ✅ Full support  |
| Game events (e.g. `zipline_player_attached`)       | ✅ Full support |
| Entity updates (player positions, abilities, etc.) | ✅ Full support |
| Seeking forwards/backwards through the demo        | ✅ Full support |

## Examples

```c#
using DemoFile;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var path = args.SingleOrDefault() ?? throw new Exception("Expected a single argument: <path to .dem>");

        var demo = new DeadlockDemoParser();
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
