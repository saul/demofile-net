using System.Buffers;
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
        Console.WriteLine($">>> SeekToTick: {tick} (from {CurrentDemoTick})");

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
        Console.WriteLine($"  Key tick: {(hasFullPacket ? $"{fullPacket.Tick}@{fullPacket.StreamPosition}" : "none")}");

        if (tick < CurrentDemoTick)
        {
            if (!hasFullPacket)
            {
                throw new InvalidOperationException($"Cannot seek backwards to tick {tick}. No {nameof(CDemoFullPacket)} has been read");
            }

            // Seeking backwards. Jump back to the key tick to read the snapshot
            (CurrentDemoTick, _stream.Position) = fullPacket;
            Console.WriteLine($"  Seeking back from: {fullPacket.Tick}@{fullPacket.StreamPosition}");
        }
        else
        {
            var deltaTicks = fullPacket.Tick - CurrentDemoTick;

            // Only read the key tick if the jump is far enough ahead
            if (hasFullPacket && deltaTicks.Value >= FullPacketInterval)
            {
                (CurrentDemoTick, _stream.Position) = fullPacket;
                Console.WriteLine($"  Seeking forward from: {fullPacket.Tick}@{fullPacket.StreamPosition}");
            }
        }

        // Keep reading commands until we reach the key tick
        _readFullPacketTick = new DemoTick(tick.Value / FullPacketInterval * FullPacketInterval + _fullPacketTickOffset);
        Console.WriteLine($"  Closest full packet: {_readFullPacketTick}");
        SkipToTick(_readFullPacketTick);

        // Advance ticks until we get to the target tick
        Console.WriteLine($"  Advancing to: {tick}");
        while (CurrentDemoTick < tick)
        {
            var startPosition = _stream.Position;
            var cmd = ReadCommandHeader();

            // We've arrived at the target tick
            if (CurrentDemoTick == tick)
            {
                _stream.Position = startPosition;
                break;
            }

            if (!await MoveNextCoreAsync(cmd.Command, cmd.Size, cancellationToken).ConfigureAwait(false))
            {
                throw new EndOfStreamException($"Reached EOF at tick {CurrentDemoTick} while seeking to tick {tick}");
            }
        }

        Console.WriteLine($"  Arrived at: {CurrentDemoTick}/{tick}\n");
    }

    private void SkipToTick(DemoTick targetTick)
    {
        while (CurrentDemoTick < targetTick)
        {
            var startPosition = _stream.Position;
            var cmd = ReadCommandHeader();

            // If we're at the target tick, jump back to the start of the command.
            if (CurrentDemoTick == targetTick && cmd.Command == (uint)EDemoCommands.DemStringTables)
            {
                _stream.Position = startPosition;
                break;
            }

            Console.WriteLine($"  Skipping past {CurrentDemoTick}...");

            // Always read string tables commands when seeking, as they contain
            // key tick information that improves seeking performance.
            if (cmd.Command == (uint) EDemoCommands.DemStringTables)
            {
                var rentedBuffer = ArrayPool<byte>.Shared.Rent((int)cmd.Size);
                var buffer = rentedBuffer.AsSpan(0, (int)cmd.Size);
                _stream.ReadExactly(buffer);
                _demoEvents.DemoStringTables?.Invoke(CDemoStringTables.Parser.ParseFrom(buffer));
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
            else
            {
                // Skip over the data and start reading the next command
                _stream.Position += cmd.Size;
            }
        }
    }

    private void OnDemoFullPacket(CDemoFullPacket fullPacket)
    {
        if (CurrentDemoTick < DemoTick.Zero)
            return;

        // DemoStringTables and packet entity snapshots are recorded in demos
        // every 3,840 ticks (60 secs). Keep track of where they are to allow
        // for fast seeking through the demo.
        // DemoStringTables are always before the packet entities snapshot.
        var idx = _fullPacketPositions.BinarySearch(new FullPacketPosition(CurrentDemoTick, 0L));
        if (idx < 0)
        {
            _fullPacketPositions.Insert(~idx, new FullPacketPosition(CurrentDemoTick, _commandStartPosition));
        }

        // Some demos have fullpackets at tick 0, some at tick 1.
        _fullPacketTickOffset = CurrentDemoTick.Value % FullPacketInterval;
        Debug.Assert(_fullPacketTickOffset == 0 || _fullPacketTickOffset == 1, "Unexpected key tick offset");

        Console.WriteLine($"DemoFullPacket - {CurrentDemoTick}");

        // We only care about DemoStringTables if we're seeking to a key tick
        if (CurrentDemoTick != _readFullPacketTick)
        {
            Console.WriteLine("\tSkipping!");
            return;
        }

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