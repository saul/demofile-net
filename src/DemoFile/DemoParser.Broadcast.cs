using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DemoFile;

public partial class DemoParser
{
    public async Task ReadBroadcastAsync(Uri baseUrl, CancellationToken cancellationToken)
    {
        // todo: take a HTTP client as input
        using var httpClient = new HttpClient();

        var syncUrl = AppendPath(baseUrl, "sync");
        var syncDto = (await httpClient.GetFromJsonAsync(syncUrl, BroadcastJsonSerializerContext.Default.BroadcastSyncDto, cancellationToken).ConfigureAwait(false))!;
        if (syncDto.Protocol != 5)
        {
            throw new Exception($"Unknown protocol in broadcast, expected 5, got {syncDto.Protocol}");
        }

        var signonUrl = AppendPath(baseUrl, $"{syncDto.SignupFragment}/start");
        var signonData = await httpClient.GetByteArrayAsync(signonUrl, cancellationToken).ConfigureAwait(false);
        ReadBroadcastFragment(signonData, -1);

        var isFullFragment = true;
        var fragment = syncDto.Fragment;
        var failedFetches = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var fragmentUrl = AppendPath(
                baseUrl,
                $"{fragment}/{(isFullFragment ? "full" : "delta")}");

            byte[] fragmentBytes;
            try
            {
                fragmentBytes = await httpClient.GetByteArrayAsync(fragmentUrl, cancellationToken).ConfigureAwait(false);
                failedFetches = 0;
            }
            catch (HttpRequestException exc) when (exc.StatusCode == HttpStatusCode.NotFound && failedFetches < 10)
            {
                // todo: after n retries, re-sync?

                failedFetches += 1;
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (!ReadBroadcastFragment(fragmentBytes, 0))
            {
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

    private bool ReadBroadcastFragment(byte[] fragmentBytes, int tickOffset)
    {
        var byteBuf = new ByteBuffer(fragmentBytes);

        while (byteBuf.Position < fragmentBytes.Length)
        {
            var command = byteBuf.ReadUVarInt32();

            var gameTickBytes = byteBuf.ReadBytes(4);
            var gameTick = (int)BinaryPrimitives.ReadUInt32LittleEndian(gameTickBytes);
            CurrentDemoTick = new DemoTick(gameTick + tickOffset);

            var unknown = byteBuf.ReadByte();
            Debug.Assert(unknown == 0);

            // Signifies the end of the stream
            if (command == 0)
            {
                return false;
            }

            var sizeBytes = byteBuf.ReadBytes(4);
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(sizeBytes);

            var isCompressed = (command & (uint) EDemoCommands.DemIsCompressed) != 0;
            var msgType = (EDemoCommands)(command & ~(uint) EDemoCommands.DemIsCompressed);

            var packet = byteBuf.ReadBytes(size);

            if (msgType is EDemoCommands.DemSignonPacket or EDemoCommands.DemPacket)
            {
                OnDemoPacketCore(new BitBuffer(packet));
            }
            else
            {
                ReadCommand(msgType, isCompressed, packet);
            }
        }

        return true;
    }

    private static Uri AppendPath(Uri baseUrl, string path)
    {
        var uriBuilder = new UriBuilder(baseUrl);

        if (!uriBuilder.Path.EndsWith("/"))
        {
            uriBuilder.Path += "/";
        }

        uriBuilder.Path += path;

        return uriBuilder.Uri;
    }

    // {"tick":52599,"rtdelay":10,"rcvage":0,"fragment":816,"signup_fragment":0,"tps":64,"protocol":5}
    private class BroadcastSyncDto
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
    private partial class BroadcastJsonSerializerContext : JsonSerializerContext
    {}
}
