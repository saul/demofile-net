using System.Net;
using System.Text;
using DemoFile.Game.Deadlock;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DemoFile.Test.Integration;

[TestFixture]
public class HttpBroadcastReaderTest
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Action<string> _log;
        private readonly Dictionary<string, int> _urlFailureCounts;
        private readonly string _baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

        public MockHttpMessageHandler(
            Action<string> log,
            IReadOnlyDictionary<string, int> urlFailureCounts)
        {
            _log = log;
            _urlFailureCounts = new Dictionary<string, int>(urlFailureCounts, StringComparer.OrdinalIgnoreCase);
        }

        public void AssertAllUrlsHit()
        {
            Assert.Multiple(() =>
            {
                foreach (var (url, remaining) in _urlFailureCounts)
                {
                    Assert.That(remaining, Is.EqualTo(0), $"Expected '{url}' to be called {remaining} more times");
                }
            });
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
            }

            var requestedPath = request.RequestUri.AbsolutePath.TrimStart('/');

            if (_urlFailureCounts.TryGetValue(requestedPath, out var remainingFailures)
                && remainingFailures > 0)
            {
                _log($"Synthetic HTTP 404 for '{requestedPath}'.  {remainingFailures} failures remaining");

                _urlFailureCounts[requestedPath] = remainingFailures - 1;
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var ext = Path.GetFileName(requestedPath) == "sync"
                ? ".json"
                : ".bin";

            var filePath = Path.Combine(_baseDirectory, requestedPath + ext);

            if (File.Exists(filePath))
            {
                _log($"Reading '{requestedPath}'");

                var fileContent = await File.ReadAllBytesAsync(filePath, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(fileContent)
                };
            }
            else
            {
                _log($"'{requestedPath}' not found => HTTP 404");
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }
    }

    [Test]
    public async Task HttpBroadcast_Deadlock()
    {
        // Arrange
        var clock = new DateTimeOffset(2024, 09, 22, 14, 00, 00, TimeSpan.Zero);
        var snapshot = new DemoSnapshot();
        var demo = new DeadlockDemoParser();
        var httpRequestLog = new StringBuilder();

        var mockMessageHandler = new MockHttpMessageHandler(
            line => httpRequestLog.AppendLine($"{clock:O}: {line}"),
            new Dictionary<string, int>
            {
                {"DeadlockHttpBroadcast/23/delta", 3},
                {"DeadlockHttpBroadcast/28/delta", 10},
                {"DeadlockHttpBroadcast/30/delta", 10},
            });

        var httpClient = new HttpClient(mockMessageHandler)
        {
            BaseAddress = new("http://localhost/DeadlockHttpBroadcast/")
        };

        demo.Source1GameEvents.Source1GameEvent += e =>
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Event {e.GameEventName}@{demo.CurrentGameTick}:");

            var eventJson = JsonSerializer.Serialize(e, DemoJson.SerializerOptions)
                .ReplaceLineEndings(Environment.NewLine + "  ");
            sb.AppendLine($"  {eventJson}");

            snapshot.Add(demo.CurrentDemoTick, $"{clock:O}: {sb}");
        };

        demo.PacketEvents.SvcServerInfo += e =>
        {
            snapshot.Add(demo.CurrentDemoTick, $"{clock:O}: GameTick: {demo.CurrentGameTick}, SvcServerInfo: {JsonSerializer.Serialize(e, DemoJson.SerializerOptions)}");
        };

        string SnapshotPlayerState()
        {
            var sb = new StringBuilder();

            foreach (var player in demo.Players)
            {
                var pawn = player.HeroPawn;

                var abilities = pawn?.CCitadelAbilityComponent.Abilities.Select(handle =>
                {
                    var ability = demo.GetEntityByHandle(handle) as CCitadelBaseAbility;
                    if (ability == null)
                        return null;

                    return new
                    {
                        ability.ServerClass.Name,
                        ability.AbilitySlot,
                        ability.CooldownStart,
                        ability.CooldownEnd,
                        ability.RemainingCharges,
                        ability.ToggledTime,
                        ability.ToggleState,
                        ability.Channeling
                    };
                });

                var playerJson = JsonSerializer.Serialize(
                        new
                        {
                            player.IsActive,
                            PlayerName = player.PlayerName,
                            Team = player.Team.ToString(),
                            IsAlive = pawn?.IsAlive,
                            ControllerHandle = player.EntityHandle,
                            PawnHandle = player.PawnHandle,
                            pawn?.MovementServices?.PositionDeltaVelocity,
                            Origin = pawn?.Origin,
                            Rotation = pawn?.Rotation,
                            EyeAngles = pawn?.EyeAngles,
                            Abilities = abilities
                        },
                        DemoJson.SerializerOptions)
                    .ReplaceLineEndings(Environment.NewLine + "  ");

                sb.AppendLine($"  {playerJson}");
            }

            return sb.ToString();
        }

        var snapshotIntervalTicks = new DemoTick(600); // 10 secs in Deadlock ticks

        void OnSnapshotTimer()
        {
            snapshot.Add(demo.CurrentDemoTick, $"Interval snapshot:{Environment.NewLine}{SnapshotPlayerState()}");

            demo.CreateTimer(demo.CurrentDemoTick + snapshotIntervalTicks, OnSnapshotTimer);
        }

        demo.CreateTimer(DemoTick.Zero, OnSnapshotTimer);

        // Act
        var httpReader = HttpBroadcastReader.Create(demo, httpClient);
        httpReader.DelayAsync = async (msecs, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            clock = clock.AddMilliseconds(msecs);

            // Avoid hot-looping on the thread pool
            await Task.Yield();
        };

        await httpReader.StartReadingAsync(default);
        snapshot.Add(demo.CurrentDemoTick, $"After StartReadingAsync");

        var count = 0;
        while (await httpReader.MoveNextAsync(default))
        {
            snapshot.Add(demo.CurrentDemoTick, $"MoveNextAsync #{++count}");
        }

        snapshot.Add(demo.CurrentDemoTick, $"Finished. HTTP logs follow:{Environment.NewLine}  {httpRequestLog.ToString().ReplaceLineEndings(Environment.NewLine + "  ")}");

        // Assert
        mockMessageHandler.AssertAllUrlsHit();
        Snapshot.Assert(snapshot.ToString());
    }
}
