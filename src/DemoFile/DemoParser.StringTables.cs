using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Snappier;

namespace DemoFile;

internal readonly record struct BaselineKey(uint ServerClassId, uint AlternateBaseline)
{
    public override string ToString() => AlternateBaseline == 0
        ? ServerClassId.ToString()
        : $"{ServerClassId}:{AlternateBaseline}";
}

public partial class DemoParser<TGameParser>
{
    private readonly Dictionary<string, StringTable> _stringTables = new();
    private readonly List<StringTable> _stringTableList = new();

    private readonly Dictionary<BaselineKey, int> _instanceBaselineLookup = new();
    private KeyValuePair<BaselineKey, ReadOnlyMemory<byte>>[] _instanceBaselines = new KeyValuePair<BaselineKey, ReadOnlyMemory<byte>>[64];
    private CMsgPlayerInfo?[] _playerInfos = Array.Empty<CMsgPlayerInfo?>();

    public bool TryGetStringTable(string tableName, [NotNullWhen(true)] out StringTable? stringTable) =>
        _stringTables.TryGetValue(tableName, out stringTable);

    private void OnCreateStringTable(CSVCMsg_CreateStringTable msg)
    {
        StringTable.UpdateCallback? onUpdatedEntry = msg.Name switch
        {
            "instancebaseline" => OnInstanceBaselineUpdate,
            "userinfo" => OnUserInfoUpdate,
            _ => null
        };

        if (msg.Name == "userinfo")
        {
            _playerInfos = new CMsgPlayerInfo?[msg.NumEntries];
        }

        var stringTable = new StringTable(
            msg.Name,
            msg.Flags,
            msg.UserDataSizeBits,
            msg.UsingVarintBitcounts,
            msg.UserDataFixedSize,
            onUpdatedEntry);

        if (msg.DataCompressed)
        {
            using var decompressed = Snappy.DecompressToMemory(msg.StringData.Span);
            stringTable.ReadUpdate(decompressed.Memory.Span, msg.NumEntries);
        }
        else
        {
            stringTable.ReadUpdate(msg.StringData.Span, msg.NumEntries);
        }

        _stringTableList.Add(stringTable);
        _stringTables.Add(msg.Name, stringTable);
    }

    private void OnUpdateStringTable(CSVCMsg_UpdateStringTable msg)
    {
        var stringTable = _stringTableList[msg.TableId];
        stringTable.ReadUpdate(msg.StringData.Span, msg.NumChangedEntries);
    }

    private void RestoreStringTables(ImmutableDictionary<string, IReadOnlyList<KeyValuePair<string, ReadOnlyMemory<byte>>>> snapshot)
    {
        foreach (var stringTable in _stringTableList)
        {
            stringTable.ReplaceWith(snapshot[stringTable.Name]);
        }
    }

    private void OnDemoStringTables(CDemoStringTables stringTables)
    {
        foreach (var table in stringTables.Tables)
        {
            OnDemoStringTable(table);
        }
    }

    private void OnDemoStringTable(CDemoStringTables.Types.table_t snapshot)
    {
        var stringTable = _stringTables[snapshot.TableName];

        var newEntries = snapshot.Items
            .Select(item => KeyValuePair.Create(item.Str, item.Data.Memory))
            .ToImmutableList();

        stringTable.ReplaceWith(newEntries);
    }

    private void OnInstanceBaselineUpdate(int index, KeyValuePair<string, ReadOnlyMemory<byte>>? entry)
    {
        if (index >= _instanceBaselines.Length)
        {
            var newSize = (int) BitOperations.RoundUpToPowerOf2((uint) index + 1);
            Array.Resize(ref _instanceBaselines, newSize);
        }

        if (entry is {Key: var stringData, Value: var userData})
        {
            ReadOnlySpan<char> key = stringData;

            BaselineKey baselineKey;
            if (key.IndexOf(':') is var sepIdx && sepIdx >= 0)
            {
                var classId = uint.Parse(key[..sepIdx]);
                var alternateBaseline = uint.Parse(key[(sepIdx + 1)..]);

                baselineKey = new BaselineKey(classId, alternateBaseline);
            }
            else
            {
                var classId = uint.Parse(key);
                baselineKey = new BaselineKey(classId, 0);
            }

            _instanceBaselines[index] = KeyValuePair.Create(baselineKey, userData);
            _instanceBaselineLookup[baselineKey] = index;
        }
        else
        {
            var (removedBaseline, _) = _instanceBaselines[index];
            var removed = _instanceBaselineLookup.Remove(removedBaseline);
            Debug.Assert(removed, "Expected to remove instancebaseline");
        }
    }

    private void OnUserInfoUpdate(int index, KeyValuePair<string, ReadOnlyMemory<byte>>? entry)
    {
        _playerInfos[index] = entry?.Value is {Length: >0} userInfo
            ? CMsgPlayerInfo.Parser.ParseFrom(userInfo.Span)
            : null;
    }
}
