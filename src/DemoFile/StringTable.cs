using System.Buffers;
using Snappier;

namespace DemoFile;

public class StringTable
{
    private readonly int _flags;
    private readonly int _userDataSizeBits;
    private readonly bool _isBitcountVarint;
    private readonly bool _isUserDataFixedSize;
    private List<KeyValuePair<string, byte[]>> _entries = new();

    public string Name { get; }

    public IReadOnlyList<KeyValuePair<string, byte[]>> Entries => _entries;

    public StringTable(string name, int flags, int userDataSizeBits, bool isBitcountVarint, bool isUserDataFixedSize)
    {
        _flags = flags;
        _userDataSizeBits = userDataSizeBits;
        _isBitcountVarint = isBitcountVarint;
        _isUserDataFixedSize = isUserDataFixedSize;

        Name = name;
    }

    public override string ToString() => $"StringTable {{ {Name}, Entries = {_entries.Count} }}";

    internal void ReadUpdate(ReadOnlySpan<byte> stringData, int entries, Action<int, KeyValuePair<string, byte[]>>? onUpdatedEntry)
    {
        if (_entries.Count == 0)
            _entries.EnsureCapacity(entries);

        var bitBuffer = new BitBuffer(stringData);

        var keys = ArrayPool<string>.Shared.Rent(entries);

        var index = -1;
        for (var i = 0; i < entries; ++i)
        {
            var key = "";
            var value = Array.Empty<byte>();

            if (bitBuffer.ReadOneBit())
            {
                index += 1;
            }
            else
            {
                index += (int)bitBuffer.ReadUVarInt32() + 1;
            }

            // Does this entry have a key?
            if (bitBuffer.ReadOneBit())
            {
                // Should we refer back to history?
                if (bitBuffer.ReadOneBit())
                {
                    var position = bitBuffer.ReadUBits(5);
                    var length = bitBuffer.ReadUBits(5);

                    // position is in the range [0..32), where:
                    //   32 = the most recent entry
                    //   0  = 32 entries ago
                    var historicalKey = keys[i < 32 ? position : i - (32 - position)];
                    if (length > historicalKey.Length)
                    {
                        key = historicalKey + bitBuffer.ReadStringUtf8();
                    }
                    else
                    {
                        key = historicalKey[..(int)length] + bitBuffer.ReadStringUtf8();
                    }
                }
                else
                {
                    key = bitBuffer.ReadStringUtf8();
                }
            }

            // Keep track of the key
            keys[i] = key;

            // Does this entry have a value?
            if (bitBuffer.ReadOneBit())
            {
                var bits = _userDataSizeBits;
                var isCompressed = false;

                if (!_isUserDataFixedSize)
                {
                    if ((_flags & 0x1) != 0)
                    {
                        isCompressed = bitBuffer.ReadOneBit();
                    }

                    bits = (int)(_isBitcountVarint
                        ? bitBuffer.ReadUBitVar() * 8
                        : bitBuffer.ReadUBits(17) * 8);
                }

                value = new byte[(bits + 7) / 8];
                bitBuffer.ReadBitsAsBytes(value, bits);

                if (isCompressed)
                {
                    value = Snappy.DecompressToArray(value);
                }
            }

            var entry = new KeyValuePair<string, byte[]>(key, value);

            if (index == _entries.Count)
                _entries.Add(entry);
            else
                _entries[index] = entry;

            onUpdatedEntry?.Invoke(index, entry);
        }

        ArrayPool<string>.Shared.Return(keys);
    }
}
