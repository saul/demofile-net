﻿using System.Text;
using DemoFile;

namespace DemoFile.Source1EventGen;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var (demoPath, outputDir) = args switch
        {
            [var fst, var snd] => (fst, snd),
            _ => throw new Exception("Expected format: <path to .dem> <path to output dir for .cs file>")
        };

        Console.WriteLine($"Writing output to: {outputDir}");
        var gameName = Path.GetExtension(outputDir)[1..];
        Console.WriteLine($"Using game name: {gameName}");
        var gameSdkInfo = new GameSdkInfo(gameName);

        var cts = new CancellationTokenSource();
        var demo = new DummyDemoParser();

        demo.BaseGameEvents.Source1LegacyGameEventList += events =>
        {
            var builder = new StringBuilder();
            WriteDescriptors(gameSdkInfo, builder, events.Descriptors);
            cts.Cancel();

            var outputPath = Path.Combine(outputDir, "Source1GameEvents.Autogen.cs");
            File.WriteAllText(outputPath, builder.ToString());
        };

        var reader = DemoFileReader.Create(demo, File.OpenRead(demoPath));
        try
        {
            await reader.ReadAllAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }
    }

    public static string SnakeCaseToPascalCase(string snaky)
    {
        var parts = snaky.Split('_', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = char.ToUpper(parts[i][0]) + parts[i][1..];
        }

        return string.Concat(parts);
    }

    private static string EventKeyToCsPropertyName(GameEventKeyType keyType, string eventKey)
    {
        var parts = eventKey.Split('_', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            if (keyType == GameEventKeyType.PlayerController && parts[i] == "id")
            {
                parts[i] = "";
                continue;
            }

            parts[i] = parts[i].ToLower() switch
            {
                "userid" => "Player",
                "victimid" => "Victim",
                "attackerid" => "Attacker",
                "steamid" => "SteamId",
                "xuid" => "SteamId",
                _ => char.ToUpper(parts[i][0]) + parts[i][1..]
            };
        }

        var csPropertyName = string.Concat(parts);
        return keyType switch
        {
            GameEventKeyType.StrictEHandle => csPropertyName + "Handle",
            GameEventKeyType.PlayerController => csPropertyName + "Index",
            _ => csPropertyName
        };
    }

    private static string EventNameToCsClass(string name) =>
        $"Source1{SnakeCaseToPascalCase(name)}Event";

    private static void WriteDescriptors(
        GameSdkInfo gameSdkInfo,
        StringBuilder builder,
        IReadOnlyList<CMsgSource1LegacyGameEventList.Types.descriptor_t> descriptors)
    {
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("#pragma warning disable CS1591");
        builder.AppendLine();
        builder.AppendLine("using System.Text.Json.Serialization;");
        builder.AppendLine("using DemoFile.Sdk;");
        builder.AppendLine();
        builder.AppendLine($"namespace DemoFile.Game.{gameSdkInfo.GameName};");
        builder.AppendLine();

        builder.AppendLine($"public partial class Source1GameEvents");
        builder.AppendLine($"{{");

        foreach (var descriptor in descriptors)
        {
            if (gameSdkInfo.SyntheticEvents.Contains(descriptor.Name))
                continue;

            builder.AppendLine($"    public Action<{EventNameToCsClass(descriptor.Name)}>? {SnakeCaseToPascalCase(descriptor.Name)};");
        }

        builder.AppendLine("");
        builder.AppendLine($"    internal void ParseSource1GameEventList(CMsgSource1LegacyGameEventList eventList)");
        builder.AppendLine($"    {{");
        builder.AppendLine($"        _handlers = new Dictionary<int, Action<{gameSdkInfo.DemoParserClass}, CMsgSource1LegacyGameEvent>>(eventList.Descriptors.Count);");
        builder.AppendLine($"        foreach (var descriptor in eventList.Descriptors)");
        builder.AppendLine($"        {{");

        foreach (var descriptor in descriptors)
        {
            builder.AppendLine($"            if (descriptor.Name == \"{descriptor.Name}\")");
            builder.AppendLine($"            {{");
            builder.AppendLine($"                var keys = descriptor.Keys.Select(Action<{EventNameToCsClass(descriptor.Name)}, CMsgSource1LegacyGameEvent.Types.key_t> (key) =>");
            builder.AppendLine($"                    {{");

            foreach (var key in descriptor.Keys)
            {
                builder.AppendLine($"                        if (key.Name == \"{key.Name}\")");
                builder.AppendLine($"                            return (@this, x) => @this.{EventKeyToCsPropertyName((GameEventKeyType) key.Type, key.Name)} = {CSharpEventKeyParser(gameSdkInfo, (GameEventKeyType) key.Type)};");
            }

            builder.AppendLine($"                        return (@this, x) => {{ }};");
            builder.AppendLine($"                    }})");
            builder.AppendLine($"                    .ToArray();");
            builder.AppendLine();
            builder.AppendLine($"                _handlers[descriptor.Eventid] = (demo, @event) =>");
            builder.AppendLine($"                {{");
            builder.AppendLine($"                    if (Source1GameEvent == null && {SnakeCaseToPascalCase(descriptor.Name)} == null)");
            builder.AppendLine($"                        return;");
            builder.AppendLine($"                    var @this = new {EventNameToCsClass(descriptor.Name)}(demo);");
            builder.AppendLine($"                    for (var i = 0; i < @event.Keys.Count; i++)");
            builder.AppendLine($"                    {{");
            builder.AppendLine($"                        keys[i](@this, @event.Keys[i]);");
            builder.AppendLine($"                    }}");
            builder.AppendLine($"                    {SnakeCaseToPascalCase(descriptor.Name)}?.Invoke(@this);");
            builder.AppendLine($"                    Source1GameEvent?.Invoke(@this);");
            builder.AppendLine($"                }};");
            builder.AppendLine($"            }}");
        }

        builder.AppendLine($"        }}");
        builder.AppendLine($"    }}");
        builder.AppendLine($"}}");

        foreach (var descriptor in descriptors)
        {
            builder.AppendLine();
            builder.AppendLine($"public partial class {EventNameToCsClass(descriptor.Name)} : Source1GameEventBase");
            builder.AppendLine($"{{");

            builder.AppendLine($"    internal {EventNameToCsClass(descriptor.Name)}({gameSdkInfo.DemoParserClass} demo) : base(demo) {{}}");
            builder.AppendLine($"");
            builder.AppendLine($"    public override string GameEventName => \"{descriptor.Name}\";");

            foreach (var key in descriptor.Keys)
            {
                builder.AppendLine($"");

                var csPropertyName = EventKeyToCsPropertyName((GameEventKeyType) key.Type, key.Name);
                builder.Append($"    public {CSharpEventTypeName(gameSdkInfo, (GameEventKeyType) key.Type)} {csPropertyName} {{ get; set; }}");

                if ((GameEventKeyType)key.Type == GameEventKeyType.String)
                    builder.AppendLine(" = \"\";");
                else
                    builder.AppendLine();

                if ((GameEventKeyType) key.Type == GameEventKeyType.PlayerController)
                {
                    builder.AppendLine($"    public {gameSdkInfo.PlayerControllerClass}? {csPropertyName[..^5]} => _demo.GetEntityByIndex<{gameSdkInfo.PlayerControllerClass}>({csPropertyName});");
                }
                else if ((GameEventKeyType) key.Type == GameEventKeyType.StrictEHandle && key.Name.EndsWith("_pawn"))
                {
                    builder.AppendLine($"    public {gameSdkInfo.PlayerPawnClass}? {csPropertyName[..^6]} => _demo.GetEntityByHandle({csPropertyName}) as {gameSdkInfo.PlayerPawnClass};");
                }
            }

            builder.AppendLine($"}}");
        }

        builder.AppendLine();

        foreach (var descriptor in descriptors)
        {
            builder.AppendLine($"[JsonDerivedType(typeof({EventNameToCsClass(descriptor.Name)}))]");
        }
        builder.AppendLine($"public partial class Source1GameEventBase");
        builder.AppendLine($"{{");
        builder.AppendLine($"}}");
    }

    private static string CSharpEventTypeName(GameSdkInfo gameSdkInfo, GameEventKeyType keyType)
    {
        return keyType switch
        {
            GameEventKeyType.String => $"string",
            GameEventKeyType.Float => $"float",
            GameEventKeyType.Long => $"int",
            GameEventKeyType.Short => $"int",
            GameEventKeyType.Byte => $"int",
            GameEventKeyType.Bool => $"bool",
            GameEventKeyType.UInt64 => $"ulong",
            GameEventKeyType.StrictEHandle => $"CHandle<CEntityInstance<{gameSdkInfo.DemoParserClass}>, {gameSdkInfo.DemoParserClass}>",
            GameEventKeyType.PlayerController => $"CEntityIndex",
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
        };
    }

    private static string CSharpEventKeyParser(GameSdkInfo gameSdkInfo, GameEventKeyType keyType)
    {
        return keyType switch
        {
            GameEventKeyType.String => $"x.ValString",
            GameEventKeyType.Float => $"x.ValFloat",
            GameEventKeyType.Long => $"x.ValLong",
            GameEventKeyType.Short => $"x.ValShort",
            GameEventKeyType.Byte => $"x.ValByte",
            GameEventKeyType.Bool => $"x.ValBool",
            GameEventKeyType.UInt64 => $"x.ValUint64",
            GameEventKeyType.StrictEHandle => $"CHandle<CEntityInstance<{gameSdkInfo.DemoParserClass}>, {gameSdkInfo.DemoParserClass}>.FromEventStrictEHandle((uint) x.ValLong)",
            GameEventKeyType.PlayerController => $"x.ValShort == ushort.MaxValue ? CEntityIndex.Invalid : new CEntityIndex((uint) (x.ValShort & 0xFF) + 1)",
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
        };
    }
}
