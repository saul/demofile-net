using System.Collections.Immutable;

namespace DemoFile;

public partial class DemoFileReader<TGameParser>
{
    /// <summary>
    /// <c>true</c> when the demo is seeking between ticks, for example
    /// through <see cref="SeekToTickAsync"/>.
    /// </summary>
    public bool IsSeeking { get; private set; }

    private readonly struct SeekScope : IDisposable
    {
        private readonly DemoFileReader<TGameParser> _reader;

        public SeekScope(DemoFileReader<TGameParser> reader)
        {
            _reader = reader;
            reader.IsSeeking = true;
        }

        public void Dispose()
        {
            _reader.IsSeeking = false;
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
    private const int FullPacketInterval = 64 * 60; // todo: fix hardcoded tickrate

    private readonly List<FullPacketRecord> _fullPackets = new(64);
    private DemoTick _readFullPacketTick = DemoTick.PreRecord;
    private int _fullPacketTickOffset;

    internal IReadOnlyList<FullPacketRecord> FullPackets => _fullPackets;
    public int NumFullPackets => _fullPackets.Count;

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
    /// Seeking is not allowed while reading commands. See <see cref="DemoParser{TGameParser}.IsReading"/>.
    /// </remarks>
    public async ValueTask SeekToTickAsync(DemoTick targetTick, CancellationToken cancellationToken)
    {
        using var _ = new SeekScope(this);

        if (_demo.IsReading)
            throw new InvalidOperationException($"Cannot seek to tick while reading commands");

        if (_demo.TickCount < DemoTick.Zero)
            throw new InvalidOperationException($"Cannot seek to tick {targetTick}");

        if (_demo.TickCount != default && targetTick > _demo.TickCount)
            throw new InvalidOperationException($"Cannot seek to tick {targetTick}. The demo only contains data for {_demo.TickCount} ticks");

        // Can never seek before the first tick of the demo
        targetTick = new DemoTick(Math.Max(targetTick.Value, _fullPacketTickOffset));

        var hasFullPacket = TryFindFullPacketBefore(targetTick, out var fullPacket);
        var movedToFullPacket = false;
        if (targetTick <= _demo.CurrentDemoTick)
        {
            if (!hasFullPacket)
                throw new InvalidOperationException($"Cannot seek backwards to tick {targetTick} from {_demo.CurrentDemoTick}. No {nameof(EDemoCommands.DemFullPacket)} has been read");

            // Seeking backwards. Jump back to the full packet to read the snapshot
            RestoreFullPacket(fullPacket);
            movedToFullPacket = true;
        }
        else
        {
            var deltaTicks = fullPacket.Tick - _demo.CurrentDemoTick;

            // Only read the full packet if the jump is far enough ahead
            if (hasFullPacket && deltaTicks.Value >= FullPacketInterval)
            {
                RestoreFullPacket(fullPacket);
                movedToFullPacket = true;
            }
        }

        // Keep reading commands until we reach the full packet
        if (!movedToFullPacket)
        {
            _readFullPacketTick = new DemoTick((targetTick.Value - _fullPacketTickOffset) / FullPacketInterval * FullPacketInterval + _fullPacketTickOffset);
            if (_demo.CurrentDemoTick < _readFullPacketTick)
            {
                await SkipToFullPacketTickAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        // Advance ticks until we get to the target tick
        while (_demo.CurrentDemoTick < targetTick)
        {
            var cmd = ReadCommandHeader();

            if (_demo.CurrentDemoTick == targetTick)
            {
                _stream.Position = _commandStartPosition;
                break;
            }

            if (!await MoveNextCoreAsync(cmd.Command, cmd.IsCompressed, cmd.Size, cancellationToken).ConfigureAwait(false))
            {
                throw new EndOfStreamException($"Reached EOF at tick {_demo.CurrentDemoTick} while seeking to tick {targetTick}");
            }
        }
    }

    private void RestoreFullPacket(FullPacketRecord fullPacket)
    {
        _readFullPacketTick = fullPacket.Tick;
        (_demo.CurrentDemoTick, _stream.Position, var stringTables) = fullPacket;
        _demo.RestoreStringTables(stringTables);
    }

    private async ValueTask SkipToFullPacketTickAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var cmd = ReadCommandHeader();

            // If we're at or after the target tick, jump back to the start of the command
            if (_demo.CurrentDemoTick >= _readFullPacketTick && cmd.Command == EDemoCommands.DemFullPacket)
            {
                // #72: some demos are missing full packets where they should be
                // e.g. in this demo, expect a DemFullPacket at 192001, but all full packets
                // after this point are offset by another 1 tick:
                //   192000 - DemPacket
                //   192002 - DemPacket
                //   192002 - DemFullPacket
                // Update the full tick to seek to accordingly
                _readFullPacketTick = _demo.CurrentDemoTick;

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
    }

    private void OnDemoFullPacket(CDemoFullPacket fullPacket)
    {
        // CDemoFullPacket.string_table only contains tables that have changed
        // since the last CDemoFullPacket, so we need to read each one while seeking.
        if (IsSeeking || _demo.CurrentDemoTick == _readFullPacketTick)
        {
            foreach (var snapshot in fullPacket.StringTable.Tables)
            {
                _demo.OnDemoStringTable(snapshot);
            }
        }

        // DemoFullPackets are recorded in demos every 3,840 ticks (60 secs).
        // Keep track of where they are to allow for fast seeking through the demo.
        var idx = _fullPackets.BinarySearch(FullPacketRecord.ForTick(_demo.CurrentDemoTick));
        if (idx < 0)
        {
            _fullPackets.Insert(~idx, new FullPacketRecord(
                _demo.CurrentDemoTick,
                _commandStartPosition,
                _demo.StringTables.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.Entries)));
        }

        // We only need to parse the entity snapshot if we're seeking to it
        if (_demo.CurrentDemoTick == _readFullPacketTick)
        {
            _demo.OnDemoPacket(new BitBuffer(fullPacket.Packet.Data.Span));
        }
    }
}
