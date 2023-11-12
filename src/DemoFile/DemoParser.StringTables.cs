using System.Diagnostics.CodeAnalysis;
using Snappier;

namespace DemoFile;

public readonly record struct BaselineKey(uint ServerClassId, uint AlternateBaseline)
{
    public override string ToString() => AlternateBaseline == 0
        ? ServerClassId.ToString()
        : $"{ServerClassId}:{AlternateBaseline}";
}

public partial class DemoParser
{
    private readonly Dictionary<string, StringTable> _stringTables = new();
    private readonly List<StringTable> _stringTableList = new();

    // todo: split into baseline/alternate baselines, store plain baseline in array?
    private readonly Dictionary<BaselineKey, byte[]> _instanceBaselines = new();
    private readonly List<KeyValuePair<string, byte[]>> _instanceBaselineList = new();

    public bool TryGetStringTable(string tableName, [NotNullWhen(true)] out StringTable? stringTable) =>
        _stringTables.TryGetValue(tableName, out stringTable);

    private int _instanceBaselineTableId = -1;

    private void OnCreateStringTable(CSVCMsg_CreateStringTable msg)
    {
        var stringTable = new StringTable(
            msg.Name,
            (StringTableFlags) msg.Flags,
            msg.UserDataSizeBits,
            msg.UsingVarintBitcounts,
            msg.UserDataFixedSize);

        Action<KeyValuePair<string, byte[]>>? onUpdatedEntry = null;
        if (msg.Name == "instancebaseline")
        {
            _instanceBaselineTableId = _stringTableList.Count;
            onUpdatedEntry = OnInstanceBaselineUpdate;
        }

        if (msg.DataCompressed)
        {
            using var decompressed = Snappy.DecompressToMemory(msg.StringData.Span);
            stringTable.ReadUpdate(decompressed.Memory.Span, msg.NumEntries, onUpdatedEntry);
        }
        else
        {
            stringTable.ReadUpdate(msg.StringData.Span, msg.NumEntries, onUpdatedEntry);
        }

        _stringTableList.Add(stringTable);
        _stringTables.Add(msg.Name, stringTable);
    }

    private void OnUpdateStringTable(CSVCMsg_UpdateStringTable msg)
    {
        var stringTable = _stringTableList[msg.TableId];

        Action<KeyValuePair<string,byte[]>>? onUpdatedEntry =
            msg.TableId == _instanceBaselineTableId ? OnInstanceBaselineUpdate : null;

        stringTable.ReadUpdate(msg.StringData.Span, msg.NumChangedEntries, onUpdatedEntry);
    }

    private void OnInstanceBaselineUpdate(KeyValuePair<string, byte[]> entry)
    {
        ReadOnlySpan<char> key = entry.Key;

        if (key.IndexOf(':') is var index && index >= 0)
        {
            var classId = uint.Parse(key[..index]);
            var alternateBaseline = uint.Parse(key[(index + 1)..]);

            _instanceBaselines[new BaselineKey(classId, alternateBaseline)] = entry.Value;
        }
        else
        {
            var classId = uint.Parse(key);
            _instanceBaselines[new BaselineKey(classId, 0)] = entry.Value;
        }
    }

    private void OnDemoStringTables(CDemoStringTables msg)
    {
        foreach (var table in msg.Tables)
        {
            // todo: handle other tables
            if (table.TableName != "instancebaseline")
                continue;

            var flags = (StringTableFlags) table.TableFlags;
            foreach (var item in table.Items)
            {
                OnInstanceBaselineUpdate(KeyValuePair.Create(item.Str, item.Data.ToByteArray()));
            }
        }
    }
}
