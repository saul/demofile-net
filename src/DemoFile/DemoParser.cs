using System.Buffers;
using System.Diagnostics;

namespace DemoFile;

public abstract partial class DemoParser<TGameParser>
    where TGameParser : DemoParser<TGameParser>, new()
{
    private readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Create();
    private readonly PriorityQueue<ITickTimer, int> _demoTickTimers = new();
    private readonly PriorityQueue<QueuedPacket, (int, int)> _packetQueue = new(128);
    private readonly PriorityQueue<ITickTimer, uint> _serverTickTimers = new();

    private DemoEvents _demoEvents;
    private BaseGameEvents _baseGameEvents;
    private PacketEvents _packetEvents;
    private TempEntityEvents _tempEntityEvents;
    private BaseUserMessageEvents _baseUserMessageEvents;

    /// <summary>
    /// Event fired when the current demo command has finished.
    /// This field is reset to <c>null</c> just before it is invoked,
    /// so consumers must set it every call if needed.
    /// </summary>
    public Action? OnCommandFinish;

    protected DemoParser()
    {
        if (this is not TGameParser)
        {
            throw new InvalidOperationException($"{GetType()} must derive from {typeof(TGameParser)}");
        }

        _demoEvents.DemoFileHeader += msg => { FileHeader = msg; };
        _demoEvents.DemoPacket += msg => OnDemoPacket(new BitBuffer(msg.Data.Span));
        _demoEvents.DemoClassInfo += OnDemoClassInfo;
        _demoEvents.DemoSendTables += OnDemoSendTables;
        _demoEvents.DemoFileInfo += OnDemoFileInfo;
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
        _packetEvents.SvcClearAllStringTables += OnClearStringTables;
    }

    /// <summary>
    /// Flag indicate whether the parser is currently reading a command.
    /// During reading, seeking (e.g. with <see cref="DemoFileReader{TGameParser}.SeekToTickAsync"/>) is not possible.
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

    public DemoTick CurrentDemoTick { get; internal set; } = DemoTick.PreRecord;

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

    internal void OnDemoPacket(BitBuffer buffer)
    {
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

    internal bool ReadCommand(EDemoCommands msgType, bool isCompressed, ReadOnlySpan<byte> buffer, bool fireTimers)
    {
        IsReading = true;

        Debug.Assert(msgType is >= 0 and < EDemoCommands.DemMax, $"Unexpected demo command: {msgType}");

        //Console.WriteLine($"{msgType}({(int)msgType}) - {buffer.Length:N0} bytes");

        if (fireTimers)
        {
            while (_demoTickTimers.TryPeek(out var timer, out var timerTick) && timerTick <= CurrentDemoTick.Value)
            {
                _demoTickTimers.Dequeue();
                timer.Invoke();
            }
        }

        var canContinue = _demoEvents.ReadDemoCommand(msgType, buffer, isCompressed);

        if (OnCommandFinish is { } onCommandFinish)
        {
            // Reset to null before invoking to allow any callbacks to re-register
            OnCommandFinish = null;
            onCommandFinish();
        }

        IsReading = false;
        return canContinue;
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
