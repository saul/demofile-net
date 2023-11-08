using System.Collections.Immutable;
using System.Text;
using ValveKeyValue;

internal class Program
{
    public static void Main(string[] args)
    {
        var (itemGamesPath, outputPath) = args switch
        {
            [var fst, var snd] => (fst, snd),
            _ => throw new Exception("Expected arguments: <path to items_game.txt> <output dir for .cs files>")
        };

        var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
        var data = kv.Deserialize(File.OpenRead(itemGamesPath));

        var paintKits = data
            .Where(x => x.Name == "paint_kits")
            .SelectMany(x => x.Children)
            .Where(x => int.TryParse(x.Name, out _))
            .ToImmutableDictionary(x => x.Name);

        var items = data
            .Where(x => x.Name == "items")
            .SelectMany(x => x.Children)
            .Where(x => int.TryParse(x.Name, out _))
            .ToImmutableDictionary(x => x.Name);

        var attributes = data
            .Where(x => x.Name == "attributes")
            .SelectMany(x => x.Children)
            .Where(x => int.TryParse(x.Name, out _))
            .ToImmutableDictionary(x => x.Name);

        var qualities = data
            .Where(x => x.Name == "qualities")
            .SelectMany(x => x.Children)
            .Select(x => (Name: x.Name, Value: x["value"].ToInt32(null)));

        var rarities = data
            .Where(x => x.Name == "rarities")
            .SelectMany(x => x.Children)
            .Select(x => (Name: x.Name, Value: x["value"].ToInt32(null)));

        var builder = new StringBuilder();

        builder.AppendLine($"namespace DemoFile.GameStatic;");
        builder.AppendLine($"");
        builder.AppendLine($"public enum ItemQuality");
        builder.AppendLine($"{{");
        foreach (var (quality, value) in qualities)
        {
            builder.AppendLine($"    {char.ToUpper(quality[0])}{quality[1..]} = {value},");
        }
        builder.AppendLine($"}}");
        builder.AppendLine($"");
        builder.AppendLine($"public enum ItemRarity");
        builder.AppendLine($"{{");
        foreach (var (rarity, value) in rarities)
        {
            builder.AppendLine($"    {char.ToUpper(rarity[0])}{rarity[1..]} = {value},");
        }
        builder.AppendLine($"}}");
        builder.AppendLine($"");
        builder.AppendLine($"public static class GameItems");
        builder.AppendLine($"{{");
        builder.AppendLine($"    public static readonly IReadOnlyDictionary<int, ItemDefinition> ItemDefinitions = new Dictionary<int, ItemDefinition>()");
        builder.AppendLine($"    {{");

        foreach (var (key, item) in items.OrderBy(x => int.Parse(x.Key)))
        {
            builder.AppendLine($"        {{ {key}, new(\"{item["name"]}\") }},");
        }

        builder.AppendLine($"    }};");
        builder.AppendLine($"");
        builder.AppendLine($"    public static readonly IReadOnlyDictionary<int, PaintKit> PaintKits = new Dictionary<int, PaintKit>()");
        builder.AppendLine($"    {{");

        foreach (var (key, paintKit) in paintKits.OrderBy(x => int.Parse(x.Key)))
        {
            builder.AppendLine($"        {{ {key}, new(\"{paintKit["name"]}\") }},");
        }

        builder.AppendLine($"    }};");
        builder.AppendLine($"");
        builder.AppendLine($"    public static readonly IReadOnlyDictionary<int, Attribute> Attributes = new Dictionary<int, Attribute>()");
        builder.AppendLine($"    {{");

        foreach (var (key, attribute) in attributes.OrderBy(x => int.Parse(x.Key)))
        {
            builder.AppendLine($"        {{ {key}, new(\"{attribute["name"]}\") }},");
        }

        builder.AppendLine($"    }};");
        builder.AppendLine($"}}");

        File.WriteAllText(Path.Combine(outputPath, "GameItems.AutoGen.cs"), builder.ToString());
    }
}
