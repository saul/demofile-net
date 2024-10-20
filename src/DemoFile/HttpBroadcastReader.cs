using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace DemoFile;

public static class HttpBroadcastReader
{
    public static HttpBroadcastReader<TGameParser> Create<TGameParser>(DemoParser<TGameParser> demo, HttpClient httpClient)
        where TGameParser : DemoParser<TGameParser>, new()
    {
        return new HttpBroadcastReader<TGameParser>(demo, httpClient);
    }
}

public class HttpBroadcastReader<TGameParser>
    where TGameParser : DemoParser<TGameParser>, new()
{
    private readonly Channel<QueuedCommand> _channel;
    private readonly DemoParser<TGameParser> _demo;
    private readonly HttpClient _httpClient;
    private int _tailTick = -1;

    public HttpBroadcastReader(DemoParser<TGameParser> demo, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _demo = demo;

        _channel = Channel.CreateUnbounded<QueuedCommand>();
    }

    public BroadcastSyncDto? BroadcastSyncDto { get; private set; }

    /// <summary>
    /// The most recently received tick over the HTTP broadcast.
    /// </summary>
    public DemoTick BufferTailTick => new(Volatile.Read(ref _tailTick));

    public int EnqueuedFragmentsCount => _channel.Reader.Count;

    private bool _fetchWorkerIsFullFragment;
    private int _fetchWorkerFragment = 0;

    internal Func<int, CancellationToken, Task> DelayAsync { get; set; } = Task.Delay;

    public int FetchDelayIntervalMs { get; init; } = 1000;

    public int MaxNumConsecutiveFetchErrors { get; init; } = 10;
    private int _numConsecutiveFetchErrors = 0;

    private async Task FetchWorkerAsync(string urlPrefix, int startFragment, CancellationToken cancellationToken)
    {
        _fetchWorkerIsFullFragment = true;
        _fetchWorkerFragment = startFragment;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                bool hasNext = await FetchNextAsync(urlPrefix, cancellationToken).ConfigureAwait(false);
                if (!hasNext)
                    break;
            }
            catch (Exception ex)
            {
                // Possible causes:
                // - connection to server lost
                // - game server abruptly shutting down (but TV http server still running)
                // - received corrupt fragment data from server

                // Complete the Channel so that reader can be unblocked.
                // Note that exception is passed to the function, so reader will fail with same exception when trying to wait on empty Channel.
                _channel.Writer.Complete(ex);
                throw;
            }
        }

        _channel.Writer.Complete();
    }

    private async ValueTask<bool> FetchNextAsync(string urlPrefix, CancellationToken cancellationToken)
    {
        var fragmentUrl = $"{urlPrefix}{_fetchWorkerFragment}/{(_fetchWorkerIsFullFragment ? "full" : "delta")}";

        byte[] fragmentBytes;
        try
        {
            fragmentBytes = await _httpClient.GetByteArrayAsync(fragmentUrl, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exc) when (exc.StatusCode == HttpStatusCode.NotFound)
        {
            _numConsecutiveFetchErrors++;
            if (_numConsecutiveFetchErrors >= MaxNumConsecutiveFetchErrors)
                throw new EndOfStreamException($"Failed to fetch next fragment after {_numConsecutiveFetchErrors} retries, assuming that server was shut down");

            // todo(net8): use time provider
            await DelayAsync(FetchDelayIntervalMs, cancellationToken).ConfigureAwait(false);
            return true;
        }

        _numConsecutiveFetchErrors = 0;

        if (!EnqueueBroadcastFragment(fragmentBytes, 0))
        {
            // Fragment signifies end of the stream
            return false;
        }

        if (_fetchWorkerIsFullFragment)
        {
            _fetchWorkerIsFullFragment = false;
        }
        else
        {
            _fetchWorkerFragment += 1;
        }

        return true;
    }

    /// <summary>
    /// Advance the broadcast by a single HTTP fragment.
    /// In HTTP broadcasts, the tick rate is defined by the `tv_snapshotrate` cvar on the server,
    /// which defaults to 20 for Deadlock.
    /// This means that a single call to <c>MoveNextAsync</c> can advance multiple game ticks (typically 3 ticks per <c>MoveNextAsync</c> call).
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        var readingTick = default(DemoTick?);
        while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!_channel.Reader.TryPeek(out var queued))
            {
                continue;
            }

            if (readingTick.HasValue && queued.Tick > readingTick.Value)
            {
                // We've read all the data for the given tick - break
                return true;
            }

            var msg = await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            // First call to MoveNextAsync should read all PreRecord and first tick of data
            if (msg.Tick > DemoTick.Zero)
            {
                readingTick = msg.Tick;
            }

            _demo.CurrentDemoTick = msg.Tick;
            using var _ = _demo.StartReadCommandScope(fireTimers: true);
            if (msg.Type is EDemoCommands.DemSignonPacket or EDemoCommands.DemPacket)
            {
                _demo.OnDemoPacket(new BitBuffer(msg.Data.Span));
            }
            else
            {
                _demo.DemoEvents.ReadDemoCommand(msg.Type, msg.Data.Span, msg.IsCompressed);
            }
        }

        return false;
    }

    public async ValueTask StartReadingAsync(CancellationToken cancellationToken)
    {
        var syncDto = (await _httpClient.GetFromJsonAsync("sync", BroadcastJsonSerializerContext.Default.BroadcastSyncDto, cancellationToken).ConfigureAwait(false))!;
        if (syncDto.Protocol != 5)
        {
            throw new Exception($"Unknown protocol in broadcast, expected 5, got {syncDto.Protocol}");
        }

        BroadcastSyncDto = syncDto;

        var urlPrefix = string.IsNullOrEmpty(syncDto.TokenRedirect)
            ? ""
            : syncDto.TokenRedirect.TrimEnd('/') + "/";

        var signonUrl = $"{urlPrefix}{syncDto.SignupFragment}/start";
        var signonData = await _httpClient.GetByteArrayAsync(signonUrl, cancellationToken).ConfigureAwait(false);

        EnqueueBroadcastFragment(signonData, -1);

        // Start the fetch worker in the background
        _ = Task.Run(() => FetchWorkerAsync(urlPrefix, syncDto.Fragment, cancellationToken), cancellationToken);
    }

    private bool EnqueueBroadcastFragment(ReadOnlyMemory<byte> fragment, int tickOffset)
    {
        var byteBuf = new ByteBuffer(fragment.Span);

        while (byteBuf.Position < fragment.Length)
        {
            var command = byteBuf.ReadUVarInt32();
            var isCompressed = (command & (uint) EDemoCommands.DemIsCompressed) != 0;
            var msgType = (EDemoCommands)(command & ~(uint) EDemoCommands.DemIsCompressed);

            var rawTickBytes = byteBuf.ReadBytes(4);
            var rawTick = (int)BinaryPrimitives.ReadUInt32LittleEndian(rawTickBytes);
            var demoTick = new DemoTick(rawTick + tickOffset);

            var unknown = byteBuf.ReadByte();
            Debug.Assert(unknown == 0);

            // Signifies the end of the stream
            if (command == 0)
            {
                return false;
            }

            var sizeBytes = byteBuf.ReadBytes(4);
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(sizeBytes);

            var packet = fragment[byteBuf.Position..(byteBuf.Position + size)];
            byteBuf.Position += size;

            var didWrite = _channel.Writer.TryWrite(new QueuedCommand(demoTick, msgType, isCompressed, packet));
            Debug.Assert(didWrite);

            Interlocked.Exchange(ref _tailTick, demoTick.Value);
        }

        return true;
    }

    readonly record struct QueuedCommand(
        DemoTick Tick,
        EDemoCommands Type,
        bool IsCompressed,
        ReadOnlyMemory<byte> Data);
}

