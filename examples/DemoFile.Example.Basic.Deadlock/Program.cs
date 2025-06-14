﻿using DemoFile;

var path = args.SingleOrDefault() ?? throw new Exception("Expected a single argument: <path to .dem>");

var demo = new DeadlockDemoParser();

demo.PacketEvents.SvcServerInfo += e =>
{
    Console.WriteLine($"{e}");
};

demo.Source1GameEvents.PlayerDeath += e =>
{
    Console.WriteLine($"{e.Attacker?.PlayerName} [{e.Weapon}] {e.Player?.PlayerName}");
};

var reader = DemoFileReader.Create(demo, File.OpenRead(path));
await reader.ReadAllAsync();

Console.WriteLine("\nFinished!");
