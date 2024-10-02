using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DemoFile;

public static class DemoFileReader
{
    /// <summary>
    /// Construct a new <c>.dem</c> reader.
    /// </summary>
    /// <param name="demo">The parser to drive.</param>
    /// <param name="stream">A stream of the <c>.dem</c> file.</param>
    public static DemoFileReader<TGameParser> Create<TGameParser>(TGameParser demo, Stream stream)
        where TGameParser : DemoParser<TGameParser>, new()
    {
        return new DemoFileReader<TGameParser>(demo, stream);
    }
}

public partial class DemoFileReader<TGameParser>
    where TGameParser : DemoParser<TGameParser>, new()
{
    private readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Create();
    private readonly TGameParser _demo;
    private readonly Stream _stream;

    private long _commandStartPosition;

    /// <summary>
    /// Incomplete demo files have several commands missing at end of file, due to server abruptly shutting down.
    /// </summary>
    public bool IsIncompleteFile { get; internal set; }
    public long IncompleteFileLastStreamPosition { get; internal set; }

    /// <summary>
    /// Event fired every time a demo command is parsed during <see cref="ReadAllAsync(System.Threading.CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// Only fired if demo is a complete recording (i.e. <see cref="DemoParser{TGameParser}.TickCount"/> is non-zero).
    /// </remarks>
    public Action<DemoProgressEvent>? OnProgress;

    /// <summary>
    /// Construct a new <c>.dem</c> reader.
    /// </summary>
    /// <param name="demo">The parser to drive.</param>
    /// <param name="stream">A stream of the <c>.dem</c> file.</param>
    public DemoFileReader(TGameParser demo, Stream stream)
    {
        _demo = demo;
        _stream = stream;

        // todo: do we just want to hardcode this in MoveNextCoreAsync?
        _demo.DemoEvents.DemoFullPacket += OnDemoFullPacket;
    }

    private static int ReadDemoSize(Span<byte> bytes)
    {
        ReadOnlySpan<int> values = MemoryMarshal.Cast<byte, int>(bytes);
        return values[0];
    }

    private async ValueTask ReadFileInfo(CancellationToken cancellationToken)
    {
        var cmd = ReadCommandHeader();
        Debug.Assert(cmd.Command == EDemoCommands.DemFileInfo);

        // Always treat DemoFileInfo as being at 'pre-record'
        _demo.CurrentDemoTick = DemoTick.PreRecord;

        var rented = _bytePool.Rent(cmd.Size);
        var buf = rented.AsMemory(..cmd.Size);
        await _stream.ReadExactlyAsync(buf, cancellationToken).ConfigureAwait(false);
        _demo.DemoEvents.DemoFileInfo?.Invoke(CDemoFileInfo.Parser.ParseFrom(buf.Span));
        _bytePool.Return(rented);
    }

    private static void ValidateMagic(ReadOnlySpan<byte> magic)
    {
        if (!magic.SequenceEqual("PBDEMS2\x00"u8))
        {
            throw new InvalidDemoException(
                $"Invalid Source 2 demo magic ('{Encoding.ASCII.GetString(magic)}' != expected 'PBDEMS2')");
        }
    }

    /// <summary>
    /// Start reading a demo file.
    /// Each demo command should be read with <see cref="MoveNextAsync"/>,
    /// until it returns <c>false</c>.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop reading the demo header.</param>
    /// <returns>
    /// Task that completes when the demo header has finished reading.
    /// </returns>
    public async ValueTask StartReadingAsync(CancellationToken cancellationToken)
    {
        var rented = _bytePool.Rent(16);
        var buf = rented.AsMemory(..16);
        await _stream.ReadExactlyAsync(buf, cancellationToken).ConfigureAwait(false);
        ValidateMagic(buf.Span[..8]);
        var sizeBytes = ReadDemoSize(buf.Span[8..]);
        _bytePool.Return(rented);

        // `sizeBytes` represents the number of bytes remaining in the demo,
        // from this point (i.e. 16 bytes into the file).

        var isComplete = sizeBytes > 0;
        bool hasDemoFileInfo = false;
        if (_stream.CanSeek && isComplete)
        {
            var oldPosition = _stream.Position;
            _stream.Position = sizeBytes;

            try
            {
                await ReadFileInfo(cancellationToken).ConfigureAwait(false);
                hasDemoFileInfo = true;
            }
            catch (Exception)
            {
                // Swallow any exceptions during ReadFileInfo - it's best effort
            }
            _stream.Position = oldPosition;
        }

        if (!hasDemoFileInfo)
        {
            HandleMissingDemoFileInfo();
        }

        // Keep reading commands until we've passed the PreRecord tick
        while (_demo.CurrentDemoTick == DemoTick.PreRecord)
        {
            var cmd = ReadCommandHeader();
            if (_demo.CurrentDemoTick != DemoTick.PreRecord)
            {
                _fullPacketTickOffset = _demo.CurrentDemoTick.Value;
                Debug.Assert(_fullPacketTickOffset is 0 or 1, "Unexpected first demo tick");

                _stream.Position = _commandStartPosition;
                break;
            }

            if (!await MoveNextCoreAsync(cmd.Command, cmd.IsCompressed, cmd.Size, cancellationToken).ConfigureAwait(false))
            {
                throw new EndOfStreamException($"Reached EOF before reaching tick 0");
            }
        }
    }

    /// <summary>
    /// Read the entire demo file from beginning to end,
    /// with the ability to cancel the parsing through the <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop reading.</param>
    /// <returns>
    /// Task that completes when the demo file has finished reading.
    /// </returns>
    /// <exception cref="InvalidDemoException">Invalid demo file.</exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> was cancelled during reading.
    /// </exception>
    public async ValueTask ReadAllAsync(CancellationToken cancellationToken)
    {
        await StartReadingAsync(cancellationToken).ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!await MoveNextAsync(cancellationToken).ConfigureAwait(false))
                break;

            if (OnProgress is {} onProgress)
            {
                var progressRatio = _demo.TickCount == default
                    ? 0
                    : (float) _demo.CurrentDemoTick.Value / _demo.TickCount.Value;

                onProgress(new DemoProgressEvent(progressRatio));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (EDemoCommands Command, bool IsCompressed, int Size) ReadCommandHeader()
    {
        if (IsIncompleteFile && _stream.Position >= IncompleteFileLastStreamPosition)
        {
            return (EDemoCommands.DemStop, false, 0);
        }

        _commandStartPosition = _stream.Position;
        var command = _stream.ReadUVarInt32();
        var tick = (int) _stream.ReadUVarInt32();
        var size = (int) _stream.ReadUVarInt32();

        _demo.CurrentDemoTick = new DemoTick(tick);

        var isCompressed = (command & (uint) EDemoCommands.DemIsCompressed) != 0;
        var msgType = (EDemoCommands)(command & ~(uint) EDemoCommands.DemIsCompressed);

        return (Command: msgType, IsCompressed: isCompressed, Size: size);
    }

    /// <summary>
    /// Read the entire demo file from beginning to end,
    /// with no ability to cancel the operation.
    /// </summary>
    /// <returns>
    /// Task that completes when the demo file has finished reading.
    /// </returns>
    /// <exception cref="InvalidDemoException">Invalid demo file.</exception>
    public ValueTask ReadAllAsync() => ReadAllAsync(default(CancellationToken));

    /// <summary>
    /// Read the next command in the demo file.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop reading the command.</param>
    /// <returns><c>true</c> if more commands are available in the demo file, otherwise <c>false</c>.</returns>
    public ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cmd = ReadCommandHeader();
        return MoveNextCoreAsync(cmd.Command, cmd.IsCompressed, cmd.Size, cancellationToken);
    }

    private async ValueTask<bool> MoveNextCoreAsync(EDemoCommands msgType, bool isCompressed, int size, CancellationToken cancellationToken)
    {
        var rented = _bytePool.Rent(size);
        var buf = rented.AsMemory(..size);
        await _stream.ReadExactlyAsync(buf, cancellationToken).ConfigureAwait(false);

        using var _ = _demo.StartReadCommandScope(fireTimers: !IsSeeking);
        var canContinue = _demo.DemoEvents.ReadDemoCommand(msgType, buf.Span, isCompressed);
        _bytePool.Return(rented);
        return canContinue;
    }

    private void HandleMissingDemoFileInfo()
    {
        // Manually discover `PlaybackTicks` by reading all commands till end of file, until a failure happens.

        // performance for MemoryStream: 15 ms for 200K ticks
        // performance for FileStream: 80 ms for 200K ticks

        var lastTick = DemoTick.Zero;
        var oldStreamPosition = _stream.Position;
        var lastStreamPosition = oldStreamPosition;
        
        try
        {
            while (true)
            {
                var streamPositionBeforeCommand = _stream.Position;

                var cmd = ReadCommandHeader();

                lastTick = _demo.CurrentDemoTick;
                lastStreamPosition = streamPositionBeforeCommand;

                if (cmd.Command == EDemoCommands.DemStop)
                    break;

                _stream.Position += cmd.Size;
            }
        }
        catch
        {
        }

        IsIncompleteFile = true;
        IncompleteFileLastStreamPosition = lastStreamPosition;

        // invoke event

        // Always treat DemoFileInfo as being at 'pre-record'
        _demo.CurrentDemoTick = DemoTick.PreRecord;

        _demo.DemoEvents.DemoFileInfo?.Invoke(new CDemoFileInfo() { PlaybackTicks = lastTick.Value });

        _stream.Position = oldStreamPosition;
    }
}
