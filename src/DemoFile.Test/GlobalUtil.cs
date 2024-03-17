using System.Collections;
using System.Text;

namespace DemoFile.Test;

public static class GlobalUtil
{
    private static readonly string DemoBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos");

    public static byte[] GotvCompetitiveProtocol13963 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "navi-javelins-vs-9-pandas-fearless-m1-mirage.dem"));

    public static byte[] GotvCompetitiveProtocol13992 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "virtus-pro-vs-natus-vincere-m1-ancient.dem"));

    public static MemoryStream MatchmakingProtocol13968 => new(File.ReadAllBytes(Path.Combine(DemoBase, "93n781.dem")));

    public static byte[] GotvProtocol13978 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "13978.dem"));

    public static byte[] GotvProtocol13980 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "13980.dem"));

    public static byte[] GotvProtocol13987 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "13987.dem"));

    public static byte[] GotvProtocol13990ArmsRace { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "13990_armsrace.dem"));

    public static byte[] GotvProtocol13990Deathmatch { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "13990_dm.dem"));

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

    public static async Task<string> Parse(ParseMode mode, byte[] demoFileBytes, Func<DemoParser, DemoSnapshot> parseSection)
    {
        if (mode == ParseMode.ReadAll)
        {
            var demo = new DemoParser();
            var stream = new MemoryStream(demoFileBytes);

            var result = parseSection(demo);

            await demo.ReadAllAsync(stream, default);
            return result.ToString();
        }
        else if (mode == ParseMode.ByTick)
        {
            var demo = new DemoParser();
            var stream = new MemoryStream(demoFileBytes);

            var result = parseSection(demo);

            await demo.StartReadingAsync(stream, default);
            while (await demo.MoveNextAsync(default))
            {
            }

            return result.ToString();
        }
        else if (mode == ParseMode.ReadAllParallel)
        {
            var results = await DemoParser.ReadAllParallelAsync(demoFileBytes, parseSection, default);

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
