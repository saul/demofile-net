using System.Collections.Immutable;

namespace DemoFile;

public partial class DemoParser
{
    /// <summary>
    /// <c>true</c> when the demo is seeking between ticks, for example
    /// through <see cref="SeekToTickAsync"/>.
    /// </summary>
    public bool IsSeeking { get; private set; }

    private readonly struct SeekScope : IDisposable
    {
        private readonly DemoParser _parser;

        public SeekScope(DemoParser parser)
        {
            _parser = parser;
            parser.IsSeeking = true;
        }

        public void Dispose()
        {
            _parser.IsSeeking = false;
        }
    }

    internal readonly record struct FullPacketRecord(
        DemoTick Tick,
        long StreamPosition,
        ImmutableDictionary<string, IReadOnlyList<KeyValuePair<string, ReadOnlyMemory<byte>>>> StringTables)
        : IComparable<FullPacketRecord>
    {
        public static FullPacketRecord ForTick(DemoTick tick) => new(tick, 0L, ImmutableDictionary<string, IReadOnlyList<KeyValuePair<string, ReadOnlyMemory<byte>>>>.Empty);

        public int CompareTo(FullPacketRecord other) => Tick.CompareTo(other.Tick);
    }

    /// Full packets occur every 60 seconds
    private const int FullPacketInterval = 64 * 60;

    private readonly List<FullPacketRecord> _fullPackets = new(64);
    private DemoTick _readFullPacketTick = DemoTick.PreRecord;
    private int _fullPacketTickOffset;

    internal IReadOnlyList<FullPacketRecord> FullPackets => _fullPackets;

    private bool TryFindFullPacketBefore(DemoTick demoTick, out FullPacketRecord fullPacket)
    {
        var idx = _fullPackets.BinarySearch(FullPacketRecord.ForTick(demoTick));
        if (idx >= 0)
        {
            fullPacket = _fullPackets[idx];
            return true;
        }

        var fullPacketBeforeIdx = ~idx - 1;
        if (fullPacketBeforeIdx >= 0 && fullPacketBeforeIdx < _fullPackets.Count)
        {
            fullPacket = _fullPackets[fullPacketBeforeIdx];
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
        using var _ = new SeekScope(this);

        if (IsReading)
            throw new InvalidOperationException($"Cannot seek to tick while reading commands");

        if (TickCount < DemoTick.Zero)
            throw new InvalidOperationException($"Cannot seek to tick {targetTick}");

        if (TickCount != default && targetTick > TickCount)
            throw new InvalidOperationException($"Cannot seek to tick {targetTick}. The demo only contains data for {TickCount} ticks");

        // Can never seek before the first tick of the demo
        targetTick = new DemoTick(Math.Max(targetTick.Value, _fullPacketTickOffset));

        var hasFullPacket = TryFindFullPacketBefore(targetTick, out var fullPacket);
        if (targetTick <= CurrentDemoTick)
        {
            if (!hasFullPacket)
                throw new InvalidOperationException($"Cannot seek backwards to tick {targetTick} from {CurrentDemoTick}. No {nameof(EDemoCommands.DemFullPacket)} has been read");

            // Seeking backwards. Jump back to the full packet to read the snapshot
            (CurrentDemoTick, _stream.Position, var stringTables) = fullPacket;
            RestoreStringTables(stringTables);
        }
        else
        {
            var deltaTicks = fullPacket.Tick - CurrentDemoTick;

            // Only read the full packet if the jump is far enough ahead
            if (hasFullPacket && deltaTicks.Value >= FullPacketInterval)
            {
                (CurrentDemoTick, _stream.Position, var stringTables) = fullPacket;
                RestoreStringTables(stringTables);
            }
        }

        // Keep reading commands until we reach the full packet
        _readFullPacketTick = new DemoTick((targetTick.Value - _fullPacketTickOffset) / FullPacketInterval * FullPacketInterval + _fullPacketTickOffset);
        if (CurrentDemoTick < _readFullPacketTick)
        {
            await SkipToFullPacketTickAsync(_readFullPacketTick, cancellationToken).ConfigureAwait(false);
        }

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

    private async ValueTask SkipToFullPacketTickAsync(DemoTick targetTick, CancellationToken cancellationToken)
    {
        while (CurrentDemoTick <= targetTick)
        {
            var cmd = ReadCommandHeader();

            // If we're at the target tick, jump back to the start of the command
            if (CurrentDemoTick == targetTick && cmd.Command == EDemoCommands.DemFullPacket)
            {
                _stream.Position = _commandStartPosition;
                return;
            }

            // Decode fullpackets even when seeking for two reasons:
            // - Full packets are recorded to enable faster seeking in future
            // - Contain string table delta since last full packet that must be decoded
            if (cmd.Command == EDemoCommands.DemFullPacket)
            {
                await MoveNextCoreAsync(cmd.Command, cmd.IsCompressed, cmd.Size, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Skip over the data and start reading the next command
                _stream.Position += cmd.Size;
            }
        }

        throw new InvalidDemoException($"Could not find {nameof(EDemoCommands.DemFullPacket)} at tick {targetTick}");
    }

    private void OnDemoFullPacket(CDemoFullPacket fullPacket)
    {
        // CDemoFullPacket.string_table only contains tables that have changed
        // since the last CDemoFullPacket, so we need to read each one while seeking.
        if (IsSeeking || CurrentDemoTick == _readFullPacketTick)
        {
            foreach (var snapshot in fullPacket.StringTable.Tables)
            {
                OnDemoStringTable(snapshot);
            }
        }

        // DemoFullPackets are recorded in demos every 3,840 ticks (60 secs).
        // Keep track of where they are to allow for fast seeking through the demo.
        var idx = _fullPackets.BinarySearch(FullPacketRecord.ForTick(CurrentDemoTick));
        if (idx < 0)
        {
            _fullPackets.Insert(~idx, new FullPacketRecord(
                CurrentDemoTick,
                _commandStartPosition,
                _stringTables.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.Entries)));
        }

        // We only need to parse the entity snapshot if we're seeking to it
        if (CurrentDemoTick == _readFullPacketTick)
        {
            OnDemoPacket(fullPacket.Packet);
        }
    }
}
