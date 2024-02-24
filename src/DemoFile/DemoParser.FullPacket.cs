using System.Diagnostics;

namespace DemoFile;

public partial class DemoParser
{
    private readonly record struct FullPacketPosition(DemoTick Tick, long StreamPosition)
        : IComparable<FullPacketPosition>
    {
        public int CompareTo(FullPacketPosition other) => Tick.CompareTo(other.Tick);
    }

    /// Key ticks occur every 60 seconds
    private const int FullPacketInterval = 64 * 60;

    // todo: reset on StartReadingAsync
    private readonly List<FullPacketPosition> _fullPacketPositions = new(64);
    private DemoTick _readFullPacketTick = DemoTick.PreRecord;
    private int _fullPacketTickOffset;

    private bool TryFindFullPacketBefore(DemoTick demoTick, out FullPacketPosition fullPacket)
    {
        var idx = _fullPacketPositions.BinarySearch(new FullPacketPosition(demoTick, 0L));
        if (idx >= 0)
        {
            fullPacket = _fullPacketPositions[idx];
            return true;
        }

        var fullPacketBeforeIdx = ~idx - 1;
        if (fullPacketBeforeIdx >= 0 && fullPacketBeforeIdx < _fullPacketPositions.Count)
        {
            fullPacket = _fullPacketPositions[fullPacketBeforeIdx];
            return true;
        }

        fullPacket = default;
        return false;
    }

    public async ValueTask SeekToTickAsync(DemoTick tick, CancellationToken cancellationToken)
    {
        // todo: throw if currently in a MoveNextAsync

        if (TickCount < DemoTick.Zero)
        {
            throw new InvalidOperationException($"Cannot seek to tick {tick}");
        }

        if (TickCount != default && tick > TickCount)
        {
            throw new InvalidOperationException($"Cannot seek to tick {tick}. The demo only contains data for {TickCount} ticks");
        }

        var hasFullPacket = TryFindFullPacketBefore(tick, out var fullPacket);
        if (tick < CurrentDemoTick)
        {
            if (!hasFullPacket)
            {
                throw new InvalidOperationException($"Cannot seek backwards to tick {tick}. No {nameof(CDemoFullPacket)} has been read");
            }

            // Seeking backwards. Jump back to the key tick to read the snapshot
            (CurrentDemoTick, _stream.Position) = fullPacket;
        }
        else
        {
            var deltaTicks = fullPacket.Tick - CurrentDemoTick;

            // Only read the key tick if the jump is far enough ahead
            if (hasFullPacket && deltaTicks.Value >= FullPacketInterval)
            {
                (CurrentDemoTick, _stream.Position) = fullPacket;
            }
        }

        // Keep reading commands until we reach the key tick
        _readFullPacketTick = new DemoTick(tick.Value / FullPacketInterval * FullPacketInterval + _fullPacketTickOffset);
        SkipToTick(_readFullPacketTick);

        // Advance ticks until we get to the target tick
        while (CurrentDemoTick < tick)
        {
            var cmd = ReadCommandHeader();

            // We've arrived at the target tick
            if (CurrentDemoTick == tick)
            {
                _stream.Position = _commandStartPosition;
                break;
            }

            if (!await MoveNextCoreAsync(cmd.Command, cmd.Size, cancellationToken).ConfigureAwait(false))
            {
                throw new EndOfStreamException($"Reached EOF at tick {CurrentDemoTick} while seeking to tick {tick}");
            }
        }
    }

    private void SkipToTick(DemoTick targetTick)
    {
        while (CurrentDemoTick < targetTick)
        {
            var cmd = ReadCommandHeader();
            var actualCmd = cmd.Command & ~(uint) EDemoCommands.DemIsCompressed;

            // If we're at the target tick, jump back to the start of the command
            if (CurrentDemoTick == targetTick && actualCmd == (uint)EDemoCommands.DemFullPacket)
            {
                _stream.Position = _commandStartPosition;
                break;
            }

            // Record fullpackets even when seeking to improve seeking performance
            if (actualCmd == (uint) EDemoCommands.DemFullPacket)
            {
                RecordFullPacket();
            }

            // Skip over the data and start reading the next command
            _stream.Position += cmd.Size;
        }
    }

    private void RecordFullPacket()
    {
        // DemoFullPackets are recorded in demos every 3,840 ticks (60 secs).
        // Keep track of where they are to allow for fast seeking through the demo.
        var idx = _fullPacketPositions.BinarySearch(new FullPacketPosition(CurrentDemoTick, 0L));
        if (idx < 0)
        {
            _fullPacketPositions.Insert(~idx, new FullPacketPosition(CurrentDemoTick, _commandStartPosition));
        }

        // Some demos have fullpackets at tick 0, some at tick 1.
        _fullPacketTickOffset = CurrentDemoTick.Value % FullPacketInterval;
        Debug.Assert(_fullPacketTickOffset == 0 || _fullPacketTickOffset == 1, "Unexpected key tick offset");
    }

    private void OnDemoFullPacket(CDemoFullPacket fullPacket)
    {
        if (CurrentDemoTick < DemoTick.Zero)
            return;

        RecordFullPacket();

        // We only care about full packets if we're seeking
        if (CurrentDemoTick == _readFullPacketTick)
        {
            var stringTables = fullPacket.StringTable;
            for (var tableIdx = 0; tableIdx < stringTables.Tables.Count; tableIdx++)
            {
                var snapshot = stringTables.Tables[tableIdx];
                var stringTable = _stringTables[snapshot.TableName];
                stringTable.ReplaceWith(snapshot.Items);
            }

            OnDemoPacket(fullPacket.Packet);
        }
    }
}