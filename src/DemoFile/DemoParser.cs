using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Snappier;

namespace DemoFile;

public sealed partial class DemoParser
{
    /// Key ticks occur every 60 seconds
    private const int KeyTickInterval = 64 * 60;

    private readonly PriorityQueue<ITickTimer, int> _demoTickTimers = new();
    private readonly Dictionary<DemoTick, long> _keyTickPositions = new();
    private readonly PriorityQueue<QueuedPacket, (int, int)> _packetQueue = new(128);
    private readonly PriorityQueue<ITickTimer, uint> _serverTickTimers = new();
    private readonly Source1GameEvents _source1GameEvents = new();
    private DemoEvents _demoEvents;
    private EntityEvents _entityEvents;
    private GameEvents _gameEvents;
    private PacketEvents _packetEvents;

    private Stream _stream;
    private UserMessageEvents _userMessageEvents;

    /// <summary>
    /// Event fired every time a demo command is parsed during <see cref="ReadAllAsync(System.IO.Stream)"/>.
    /// </summary>
    /// <remarks>
    /// Only fired if demo is a complete recording (i.e. <see cref="TickCount"/> is non-zero).
    /// </remarks>
    public Action<DemoProgressEvent>? OnProgress;

    public DemoParser()
    {
        _stream = null!;

        _demoEvents.DemoFileHeader += msg => { FileHeader = msg; };
        _demoEvents.DemoPacket += OnDemoPacket;
        _demoEvents.DemoClassInfo += OnDemoClassInfo;
        _demoEvents.DemoSendTables += OnDemoSendTables;
        _demoEvents.DemoFileInfo += OnDemoFileInfo;

        _packetEvents.SvcCreateStringTable += OnCreateStringTable;
        _packetEvents.SvcUpdateStringTable += OnUpdateStringTable;
        _packetEvents.SvcPacketEntities += OnPacketEntities;
        _packetEvents.SvcServerInfo += msg =>
        {
            ServerInfo = msg;
            OnServerInfo(msg);
        };
        _packetEvents.NetTick += OnNetTick;

        _gameEvents.Source1LegacyGameEventList += Source1GameEvents.ParseSource1GameEventList;
        _gameEvents.Source1LegacyGameEvent += @event => Source1GameEvents.ParseSource1GameEvent(this, @event);
    }

    public ref DemoEvents DemoEvents => ref _demoEvents;
    public ref GameEvents GameEvents => ref _gameEvents;
    public ref PacketEvents PacketEvents => ref _packetEvents;
    public ref UserMessageEvents UserMessageEvents => ref _userMessageEvents;
    public Source1GameEvents Source1GameEvents => _source1GameEvents;
    public ref EntityEvents EntityEvents => ref _entityEvents;

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
    /// <c>true</c> if the recording client is GOTV. <c>false</c> if this is a POV demo.
    /// </summary>
    public bool IsGotv { get; private set; }

    private void OnDemoFileInfo(CDemoFileInfo fileInfo)
    {
        TickCount = new DemoTick(fileInfo.PlaybackTicks);
    }

