using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DemoFile.Sdk;

namespace DemoFile;

public abstract partial class DemoParser<TGameParser>
    where TGameParser : DemoParser<TGameParser>, new()
{
    private readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Create();
    private readonly PriorityQueue<ITickTimer, int> _demoTickTimers = new();
    private readonly PriorityQueue<QueuedPacket, (int, int)> _packetQueue = new(128);
    private readonly PriorityQueue<ITickTimer, uint> _serverTickTimers = new();

    private long _commandStartPosition;
    private DemoEvents _demoEvents;
    private BaseGameEvents _baseGameEvents;
    private PacketEvents _packetEvents;
    private Stream _stream;
    private TempEntityEvents _tempEntityEvents;
    private BaseUserMessageEvents _baseUserMessageEvents;

    /// <summary>
    /// Event fired when the current demo command has finished (e.g, just before <see cref="MoveNextAsync"/> returns).
    /// Reset to <c>null</c> just before it is invoked.
    /// </summary>
    public Action? OnCommandFinish;

    /// <summary>
    /// Event fired every time a demo command is parsed during <see cref="ReadAllAsync(System.IO.Stream)"/>.
    /// </summary>
    /// <remarks>
    /// Only fired if demo is a complete recording (i.e. <see cref="TickCount"/> is non-zero).
    /// </remarks>
    public Action<DemoProgressEvent>? OnProgress;

    protected DemoParser()
    {
        _stream = null!;

        _demoEvents.DemoFileHeader += msg => { FileHeader = msg; };
        _demoEvents.DemoPacket += OnDemoPacket;
        _demoEvents.DemoClassInfo += OnDemoClassInfo;
        _demoEvents.DemoSendTables += OnDemoSendTables;
        _demoEvents.DemoFileInfo += OnDemoFileInfo;
        _demoEvents.DemoFullPacket += OnDemoFullPacket;
        _demoEvents.DemoStringTables += OnDemoStringTables;

        _packetEvents.SvcCreateStringTable += OnCreateStringTable;
        _packetEvents.SvcUpdateStringTable += OnUpdateStringTable;
        _packetEvents.SvcPacketEntities += OnPacketEntities;
        _packetEvents.SvcServerInfo += msg =>
        {
            ServerInfo = msg;
            OnServerInfo(msg);
        };
        _packetEvents.NetTick += OnNetTick;
    }

    /// <summary>
    /// Flag indicate whether the parser is currently reading a command.
    /// During reading, seeking (e.g. with <see cref="SeekToTickAsync"/>) is not possible.
    /// </summary>
    public bool IsReading { get; private set; }

    public ref DemoEvents DemoEvents => ref _demoEvents;
    public ref BaseGameEvents BaseGameEvents => ref _baseGameEvents;
    public ref PacketEvents PacketEvents => ref _packetEvents;
    public ref BaseUserMessageEvents BaseUserMessageEvents => ref _baseUserMessageEvents;
    public ref TempEntityEvents TempEntityEvents => ref _tempEntityEvents;

    public CDemoFileHeader FileHeader { get; private set; } = new();

    public GameTick CurrentGameTick { get; private set; }
    public GameTime CurrentGameTime => CurrentGameTick.ToGameTime();

    public DemoTick CurrentDemoTick { get; private set; } = DemoTick.PreRecord;

    /// <summary>
    /// Total number of ticks in the demo.
    /// Only available when parsing stream that can be seeked,
    /// as the information is located at the end of the demo file.
    /// </summary>
    public DemoTick TickCount { get; private set; }

    public TimeSpan Elapsed => TimeSpan.FromSeconds(Math.Max(0, CurrentDemoTick.Value) / 64.0f);

    public CSVCMsg_ServerInfo ServerInfo { get; private set; } = new();

    /// <summary>
    /// <c>true</c> if the demo was recorded on the game server, or a TV relay with full state.
    /// <c>false</c> if this is a POV demo.
    /// </summary>
    public bool IsTvRecording { get; private set; }

    private void OnDemoFileInfo(CDemoFileInfo fileInfo)
    {
        TickCount = new DemoTick(fileInfo.PlaybackTicks);
    }

    private void OnDemoPacket(CDemoPacket msg)
    {
        var buffer = new BitBuffer(msg.Data.Span);

        // Read all messages from the buffer. Messages are packed serially as
        // {type, size, data}. We keep reading until there's nothing left.
        var index = 0;
        while (buffer.RemainingBytes > 0)
        {
            var msgType = (int) buffer.ReadUBitVar();
            var size = (int) buffer.ReadUVarInt32();

            var rentedBuffer = _bytePool.Rent(size);
            var msgBuf = rentedBuffer.AsSpan(..size);
            buffer.ReadBytes(msgBuf);

            // Queue packets to be read in a specific order.
            // For example, we want to read game events after entities have updated.
            var queuedPacket = new QueuedPacket(msgType, rentedBuffer, size);
            _packetQueue.Enqueue(queuedPacket, (QueuedPacket.GetPriority(msgType), index++));
        }

        while (_packetQueue.TryDequeue(out var queued, out _))
        {
            var msgBuf = queued.MsgBuffer;

            if (!_packetEvents.ParseNetMessage(queued.MsgType, msgBuf)
                && !_baseGameEvents.ParseGameEvent(queued.MsgType, msgBuf)
                && !_baseUserMessageEvents.ParseUserMessage(queued.MsgType, msgBuf)
                && !_tempEntityEvents.ParseNetMessage(queued.MsgType, msgBuf)
                && !ParseNetMessage(queued.MsgType, msgBuf))
            {
            }

            _bytePool.Return(queued.RentedBuf);
        }
    }

    private static int ReadDemoSize(Span<byte> bytes)
    {
        ReadOnlySpan<int> values = MemoryMarshal.Cast<byte, int>(bytes);
        return values[0];
    }

    /// <summary>
    /// Start reading a demo file.
    /// Each demo command should be read with <see cref="MoveNextAsync"/>,
    /// until it returns <c>false</c>.
    /// </summary>
    /// <param name="stream">A stream of the <c>.dem</c> file.</param>
    /// <param name="cancellationToken">A cancellation token to stop reading the demo header.</param>
    /// <returns>
    /// Task that completes when the demo header has finished reading.
    /// </returns>
    public async ValueTask StartReadingAsync(Stream stream, CancellationToken cancellationToken)
    {
        _fullPackets.Clear();
        _stream = stream;

        var rented = _bytePool.Rent(16);
        var buf = rented.AsMemory(..16);
        await _stream.ReadExactlyAsync(buf, cancellationToken).ConfigureAwait(false);
        ValidateMagic(buf.Span[..8]);
        var sizeBytes = ReadDemoSize(buf.Span[8..]);
        _bytePool.Return(rented);

        // `sizeBytes` represents the number of bytes remaining in the demo,
        // from this point (i.e. 16 bytes into the file).

        var isComplete = sizeBytes > 0;
        if (stream.CanSeek && isComplete)
        {
            var oldPosition = stream.Position;
            stream.Position = sizeBytes;

            try
            {
                await ReadFileInfo(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Swallow any exceptions during ReadFileInfo - it's best effort
            }
            stream.Position = oldPosition;
        }

        // Keep reading commands until we've passed the PreRecord tick
        while (CurrentDemoTick == DemoTick.PreRecord)
        {
            var cmd = ReadCommandHeader();
            if (CurrentDemoTick != DemoTick.PreRecord)
            {
                _fullPacketTickOffset = CurrentDemoTick.Value;
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
    /// with no ability to cancel the operation.
    /// </summary>
    /// <param name="stream">A stream of the <c>.dem</c> file.</param>
    /// <returns>
    /// Task that completes when the demo file has finished reading.
    /// </returns>
    /// <exception cref="InvalidDemoException">Invalid demo file.</exception>
    public ValueTask ReadAllAsync(Stream stream) => ReadAllAsync(stream, default(CancellationToken));

    /// <summary>
    /// Read the entire demo file from beginning to end,
    /// with the ability to cancel the parsing through the <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="stream">A stream of the <c>.dem</c> file.</param>
    /// <param name="cancellationToken">A cancellation token to stop reading.</param>
    /// <returns>
    /// Task that completes when the demo file has finished reading.
    /// </returns>
    /// <exception cref="InvalidDemoException">Invalid demo file.</exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> was cancelled during reading.
    /// </exception>
    public async ValueTask ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        await StartReadingAsync(stream, cancellationToken).ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!await MoveNextAsync(cancellationToken).ConfigureAwait(false))
                break;

            if (OnProgress is {} onProgress)
            {
                var progressRatio = TickCount == default
                    ? 0
                    : (float) CurrentDemoTick.Value / TickCount.Value;

                onProgress(new DemoProgressEvent(progressRatio));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (EDemoCommands Command, bool IsCompressed, int Size) ReadCommandHeader()
    {
        _commandStartPosition = _stream.Position;
        var command = _stream.ReadUVarInt32();
        var tick = (int) _stream.ReadUVarInt32();
        var size = (int) _stream.ReadUVarInt32();

        CurrentDemoTick = new DemoTick(tick);

        var isCompressed = (command & (uint) EDemoCommands.DemIsCompressed) != 0;
        var msgType = (EDemoCommands)(command & ~(uint) EDemoCommands.DemIsCompressed);

        return (Command: msgType, IsCompressed: isCompressed, Size: size);
    }

    /// <summary>
    /// Read the next command in the demo file.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop reading the command.</param>
    /// <returns><c>true</c> if more commands are available in the demo file, otherwise <c>false</c>.</returns>
    public ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken)
    {
        var cmd = ReadCommandHeader();
        return MoveNextCoreAsync(cmd.Command, cmd.IsCompressed, cmd.Size, cancellationToken);
    }

    private async ValueTask<bool> MoveNextCoreAsync(EDemoCommands msgType, bool isCompressed, int size, CancellationToken cancellationToken)
    {
        IsReading = true;

        Debug.Assert(msgType is >= 0 and < EDemoCommands.DemMax, $"Unexpected demo command: {msgType}");

        if (!IsSeeking)
        {
            while (_demoTickTimers.TryPeek(out var timer, out var timerTick) && timerTick <= CurrentDemoTick.Value)
            {
                _demoTickTimers.Dequeue();
                timer.Invoke();
            }
        }

        var rented = _bytePool.Rent(size);
        var buf = rented.AsMemory(..size);
        await _stream.ReadExactlyAsync(buf, cancellationToken).ConfigureAwait(false);

        var canContinue = _demoEvents.ReadDemoCommand(msgType, buf.Span, isCompressed);

        if (OnCommandFinish is { } onCommandFinish)
        {
            // Reset to null before invoking to allow any callbacks to re-register
            OnCommandFinish = null;
            onCommandFinish();
        }

        IsReading = false;
        _bytePool.Return(rented);
        return canContinue;
    }

    private async ValueTask ReadFileInfo(CancellationToken cancellationToken)
    {
        var cmd = ReadCommandHeader();
        Debug.Assert(cmd.Command == EDemoCommands.DemFileInfo);

        // Always treat DemoFileInfo as being at 'pre-record'
        CurrentDemoTick = DemoTick.PreRecord;

        var rented = _bytePool.Rent(cmd.Size);
        var buf = rented.AsMemory(..cmd.Size);
        await _stream.ReadExactlyAsync(buf, cancellationToken).ConfigureAwait(false);
        DemoEvents.DemoFileInfo?.Invoke(CDemoFileInfo.Parser.ParseFrom(buf.Span));
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
    /// Schedule a callback at demo tick <paramref name="tick"/>.
    /// The callback will be fired at the start of the demo tick.
    /// If the tick is in the past, it will fire at the start of the next demo message.
    /// Call <c>Dispose</c> on the returned disposable to cancel the callback.
    /// </summary>
    /// <param name="tick">Tick to fire the callback.</param>
    /// <param name="callback">Callback to invoke when <paramref name="tick"/> starts.</param>
    /// <returns>A disposable that cancels the callback on <c>Dispose</c>.</returns>
    public IDisposable CreateTimer(DemoTick tick, Action callback) =>
        CreateTimer(tick, callback, static callback => callback());

    /// <summary>
    /// Schedule a callback at demo tick <paramref name="tick"/>.
    /// The callback will be fired at the start of the demo tick with the state <paramref name="state"/>.
    /// If the tick is in the past, it will fire at the start of the next demo message.
    /// Call <c>Dispose</c> on the returned disposable to cancel the callback.
    /// </summary>
    /// <param name="tick">Tick to fire the callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    /// <param name="callback">Callback to invoke when <paramref name="tick"/> starts.</param>
    /// <returns>A disposable that cancels the callback on <c>Dispose</c>.</returns>
    public IDisposable CreateTimer<T>(DemoTick tick, T state, Action<T> callback)
    {
        var timer = new TickTimer<T>(state, callback);
        _demoTickTimers.Enqueue(timer, tick.Value);
        return timer;
    }

    /// <summary>
    /// Schedule a callback at game tick <paramref name="tick"/>.
    /// The callback will be fired at the start of the game tick.
    /// If the tick is in the past, it will fire at the start of the next <see cref="CNETMsg_Tick"/> message.
    /// Call <c>Dispose</c> on the returned disposable to cancel the callback.
    /// </summary>
    /// <param name="tick">Tick to fire the callback.</param>
    /// <param name="callback">Callback to invoke when <paramref name="tick"/> starts.</param>
    /// <returns>A disposable that cancels the callback on <c>Dispose</c>.</returns>
    public IDisposable CreateTimer(GameTick tick, Action callback) =>
        CreateTimer(tick, callback, static callback => callback());

    /// <summary>
    /// Schedule a callback at game tick <paramref name="tick"/>.
    /// The callback will be fired at the start of the game tick with the state <paramref name="state"/>.
    /// If the tick is in the past, it will fire at the start of the next demo message.
    /// Call <c>Dispose</c> on the returned disposable to cancel the callback.
    /// </summary>
    /// <param name="tick">Tick to fire the callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    /// <param name="callback">Callback to invoke when <paramref name="tick"/> starts.</param>
    /// <returns>A disposable that cancels the callback on <c>Dispose</c>.</returns>
    public IDisposable CreateTimer<T>(GameTick tick, T state, Action<T> callback)
    {
        var timer = new TickTimer<T>(state, callback);
        _serverTickTimers.Enqueue(timer, tick.Value);
        return timer;
    }
}
