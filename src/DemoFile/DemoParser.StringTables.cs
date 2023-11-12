using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Snappier;

namespace DemoFile;

public partial class DemoParser
{
    private readonly Dictionary<string, StringTable> _stringTables = new();
    private readonly List<StringTable> _stringTableList = new();

    // todo: make this an array for perf?
    private readonly Dictionary<uint, byte[]> _instanceBaselines = new();

    private readonly CMsgPlayerInfo?[] _playerInfos = Array.Empty<CMsgPlayerInfo?>();

    public bool TryGetStringTable(string tableName, [NotNullWhen(true)] out StringTable? stringTable) =>
        _stringTables.TryGetValue(tableName, out stringTable);

    private int _instanceBaselineTableId = -1;
    private int _userInfoTableId = -1;

    private void OnCreateStringTable(CSVCMsg_CreateStringTable msg)
    {
        var stringTable = new StringTable(
            msg.Name,
            msg.Flags,
            msg.UserDataSizeBits,
            msg.UsingVarintBitcounts,
            msg.UserDataFixedSize);

        Action<KeyValuePair<string, byte[]>>? onUpdatedEntry = null;
        if (msg.Name == "instancebaseline")
        {
            _instanceBaselineTableId = _stringTableList.Count;
            onUpdatedEntry = OnInstanceBaselineUpdate;
        }
        else if (msg.Name == "userinfo")
        {
            _userInfoTableId = _stringTableList.Count;
            onUpdatedEntry = OnUserInfoUpdate;
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

        Action<KeyValuePair<string, byte[]>>? onUpdatedEntry =
            msg.TableId == _instanceBaselineTableId ? OnInstanceBaselineUpdate :
            msg.TableId == _userInfoTableId ? OnUserInfoUpdate : null;

        stringTable.ReadUpdate(msg.StringData.Span, msg.NumChangedEntries, onUpdatedEntry);
    }

    private void OnInstanceBaselineUpdate(KeyValuePair<string, byte[]> entry)
    {
        if (uint.TryParse(entry.Key, out var classId))
        {
            _instanceBaselines[classId] = entry.Value;
        }
    }

    private void OnUserInfoUpdate(KeyValuePair<string, byte[]> entry)
    {
        var slot = int.Parse(entry.Key);
        _playerInfos[slot] = entry.Value.Length == 0
            ? null
            : CMsgPlayerInfo.Parser.ParseFrom(entry.Value);
    }
}
