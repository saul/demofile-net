using System.Buffers.Binary;

namespace DemoFile;

/// <summary>
/// The ICE ("Information Concealment Engine") block cipher by Matthew Kwan, as used by the Source engine to
/// encrypt <c>svc_EncryptedData</c> messages. ICE has a 64-bit block; level <c>n</c> uses an <c>8 * n</c> byte key.
/// </summary>
internal sealed class IceKey
{
    public const int BlockSize = 8;

    private static readonly int[,] SMod =
    {
        { 333, 313, 505, 369 },
        { 379, 375, 319, 391 },
        { 361, 445, 451, 397 },
        { 397, 425, 395, 505 },
    };

    private static readonly int[,] SXor =
    {
        { 0x83, 0x85, 0x9b, 0xcd },
        { 0xcc, 0xa7, 0xad, 0x41 },
        { 0x4b, 0x2e, 0xd4, 0x33 },
        { 0xea, 0xcb, 0x2e, 0x04 },
    };

    private static readonly uint[] PBox =
    {
        0x00000001, 0x00000080, 0x00000400, 0x00002000,
        0x00080000, 0x00200000, 0x01000000, 0x40000000,
        0x00000008, 0x00000020, 0x00000100, 0x00004000,
        0x00010000, 0x00800000, 0x04000000, 0x20000000,
        0x00000004, 0x00000010, 0x00000200, 0x00008000,
        0x00020000, 0x00400000, 0x08000000, 0x10000000,
        0x00000002, 0x00000040, 0x00000800, 0x00001000,
        0x00040000, 0x00100000, 0x02000000, 0x80000000,
    };

    private static readonly int[] KeyRot = { 0, 1, 2, 3, 2, 1, 3, 0, 1, 3, 2, 0, 3, 1, 0, 2 };

    // Must be declared after the tables it's built from: static fields are initialised in declaration order
    private static readonly uint[,] SBox = BuildSBoxes();

    private readonly int _size;
    private readonly int _rounds;
    private readonly uint[] _keySchedule; // _rounds * 3 subkeys

    public IceKey(int level)
    {
        if (level < 1)
        {
            _size = 1;
            _rounds = 8;
        }
        else
        {
            _size = level;
            _rounds = level * 16;
        }

        _keySchedule = new uint[_rounds * 3];
    }

    /// <summary>Key length in bytes.</summary>
    public int KeySize => _size * 8;

    public void Set(ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"ICE key must be {KeySize} bytes, got {key.Length}", nameof(key));

        Span<ushort> kb = stackalloc ushort[4];
        if (_rounds == 8)
        {
            for (var i = 0; i < 4; i++)
                kb[3 - i] = (ushort)((key[i * 2] << 8) | key[i * 2 + 1]);
            BuildSchedule(kb, 0, 0);
            return;
        }

