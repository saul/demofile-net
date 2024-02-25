using System.Diagnostics;

namespace DemoFile;

public partial class DemoParser
{
    private readonly record struct FullPacketPosition(DemoTick Tick, long StreamPosition)
        : IComparable<FullPacketPosition>
    {
        public int CompareTo(FullPacketPosition other) => Tick.CompareTo(other.Tick);
    }

    /// Full packets occur every 60 seconds
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

    /// <summary>
    /// Seek to a specific tick within the demo file. Tick can be in the future or the past.
    /// This works by first seeking to the nearest <see cref="CDemoFullPacket"/> before <paramref name="targetTick"/>,
    /// decoding the full stringtable and entity snapshot, then reading tick-by-tick to <paramref name="targetTick"/>.
    /// </summary>
    /// <param name="targetTick">Tick to seek to.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the seek.</param>
    /// <exception cref="InvalidOperationException">Tick is invalid, or attempting to seek while reading commands.</exception>
    /// <exception cref="EndOfStreamException">EOF before reaching <paramref name="targetTick"/>.</exception>
    /// <remarks>
    /// Seeking is not allowed while reading commands. See <see cref="IsReading"/>.
    /// </remarks>
    public async ValueTask SeekToTickAsync(DemoTick targetTick, CancellationToken cancellationToken)
    {
        if (IsReading)
            throw new InvalidOperationException($"Cannot seek to tick while reading commands");

        if (TickCount < DemoTick.Zero)
            throw new InvalidOperationException($"Cannot seek to tick {targetTick}");

        if (TickCount != default && targetTick > TickCount)
            throw new InvalidOperationException($"Cannot seek to tick {targetTick}. The demo only contains data for {TickCount} ticks");

        var hasFullPacket = TryFindFullPacketBefore(targetTick, out var fullPacket);
        if (targetTick < CurrentDemoTick)
        {
            if (!hasFullPacket)
                throw new InvalidOperationException($"Cannot seek backwards to tick {targetTick}. No {nameof(CDemoFullPacket)} has been read");

            // Seeking backwards. Jump back to the full packet to read the snapshot
            (CurrentDemoTick, _stream.Position) = fullPacket;
        }
        else
        {
            var deltaTicks = fullPacket.Tick - CurrentDemoTick;

            // Only read the full packet if the jump is far enough ahead
            if (hasFullPacket && deltaTicks.Value >= FullPacketInterval)
            {
                (CurrentDemoTick, _stream.Position) = fullPacket;
            }
        }

        // Keep reading commands until we reach the full packet
        _readFullPacketTick = new DemoTick(targetTick.Value / FullPacketInterval * FullPacketInterval + _fullPacketTickOffset);
        SkipToTick(_readFullPacketTick);

        // Advance ticks until we get to the target tick
        while (CurrentDemoTick < targetTick)
        {
            var cmd = ReadCommandHeader();

            if (CurrentDemoTick == targetTick)
            {
                _stream.Position = _commandStartPosition;
                break;
            }

            if (!await MoveNextCoreAsync(cmd.Command, cmd.IsCompressed, cmd.Size, cancellationToken).ConfigureAwait(false))
            {
                throw new EndOfStreamException($"Reached EOF at tick {CurrentDemoTick} while seeking to tick {targetTick}");
            }
        }
    }

    private void SkipToTick(DemoTick targetTick)
    {
        while (CurrentDemoTick < targetTick)
        {
            var cmd = ReadCommandHeader();

            // If we're at the target tick, jump back to the start of the command
            if (CurrentDemoTick == targetTick && cmd.Command == EDemoCommands.DemFullPacket)
            {
                _stream.Position = _commandStartPosition;
                break;
            }

            // Record fullpackets even when seeking to improve seeking performance
            if (cmd.Command == EDemoCommands.DemFullPacket)
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
        Debug.Assert(_fullPacketTickOffset == 0 || _fullPacketTickOffset == 1, "Unexpected full packet tick offset");
    }

    private void OnDemoFullPacket(CDemoFullPacket fullPacket)
    {
        RecordFullPacket();

        // We only want to read full packets if we're seeking to it
        if (CurrentDemoTick == _readFullPacketTick)
        {
            foreach (var snapshot in fullPacket.StringTable.Tables)
            {
                var stringTable = _stringTables[snapshot.TableName];
                stringTable.ReplaceWith(snapshot.Items);
            }

            OnDemoPacket(fullPacket.Packet);
        }
    }
}