    private void OnDemoPacket(CDemoPacket msg)
    {
        var arrayPool = ArrayPool<byte>.Shared;
        var buffer = new BitBuffer(msg.Data.Span);

        // Read all messages from the buffer. Messages are packed serially as
        // {type, size, data}. We keep reading until there's nothing left.
        var index = 0;
        while (buffer.RemainingBytes > 0)
        {
            var msgType = (int) buffer.ReadUBitVar();
            var size = (int) buffer.ReadUVarInt32();

            var rentedBuffer = arrayPool.Rent(size);
            var msgBuf = ((Span<byte>) rentedBuffer)[..size];
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
                && !_gameEvents.ParseGameEvent(queued.MsgType, msgBuf)
                && !_userMessageEvents.ParseUserMessage(queued.MsgType, msgBuf))
            {
            }

            arrayPool.Return(queued.RentedBuf);
        }
    }

    private static int ReadDemoSize(byte[] bytes)
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
        _keyTickPositions.Clear();
        _stream = stream;

        ValidateMagic(await ReadExactBytesAsync(8, cancellationToken).ConfigureAwait(false));
        var sizeBytes = ReadDemoSize(await ReadExactBytesAsync(8, cancellationToken).ConfigureAwait(false));

        // `sizeBytes` represents the number of bytes remaining in the demo,
        // from this point (i.e. 16 bytes into the file).

        var isComplete = sizeBytes > 0;
        if (stream.CanSeek && isComplete)
        {
            var oldPosition = stream.Position;
            stream.Position = sizeBytes;
            await ReadFileInfo(cancellationToken).ConfigureAwait(false);
            stream.Position = oldPosition;
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
    private (uint Command, uint Size) ReadCommandHeader()
    {
        var startPosition = _stream.Position;
        var command = _stream.ReadUVarInt32();
        var tick = (int) _stream.ReadUVarInt32();
        var size = _stream.ReadUVarInt32();

        CurrentDemoTick = new DemoTick(tick);

        if (tick % KeyTickInterval == 0)
        {
            // Keep the first time we see a tick
            _keyTickPositions.TryAdd(CurrentDemoTick, startPosition);
        }

        return (Command: command, Size: size);
    }

    /// <summary>
    /// Read the next command in the demo file.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop reading the command.</param>
    /// <returns><c>true</c> if more commands are available in the demo file, otherwise <c>false</c>.</returns>
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken)
    {
        var cmd = ReadCommandHeader();

        var msgType = (EDemoCommands)(cmd.Command & ~(uint)EDemoCommands.DemIsCompressed);
        if (msgType is < 0 or >= EDemoCommands.DemMax)
            throw new InvalidDemoException($"Unexpected demo command: {cmd.Command}");

        var isCompressed = (cmd.Command & (uint)EDemoCommands.DemIsCompressed)
                           == (uint)EDemoCommands.DemIsCompressed;

        while (_demoTickTimers.TryPeek(out var timer, out var timerTick) && timerTick <= CurrentDemoTick.Value)
        {
            _demoTickTimers.Dequeue();
            timer.Invoke();
        }

        // todo: read into pooled array
        var buf = await ReadExactBytesAsync((int)cmd.Size, cancellationToken).ConfigureAwait(false);

        // todo: disable seeking until command is over!
        if (isCompressed)
        {
            using var decompressed = Snappy.DecompressToMemory(buf);
            return _demoEvents.ReadDemoCommand(msgType, decompressed.Memory.Span);
        }
        else
        {
            return _demoEvents.ReadDemoCommand(msgType, buf);
        }
    }

    private async ValueTask ReadFileInfo(CancellationToken cancellationToken)
    {
        var cmd = ReadCommandHeader();
        Debug.Assert(cmd.Command == (uint)EDemoCommands.DemFileInfo);

        // Always treat DemoFileInfo as being at 'pre-record'
        CurrentDemoTick = DemoTick.PreRecord;

        var buf = await ReadExactBytesAsync((int)cmd.Size, cancellationToken).ConfigureAwait(false);
        DemoEvents.DemoFileInfo?.Invoke(CDemoFileInfo.Parser.ParseFrom(buf));
    }

    private static void ValidateMagic(ReadOnlySpan<byte> magic)
    {
        if (!magic.SequenceEqual("PBDEMS2\x00"u8))
        {
            throw new InvalidDemoException(
                $"Invalid Source 2 demo magic ('{Encoding.ASCII.GetString(magic)}' != expected 'PBDEMS2')");
        }
    }

    private async ValueTask<byte[]> ReadExactBytesAsync(
        int length,
        CancellationToken cancellationToken)
    {
        var result = new byte[length];
        await _stream.ReadExactlyAsync(result, 0, length, cancellationToken).ConfigureAwait(false);
        return result;
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
