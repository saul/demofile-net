using System.Collections;

namespace DemoFile.Test;

public static class GlobalUtil
{
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

        var size = (int)Math.Ceiling(input.Length / 8.0);
        var bytes = new byte[size];
        bitArray.CopyTo(bytes, 0);
        return bytes;
    }
}
