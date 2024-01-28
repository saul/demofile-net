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

public partial class DemoParser
{
    private readonly Dictionary<string, StringTable> _stringTables = new();
    private readonly List<StringTable> _stringTableList = new();

    private readonly Dictionary<BaselineKey, int> _instanceBaselineLookup = new();
    private KeyValuePair<BaselineKey, byte[]>[] _instanceBaselines = new KeyValuePair<BaselineKey, byte[]>[64];
    private CMsgPlayerInfo?[] _playerInfos = new CMsgPlayerInfo?[16];

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

    private void OnInstanceBaselineUpdate(int index, KeyValuePair<string, byte[]> entry)
    {
        if (index >= _instanceBaselines.Length)
        {
            var newSize = (int) BitOperations.RoundUpToPowerOf2((uint) index + 1);
            Array.Resize(ref _instanceBaselines, newSize);
        }

        ReadOnlySpan<char> key = entry.Key;

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

        _instanceBaselines[index] = KeyValuePair.Create(baselineKey, entry.Value);
        _instanceBaselineLookup[baselineKey] = index;
    }

    private void OnUserInfoUpdate(int index, KeyValuePair<string, byte[]> entry)
    {
        if (index >= _playerInfos.Length)
        {
            var newSize = (int) BitOperations.RoundUpToPowerOf2((uint) index + 1);
            Array.Resize(ref _playerInfos, newSize);
        }

        _playerInfos[index] = entry.Value.Length == 0
            ? null
            : CMsgPlayerInfo.Parser.ParseFrom(entry.Value);
    }

    private void OnDemoStringTables(CDemoStringTables stringTables)
    {
        // DemoStringTables and packet entity snapshots are recorded in demos
        // every 3,840 ticks (60 secs). Keep track of where they are to allow
        // for fast seeking through the demo.
        // DemoStringTables are always before the packet entities snapshot.
        _keyTickPositions.TryAdd(CurrentDemoTick, _commandStartPosition);

        // Some demos have key ticks at tick 0, some at tick 1.
        (_, _keyTickOffset) = Math.DivRem(CurrentDemoTick.Value, KeyTickInterval);

        // We only care about DemoStringTables if we're seeking to a key tick
        if (CurrentDemoTick != _readSnapshotTick) return;

        for (var tableIdx = 0; tableIdx < stringTables.Tables.Count; tableIdx++)
        {
            var snapshot = stringTables.Tables[tableIdx];
            var stringTable = _stringTableList[tableIdx];
            Debug.Assert(stringTable.Name == snapshot.TableName);

            stringTable.ReplaceWith(snapshot.Items);
        }
    }
}
