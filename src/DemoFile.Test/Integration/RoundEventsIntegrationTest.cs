using System.Text;
using System.Text.Json;

namespace DemoFile.Test.Integration;

[TestFixture]
public class RoundEventsIntegrationTest
{
    private static readonly KeyValuePair<string, Stream>[] RoundCases =
    {
        new("v13963", GotvCompetitiveProtocol13963),
        new("v13992", GotvCompetitiveProtocol13992)
    };

    [TestCaseSource(nameof(RoundCases))]
    public async Task RoundStartEnd(KeyValuePair<string, Stream> testCase)
    {
        // Arrange
        var snapshot = new StringBuilder();
        var demo = new DemoParser();

        demo.EntityEvents.CCSGameRulesProxy.AddChangeCallback(proxy => proxy.GameRules?.RoundStartCount, (_, _, _) =>
        {
            var syntheticEvent = new Source1RoundStartEvent(demo);
            demo.OnCommandFinish += () => LogEvent("RoundStartCount change", syntheticEvent);
        });

        demo.EntityEvents.CCSGameRulesProxy.AddChangeCallback(proxy => proxy.GameRules?.RoundEndCount, (proxy, _, _) =>
        {
            var gameRules = proxy.GameRules!;
            var syntheticEvent = new Source1RoundEndEvent(demo)
            {
                Legacy = gameRules.RoundEndLegacy,
                Message = gameRules.RoundEndMessage,
                Nomusic = gameRules.RoundEndNoMusic ? 1 : 0,
                Reason = gameRules.RoundEndReason,
                Winner = gameRules.RoundEndWinnerTeam,
                PlayerCount = gameRules.RoundEndPlayerCount,
            };

            demo.OnCommandFinish += () => LogEvent("RoundEndCount change", syntheticEvent);
        });

        demo.Source1GameEvents.RoundStart += e => LogEvent(e.GameEventName, e);
        demo.Source1GameEvents.RoundEnd += e => LogEvent(e.GameEventName, e);
        demo.Source1GameEvents.PlayerDeath += e => LogEvent(e.GameEventName, e);

        // Act
        await demo.ReadAllAsync(testCase.Value, default);

        // Assert
        Snapshot.Assert(snapshot.ToString());

        void LogEvent<T>(string eventName, T evt)
        {
            snapshot.AppendLine($"[{demo.CurrentDemoTick.Value}] {eventName}:");

            var eventJson = JsonSerializer.Serialize(evt, DemoJson.SerializerOptions)
                .ReplaceLineEndings(Environment.NewLine + "  ");
            snapshot.AppendLine($"  {eventJson}");
        }
    }
}