// CS2: {"tick":52599,"rtdelay":10,"rcvage":0,"fragment":816,"signup_fragment":0,"tps":64,"protocol":5}
// Deadlock: {"tick":44955,"endtick":45135,"maxtick":46395,"rtdelay":21.904,"rcvage":0.957,"fragment":239,"signup_fragment":0,"tps":60,"keyframe_interval":3,"map":"street_test","protocol":5}
//  keyframe_interval => The frequency, in seconds, of sending keyframes and delta fragments to the broadcast relay server
public class BroadcastSyncDto
{
    [JsonPropertyName("rtdelay")]
    public double RetransmitDelaySeconds { get; set; }

    [JsonPropertyName("rcvage")]
    public double ReceiveAgeSeconds { get; set; }

    [JsonPropertyName("fragment")]
    public int Fragment { get; set; }

    [JsonPropertyName("signup_fragment")]
    public int SignupFragment { get; set; }

    [JsonPropertyName("tps")]
    public int TickRate { get; set; }

    [JsonPropertyName("token_redirect")]
    public string? TokenRedirect { get; set; }

    [JsonPropertyName("protocol")]
    public int Protocol { get; set; }
}

[JsonSerializable(typeof(BroadcastSyncDto))]
public partial class BroadcastJsonSerializerContext : JsonSerializerContext
{}
