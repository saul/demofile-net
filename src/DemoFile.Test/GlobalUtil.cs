using System.Collections;
using System.Text;

namespace DemoFile.Test;

public static class GlobalUtil
{
    private static readonly string DemoBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos");

    public static byte[] GotvCompetitiveProtocol13963 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "navi-javelins-vs-9-pandas-fearless-m1-mirage.dem"));

    public static byte[] GotvCompetitiveProtocol13992 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "virtus-pro-vs-natus-vincere-m1-ancient.dem"));

    public static byte[] GotvCompetitiveProtocol14008 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "mouz-nxt-vs-space-m1-vertigo.dem"));

    public static byte[] MatchmakingProtocol13968 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "93n781.dem"));

    public static byte[] GotvProtocol13978 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "13978.dem"));

    public static byte[] GotvProtocol13980 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "13980.dem"));

    public static byte[] GotvProtocol13987 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "13987.dem"));

    public static byte[] GotvProtocol13990ArmsRace { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "13990_armsrace.dem"));

    public static byte[] GotvProtocol13990Deathmatch { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "13990_dm.dem"));

    public static byte[] GotvProtocol14005 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "14005.dem"));

    public static byte[] GotvProtocol14011 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "14011.dem"));

    public static byte[] Pov14000 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "pov.dem"));

    public static byte[] GotvProtocol14034 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "14034.dem"));

    public static byte[] GotvProtocol14065 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "14065.dem"));

    public static byte[] ToBitStream(string input)
    {
        var bitArray = new BitArray(input.Length);
        for (var i = 0; i < input.Length; i++)
        {
            bitArray.Set(i, input[i] switch
            {
                '0' => false,
                '1' => true,
                _ => throw new ArgumentOutOfRangeException()
            });
        }

        var size = (input.Length + 7) / 8;
        var bytes = new byte[size];
        bitArray.CopyTo(bytes, 0);
        return bytes;
    }

    public static ParseMode[] ParseModes => Enum.GetValues<ParseMode>();

    public static async Task<string> Parse(ParseMode mode, byte[] demoFileBytes, Func<CsDemoParser, DemoSnapshot> parseSection)
    {
        if (mode == ParseMode.ReadAll)
        {
            var demo = new CsDemoParser();
            var stream = new MemoryStream(demoFileBytes);

            var result = parseSection(demo);

            var reader = DemoFileReader.Create(demo, stream);
            await reader.ReadAllAsync(default);
            return result.ToString();
        }
        else if (mode == ParseMode.ByTick)
        {
            var demo = new CsDemoParser();
            var stream = new MemoryStream(demoFileBytes);

            var result = parseSection(demo);

            var reader = DemoFileReader.Create(demo, stream);
            await reader.StartReadingAsync(default);
            while (await reader.MoveNextAsync(default))
            {
            }

            return result.ToString();
        }
        else if (mode == ParseMode.ReadAllParallel)
        {
            var results = await DemoFileReader<CsDemoParser>.ReadAllParallelAsync(demoFileBytes, parseSection, default);

            var acc = results.Aggregate(new DemoSnapshot(), (acc, snapshot) =>
            {
                acc.MergeFrom(snapshot);
                return acc;
            });
            return acc.ToString();
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}
