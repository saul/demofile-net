using System.Collections;

namespace DemoFile.Test;

public static class GlobalUtil
{
    private static readonly string DemoBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos");

    public static MemoryStream GotvCompetitiveProtocol13963 => new(File.ReadAllBytes(Path.Combine(DemoBase, "navi-javelins-vs-9-pandas-fearless-m1-mirage.dem")));

    public static MemoryStream MatchmakingProtocol13968 => new(File.ReadAllBytes(Path.Combine(DemoBase, "93n781.dem")));

    public static MemoryStream GotvProtocol13978 => new(File.ReadAllBytes(Path.Combine(DemoBase, "13978.dem")));

    public static MemoryStream GotvProtocol13980 => new(File.ReadAllBytes(Path.Combine(DemoBase, "13980.dem")));

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
}
