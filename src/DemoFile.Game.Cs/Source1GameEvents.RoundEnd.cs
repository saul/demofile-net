using System.Diagnostics;
using DemoFile.Sdk;

namespace DemoFile.Game.Cs;

public partial class Source1GameEvents
{
    public Action<Source1RoundEndEvent>? _onRoundEnd;
    private EntityEventRegistration<CCSGameRulesProxy> _roundEndRegistration;

    public Action<Source1RoundEndEvent>? RoundEnd
    {
        get => _onRoundEnd;
        set
        {
            if (_onRoundEnd == null && value != null)
            {
                InitRoundEnd();
            }
            else if (_onRoundEnd != null && value == null)
            {
                DestroyRoundEnd();
            }

            _onRoundEnd = value;
        }
    }

    private void InitRoundEnd()
    {
        _roundEndRegistration = _demo.EntityEvents.CCSGameRulesProxy.AddChangeCallback(proxy => proxy.GameRules?.RoundEndCount, (proxy, _, _) =>
        {
            var gameRules = proxy.GameRules!;
            Debug.Assert(gameRules != null);

            var syntheticEvent = new Source1RoundEndEvent(_demo)
            {
                Legacy = gameRules.RoundEndLegacy,
                Message = gameRules.RoundEndMessage,
                Nomusic = gameRules.RoundEndNoMusic ? 1 : 0,
                Reason = gameRules.RoundEndReason,
                Winner = gameRules.RoundEndWinnerTeam,
                PlayerCount = gameRules.RoundEndPlayerCount,
            };

            // Entity updates happen mid-tick.
            // Wait until the end of the command to ensure player deaths have happened.
            _demo.OnCommandFinish += () =>
            {
                _onRoundEnd?.Invoke(syntheticEvent);
                Source1GameEvent?.Invoke(syntheticEvent);
            };
        });
    }

    private void DestroyRoundEnd()
    {
        _demo.EntityEvents.CCSGameRulesProxy.RemoveChangeCallback(_roundEndRegistration);
    }
}
