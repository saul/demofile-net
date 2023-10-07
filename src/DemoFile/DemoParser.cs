using System.Buffers;
using System.Text;
using Snappier;

namespace DemoFile;

public sealed partial class DemoParser
{
    private readonly PriorityQueue<QueuedPacket, (int, int)> _packetQueue = new(128);
    private readonly Source1GameEvents _source1GameEvents = new();
    private readonly PriorityQueue<ITickTimer, uint> _tickTimers = new();
    private DemoEvents _demoEvents;
    private GameEvents _gameEvents;
    private PacketEvents _packetEvents;
    private UserMessageEvents _userMessageEvents;

    public DemoParser()
    {
        _demoEvents.DemoFileHeader += msg => { FileHeader = msg; };
        _demoEvents.DemoPacket += OnDemoPacket;
        _demoEvents.DemoClassInfo += OnDemoClassInfo;
        _demoEvents.DemoSendTables += OnDemoSendTables;

        _packetEvents.SvcCreateStringTable += OnCreateStringTable;
        _packetEvents.SvcUpdateStringTable += OnUpdateStringTable;
        _packetEvents.SvcPacketEntities += OnPacketEntities;
        _packetEvents.SvcServerInfo += msg =>
        {
            ServerInfo = msg;
            OnServerInfo(msg);
        };

        _gameEvents.Source1LegacyGameEventList += Source1GameEvents.ParseSource1GameEventList;
        _gameEvents.Source1LegacyGameEvent += Source1GameEvents.ParseSource1GameEvent;
    }

    public ref DemoEvents DemoEvents => ref _demoEvents;
    public ref GameEvents GameEvents => ref _gameEvents;
    public ref PacketEvents PacketEvents => ref _packetEvents;
    public ref UserMessageEvents UserMessageEvents => ref _userMessageEvents;
    public Source1GameEvents Source1GameEvents => _source1GameEvents;

    public CDemoFileHeader FileHeader { get; private set; } = new();

    public GameTick_t CurrentDemoTick { get; private set; }

    public GameTime_t CurrentTime => CurrentDemoTick.ToGameTime();

    public CSVCMsg_ServerInfo ServerInfo { get; private set; } = new();

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

    public ValueTask Start(Stream stream) => Start(stream, default(CancellationToken));

    public async ValueTask Start(Stream stream, CancellationToken cancellationToken)
    {
        ValidateMagic(await ReadExactBytesAsync(stream, 8, cancellationToken));

        // Skip the next 8 bytes, which appear to be two int32s related to the size
        // of the demo file. We may need them in the future, but not so far.
        await ReadExactBytesAsync(stream, 8, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            // Read a command header, which includes both the message type
            // well as a flag to determine whether or not whether or not the
            // message is compressed with snappy.
            var command = stream.ReadUVarInt32();

            var msgType = (EDemoCommands)(command & ~(uint)EDemoCommands.DemIsCompressed);
            if (msgType is < 0 or >= EDemoCommands.DemMax)
                throw new InvalidDemoException($"Unexpected demo command: {command}");

            var isCompressed = (command & (uint)EDemoCommands.DemIsCompressed)
                               == (uint)EDemoCommands.DemIsCompressed;

            var tick = stream.ReadUVarInt32();
            var size = stream.ReadUVarInt32();

            CurrentDemoTick = new GameTick_t(tick);

            while (_tickTimers.TryPeek(out var timer, out var timerTick) && timerTick <= tick)
            {
                _tickTimers.Dequeue();
                timer.Invoke();
            }

            // todo: read into pooled array
            var buf = await ReadExactBytesAsync(stream, (int)size, cancellationToken);

            if (isCompressed)
            {
                using var decompressed = Snappy.DecompressToMemory(buf);
                if (!_demoEvents.ReadDemoCommand(msgType, decompressed.Memory.Span)) break;
            }
            else
            {
                if (!_demoEvents.ReadDemoCommand(msgType, buf)) break;
            }
        }
    }

    private static void ValidateMagic(ReadOnlySpan<byte> magic)
    {
        if (!magic.SequenceEqual("PBDEMS2\x00"u8))
        {
            throw new InvalidDemoException(
                $"Invalid Source 2 demo magic ('{Encoding.ASCII.GetString(magic)}' != expected 'PBDEMS2')");
        }
    }

    private static async ValueTask<byte[]> ReadExactBytesAsync(
        Stream stream,
        int length,
        CancellationToken cancellationToken)
    {
        var result = new byte[length];

        Memory<byte> buffer = result;
        while (buffer.Length > 0)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                throw new InvalidOperationException(
                    "End of stream reached before reading the desired number of bytes.");
            }

            buffer = buffer[read..];
        }

        return result;
    }

    /// <summary>
    /// Schedule a callback at time <paramref name="tick"/>.
    /// The callback will be fired at the start of the tick.
    /// If the tick is in the past, it will fire at the start of the next tick.
    /// Call <c>Dispose</c> on the returned disposable to cancel the callback.
    /// </summary>
    /// <param name="tick">Tick to fire the callback.</param>
    /// <param name="callback">Callback to invoke when <paramref name="tick"/> starts.</param>
    /// <returns>A disposable that cancels the callback on <c>Dispose</c>.</returns>
    public IDisposable StartTimer(GameTick_t tick, Action callback) =>
        StartTimer(tick, callback, static callback => callback());

    /// <summary>
    /// Schedule a callback at time <paramref name="tick"/>.
    /// The callback will be fired at the start of the tick with the state <paramref name="state"/>.
    /// If the tick is in the past, it will fire at the start of the next tick.
    /// Call <c>Dispose</c> on the returned disposable to cancel the callback.
    /// </summary>
    /// <param name="tick">Tick to fire the callback.</param>
    /// <param name="state">State to pass to the callback.</param>
    /// <param name="callback">Callback to invoke when <paramref name="tick"/> starts.</param>
    /// <returns>A disposable that cancels the callback on <c>Dispose</c>.</returns>
    public IDisposable StartTimer<T>(GameTick_t tick, T state, Action<T> callback)
    {
        var timer = new TickTimer<T>(state, callback);
        _tickTimers.Enqueue(timer, tick.Value);
        return timer;
    }
}
