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

    public DemoTick BufferTailTick => new(Volatile.Read(ref _tailTick));

    private async Task FetchWorkerAsync(string urlPrefix, int startFragment, CancellationToken cancellationToken)
    {
        var isFullFragment = true;
        var fragment = startFragment;

        while (!cancellationToken.IsCancellationRequested)
        {
            var fragmentUrl = $"{urlPrefix}{fragment}/{(isFullFragment ? "full" : "delta")}";

            byte[] fragmentBytes;
            try
            {
                fragmentBytes = await _httpClient.GetByteArrayAsync(fragmentUrl, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException exc) when (exc.StatusCode == HttpStatusCode.NotFound)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                continue;
            }

            Console.WriteLine($"[*] read fragment {fragmentUrl}...");
            if (!EnqueueBroadcastFragment(fragmentBytes, 0))
            {
                // Fragment signifies end of the stream
                _channel.Writer.Complete();
                return;
            }

            if (isFullFragment)
            {
                isFullFragment = false;
            }
            else
            {
                fragment += 1;
            }
        }
    }

    /// <summary>
    /// Advance the broadcast by a single tick.
    /// In HTTP broadcasts, the tick rate is defined by the `tv_snapshotrate` cvar on the server,
    /// which defaults to 20 for Deadlock.
    /// This means that a single call to <c>MoveNextAsync</c> can advance multiple game ticks (typically 3 ticks per <c>MoveNextAsync</c> call).
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        //Console.WriteLine($"reading at {_demo.CurrentDemoTick}");
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
            if (msg.Type is EDemoCommands.DemSignonPacket or EDemoCommands.DemPacket)
            {
                _demo.OnDemoPacket(new BitBuffer(msg.Data.Span));
            }
            else
            {
                _demo.ReadCommand(msg.Type, msg.IsCompressed, msg.Data.Span, fireTimers: true);
            }
        }

        return false;
    }

    public async Task StartReadingAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[*] sync...");
        var syncDto = (await _httpClient.GetFromJsonAsync("sync", BroadcastJsonSerializerContext.Default.BroadcastSyncDto, cancellationToken).ConfigureAwait(false))!;
        if (syncDto.Protocol != 5)
        {
            throw new Exception($"Unknown protocol in broadcast, expected 5, got {syncDto.Protocol}");
        }

        Console.WriteLine($"    {JsonSerializer.Serialize(syncDto, BroadcastJsonSerializerContext.Default.BroadcastSyncDto)}");

        var urlPrefix = string.IsNullOrEmpty(syncDto.TokenRedirect)
            ? ""
            : syncDto.TokenRedirect.TrimEnd('/') + "/";

        var signonUrl = $"{urlPrefix}{syncDto.SignupFragment}/start";

        Console.WriteLine($"[*] signon: {signonUrl}");
        //var signonData = await _httpClient.GetByteArrayAsync(signonUrl, cancellationToken).ConfigureAwait(false);
        var signonResponse = await _httpClient.GetAsync(signonUrl, cancellationToken).ConfigureAwait(false);
        signonResponse.EnsureSuccessStatusCode();

        var signonData = await signonResponse.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        Console.WriteLine("[*] read signon...");
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
            //Console.WriteLine($"    msgType = {msgType} (compressed = {isCompressed})");

            var rawTickBytes = byteBuf.ReadBytes(4);
            var rawTick = (int)BinaryPrimitives.ReadUInt32LittleEndian(rawTickBytes);
            //Console.WriteLine($"    tick = {gameTick}");
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
            //Console.WriteLine($"    size = {size:N0} bytes");

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
class BroadcastSyncDto
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
partial class BroadcastJsonSerializerContext : JsonSerializerContext
{}
