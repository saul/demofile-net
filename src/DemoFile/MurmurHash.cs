using System.Buffers.Binary;

namespace DemoFile;

public class MurmurHash
{
    public const uint StringtokenSeed = 0x31415926;
    public const uint ResourceIdSeed = 0xEDABCDEF;

    public static ulong MurmurHash64(ReadOnlySpan<byte> key, uint seed)
    {
        // 'm' and 'r' are mixing constants generated offline.
        // They're not really 'magic', they just happen to work well.
        const uint m = 0x5bd1e995;
        const int r = 24;

        // Initialize the hash to a 'random' value
        var h1 = seed ^ (uint)key.Length;
        var h2 = 0u;

        var len = key.Length;
        var pos = 0;

        // Mix 4 bytes at a time into the hash
        while (len >= 8)
        {
            uint k1 = BinaryPrimitives.ReadUInt32LittleEndian(key[pos..]);
            k1 *= m;
            k1 ^= k1 >> r;
            k1 *= m;
            h1 *= m;
            h1 ^= k1;
            pos += 4;
            len -= 4;

            uint k2 = BinaryPrimitives.ReadUInt32LittleEndian(key[pos..]);
            k2 *= m;
            k2 ^= k2 >> r;
            k2 *= m;
            h2 *= m;
            h2 ^= k2;
            pos += 4;
            len -= 4;
        }

        if (len >= 4)
        {
            uint k1 = BinaryPrimitives.ReadUInt32LittleEndian(key[pos..]);
            k1 *= m;
            k1 ^= k1 >> r;
            k1 *= m;
            h1 *= m;
            h1 ^= k1;
            pos += 4;
            len -= 4;
        }

        // Handle the last few bytes of the input array
        switch (len)
        {
            case 3:
                h2 ^= (uint)(key[pos + 2] << 16);
                goto case 2;
            case 2:
                h2 ^= (uint)(key[pos + 1] << 8);
                goto case 1;
            case 1:
                h2 ^= key[pos];
                h2 *= m;
                break;
        }

        h1 ^= h2 >> 18; h1 *= m;
        h2 ^= h1 >> 22; h2 *= m;
        h1 ^= h2 >> 17; h1 *= m;
        h2 ^= h1 >> 19; h2 *= m;

        ulong h = h1;
        h = (h << 32) | h2;
        return h;
    }
}