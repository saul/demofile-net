using System.Text;
using System.Text.Json;
using DemoFile.Game.Cs;

namespace DemoFile.Test.Integration;

[TestFixtureSource(typeof(GlobalUtil), nameof(ParseModes))]
public class RoundEventsIntegrationTest
{
    private static readonly KeyValuePair<string, byte[]>[] RoundCases =
    {
        new("v13963", GotvCompetitiveProtocol13963),
        new("v13992", GotvCompetitiveProtocol13992)
    };

    private readonly ParseMode _mode;

    public RoundEventsIntegrationTest(ParseMode mode)
    {
        _mode = mode;
    }

    [TestCaseSource(nameof(RoundCases))]
    public async Task RoundStartEnd(KeyValuePair<string, byte[]> testCase)
    {
        // Arrange
        DemoSnapshot ParseSection(CsDemoParser demo)
        {
            var snapshot = new DemoSnapshot();

            demo.EntityEvents.CCSGameRulesProxy.AddChangeCallback(proxy => proxy.GameRules?.RoundStartCount,
                (_, _, _) =>
                {
                    var syntheticEvent = new Source1RoundStartEvent(demo);
                    demo.OnCommandFinish += () => LogEvent("RoundStartCount change", syntheticEvent);
                });

            demo.EntityEvents.CCSGameRulesProxy.AddChangeCallback(proxy => proxy.GameRules?.RoundEndCount,
                (proxy, _, _) =>
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

            return snapshot;

            void LogEvent<T>(string eventName, T evt)
            {
                var sb = new StringBuilder();

                sb.AppendLine($"{eventName}:");

                var eventJson = JsonSerializer.Serialize(evt, DemoJson.SerializerOptions)
                    .ReplaceLineEndings(Environment.NewLine + "  ");
                sb.AppendLine($"  {eventJson}");

                snapshot.Add(demo.CurrentDemoTick, sb.ToString());
            }
        }

        // Act
        var snapshot = await Parse(_mode, testCase.Value, ParseSection);

        // Assert
        Snapshot.Assert(snapshot);
    }
}