        for (var i = 0; i < _size; i++)
        {
            for (var j = 0; j < 4; j++)
                kb[3 - j] = (ushort)((key[i * 8 + j * 2] << 8) | key[i * 8 + j * 2 + 1]);

            BuildSchedule(kb, i * 8, 0);
            BuildSchedule(kb, _rounds - 8 - i * 8, 8);
        }
    }

    public void Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
    {
        var l = BinaryPrimitives.ReadUInt32BigEndian(plaintext);
        var r = BinaryPrimitives.ReadUInt32BigEndian(plaintext[4..]);

        for (var i = 0; i < _rounds; i += 2)
        {
            l ^= F(r, i);
            r ^= F(l, i + 1);
        }

        BinaryPrimitives.WriteUInt32BigEndian(ciphertext, r);
        BinaryPrimitives.WriteUInt32BigEndian(ciphertext[4..], l);
    }

    public void Decrypt(ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
    {
        var l = BinaryPrimitives.ReadUInt32BigEndian(ciphertext);
        var r = BinaryPrimitives.ReadUInt32BigEndian(ciphertext[4..]);

        for (var i = _rounds - 1; i > 0; i -= 2)
        {
            l ^= F(r, i);
            r ^= F(l, i - 1);
        }

        BinaryPrimitives.WriteUInt32BigEndian(plaintext, r);
        BinaryPrimitives.WriteUInt32BigEndian(plaintext[4..], l);
    }

    /// <summary>Decrypt <paramref name="data"/> in place, one block at a time (ECB). Any trailing partial block is left as-is.</summary>
    public void DecryptInPlace(Span<byte> data)
    {
        Span<byte> block = stackalloc byte[BlockSize];
        for (var offset = 0; offset + BlockSize <= data.Length; offset += BlockSize)
        {
            var chunk = data.Slice(offset, BlockSize);
            Decrypt(chunk, block);
            block.CopyTo(chunk);
        }
    }

    private void BuildSchedule(Span<ushort> kb, int n, int keyRotOffset)
    {
        for (var i = 0; i < 8; i++)
        {
            var kr = KeyRot[keyRotOffset + i];
            var isk = (n + i) * 3;
            _keySchedule[isk] = 0;
            _keySchedule[isk + 1] = 0;
            _keySchedule[isk + 2] = 0;

            for (var j = 0; j < 15; j++)
            {
                ref var currSk = ref _keySchedule[isk + j % 3];
                for (var k = 0; k < 4; k++)
                {
                    ref var currKb = ref kb[(kr + k) & 3];
                    var bit = (uint)(currKb & 1);
                    currSk = (currSk << 1) | bit;
                    currKb = (ushort)(((uint)currKb >> 1) | ((bit ^ 1) << 15));
                }
            }
        }
    }

    private uint F(uint p, int round)
    {
        var sk = round * 3;

        // Left and right expansions
        var tl = ((p >> 16) & 0x3ff) | (((p >> 14) | (p << 18)) & 0xffc00);
        var tr = (p & 0x3ff) | ((p << 2) & 0xffc00);

        // Perform the salt permutation
        var al = _keySchedule[sk + 2] & (tl ^ tr);
        var ar = al ^ tr;
        al ^= tl;

        // XOR with the subkey
        al ^= _keySchedule[sk];
        ar ^= _keySchedule[sk + 1];

        // S-box lookup and permutation
        return SBox[0, al >> 10] | SBox[1, al & 0x3ff] | SBox[2, ar >> 10] | SBox[3, ar & 0x3ff];
    }

    private static uint[,] BuildSBoxes()
    {
        var sbox = new uint[4, 1024];
        for (var i = 0; i < 1024; i++)
        {
            var col = (i >> 1) & 0xff;
            var row = (i & 0x1) | ((i & 0x200) >> 8);

            sbox[0, i] = Perm32(GfExp7(col ^ SXor[0, row], SMod[0, row]) << 24);
            sbox[1, i] = Perm32(GfExp7(col ^ SXor[1, row], SMod[1, row]) << 16);
            sbox[2, i] = Perm32(GfExp7(col ^ SXor[2, row], SMod[2, row]) << 8);
            sbox[3, i] = Perm32(GfExp7(col ^ SXor[3, row], SMod[3, row]));
        }

        return sbox;
    }

    // Galois field multiplication of a by b, modulo m
    private static uint GfMult(uint a, uint b, uint m)
    {
        uint res = 0;
        while (b != 0)
        {
            if ((b & 1) != 0)
                res ^= a;

            a <<= 1;
            b >>= 1;

            if (a >= 256)
                a ^= m;
        }

        return res;
    }

    // Galois field exponentiation: b ^ 7 modulo m
    private static uint GfExp7(int b, int m)
    {
        if (b == 0)
            return 0;

        var x = GfMult((uint)b, (uint)b, (uint)m);
        x = GfMult((uint)b, x, (uint)m);
        x = GfMult(x, x, (uint)m);
        return GfMult((uint)b, x, (uint)m);
    }

    private static uint Perm32(uint x)
    {
        uint res = 0;
        var i = 0;
        while (x != 0)
        {
            if ((x & 1) != 0)
                res |= PBox[i];
            i++;
            x >>= 1;
        }

        return res;
    }
}
