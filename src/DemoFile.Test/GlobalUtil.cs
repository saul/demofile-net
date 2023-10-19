using System.Collections;

namespace DemoFile.Test;

public static class GlobalUtil
{
    public static readonly string DemoBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos");

    private static byte[] SpaceVsForwardM1 { get; } = File.ReadAllBytes(Path.Combine(DemoBase, "space-vs-forward-m1-ancient.dem"));
    public static MemoryStream SpaceVsForwardM1Stream => new(SpaceVsForwardM1);

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
