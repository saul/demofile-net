# DemoFile.Net ![NuGet](https://img.shields.io/nuget/v/DemoFile) ![Build status](https://github.com/saul/demofile-net/actions/workflows/dotnet.yml/badge.svg)

DemoFile.Net is a blazing fast demo parser library for Source 2 games, written in C#. It is cross-platform, and can be
used on Windows, Mac or Linux. This parser currently supports:

| Game             | NuGet package                                                                     | Getting started            |
|------------------|-----------------------------------------------------------------------------------|----------------------------|
| Counter-Strike 2 | ✅ [DemoFile.Game.Cs](https://www.nuget.org/packages/DemoFile.Game.Cs)             | `new CsDemoParser()`       |
| Deadlock         | ✅ [DemoFile.Game.Deadlock](https://www.nuget.org/packages/DemoFile.Game.Deadlock) | `new DeadlockDemoParser()` |
| Dota 2           | ✅ [DemoFile.Game.Dota](https://www.nuget.org/packages/DemoFile.Game.Dota)         | `new DotaDemoParser()`     |

> [!IMPORTANT]
> `DemoFile` is the base, core library and does not provide support for parsing any specific game.
> Add a reference to one of the `DemoFile.Game.*` packages instead.

![Screenshot of DemoFile.Net](https://raw.githubusercontent.com/saul/demofile-net/main/assets/screenshot-2x.png)

Easy discoverability of available data through your IDE's inbuilt autocompletion:

| ![](https://raw.githubusercontent.com/saul/demofile-net/main/assets/ide-1.png) | ![](https://raw.githubusercontent.com/saul/demofile-net/main/assets/ide-2.png) |
|-------------------------|-------------------------|
| ![](https://raw.githubusercontent.com/saul/demofile-net/main/assets/ide-3.png) | ![](https://raw.githubusercontent.com/saul/demofile-net/main/assets/ide-4.png) |

## Features

| Feature                                           | Availability   |
|---------------------------------------------------|----------------|
| CSTV / GOTV demos                                 | ✅ Full support |
| POV demos                                         | ✅ Full support |
| HTTP broadcasts                                   | ✅ Full support  |
| Game events (e.g. `player_death`)                 | ✅ Full support |
| Entity updates (player positions, grenades, etc.) | ✅ Full support |
| Seeking forwards/backwards through the demo       | ✅ Full support |

## Examples

> [!WARNING]
> This library is still under development and the API is liable to change until v1.0

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

See also the [examples/](https://github.com/saul/demofile-net/tree/main/examples) folder.

For maximum performance, a given demo file can be parsed in sections in parallel.
This utilises all available CPU cores. For example usage, take a look at [DemoFile.Example.MultiThreaded](./examples/DemoFile.Example.MultiThreaded/Program.cs).  

## Benchmarks

On an M1 MacBook Pro, DemoFile.Net can read a full competitive game (just under 1 hour of game time) in 1.3 seconds.
When parsing across multiple threads, using the `ReadAllParallelAsync` method, this drops to nearly 500 milliseconds.
This includes parsing all entity data (player positions, velocities, weapon tracking, grenades, etc).

| Method            |           Mean |    Error |   StdDev | Allocated |
|-------------------|---------------:|---------:|---------:|----------:|
| ParseDemo         | **1,294.6 ms** |  3.68 ms |  2.88 ms | 491.48 MB |
| ParseDemoParallel |   **540.1 ms** | 23.99 ms | 22.44 ms | 600.67 MB |

## Author and acknowledgements

DemoFile.Net is developed by [Saul Rennison](https://saul.re). The development of this library would not have been
possible without [demoparser by LaihoE](https://github.com/LaihoE/demoparser)
and [Manta by Dotabuff](https://raw.githubusercontent.com/dotabuff/manta/master/README.md), the latter of which depends
on the efforts of a number of people:

- [Michael Fellinger](https://github.com/manveru) built Dotabuff's Source 1
  parser [yasha](https://github.com/dotabuff/yasha).
- [Robin Dietrich](https://github.com/invokr) built the C++ parser [Alice](https://github.com/AliceStats/Alice).
- [Martin Schrodt](https://github.com/spheenik) built the Java parser [clarity](https://github.com/skadistats/clarity).
- [Drew Schleck](https://github.com/dschleck) built an original C++ parser [edith](https://github.com/dschleck/edith).

A modified version of [Source2Gen by neverlosecc](https://github.com/neverlosecc/source2gen) is used to statically
generate the game schema classes and enums.

See [ACKNOWLEDGEMENTS](./ACKNOWLEDGEMENTS) for license information.
