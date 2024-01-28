using System.Buffers;
using System.Diagnostics;
using Google.Protobuf.Collections;
using Snappier;

namespace DemoFile;

public class StringTable
{
    public delegate void UpdateCallback(int index, KeyValuePair<string, byte[]> userData);

    private readonly int _flags;
    private readonly int _userDataSizeBits;
    private readonly bool _isBitcountVarint;
    private readonly bool _isUserDataFixedSize;
    private readonly UpdateCallback? _onUpdatedEntry;
    private List<KeyValuePair<string, byte[]>> _entries = new();

    public string Name { get; }

    public IReadOnlyList<KeyValuePair<string, byte[]>> Entries => _entries;

    public StringTable(string name, int flags, int userDataSizeBits, bool isBitcountVarint, bool isUserDataFixedSize,
        UpdateCallback? onUpdatedEntry)
    {
        _flags = flags;
        _userDataSizeBits = userDataSizeBits;
        _isBitcountVarint = isBitcountVarint;
        _isUserDataFixedSize = isUserDataFixedSize;
        _onUpdatedEntry = onUpdatedEntry;

        Name = name;
    }

    public override string ToString() => $"StringTable {{ {Name}, Entries = {_entries.Count} }}";

    internal void ReadUpdate(ReadOnlySpan<byte> stringData, int entries)
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

            index += 1;
            if (!bitBuffer.ReadOneBit())
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
            else
            {
                // If key is missing, this is an update to an existing entry
                key = _entries[index].Key;
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

            _onUpdatedEntry?.Invoke(index, entry);
        }

        ArrayPool<string>.Shared.Return(keys);
    }

    internal void ReplaceWith(IReadOnlyList<CDemoStringTables.Types.items_t> items)
    {
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];

            // Add any new entries
            if (index >= _entries.Count)
            {
                var entry = KeyValuePair.Create(item.Str, item.Data.ToArray());
                _entries.Add(entry);
                _onUpdatedEntry?.Invoke(index, entry);
                continue;
            }

            var existing = _entries[index];
            Debug.Assert(existing.Key == item.Str, "String table entry changed on snapshot");

            // If the user data has changed, invoke the change callback
            if (!existing.Value.SequenceEqual(item.Data))
            {
                var entry = KeyValuePair.Create(item.Str, item.Data.ToArray());
                _entries[index] = entry;
                _onUpdatedEntry?.Invoke(index, entry);
            }
        }

        if (items.Count < _entries.Count)
        {
            _entries.RemoveRange(items.Count, _entries.Count - items.Count);
        }
    }
}
