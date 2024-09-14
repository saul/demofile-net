using DemoFile.Sdk;

namespace DemoFile;

public partial class Source1GameEvents
{
    private Action<Source1RoundStartEvent>? _onRoundStart;
    private EntityEventRegistration<CCSGameRulesProxy> _roundStartRegistration;

    public Action<Source1RoundStartEvent>? RoundStart
    {
        get => _onRoundStart;
        set
        {
            if (_onRoundStart == null && value != null)
            {
                InitRoundStart();
            }
            else if (_onRoundStart != null && value == null)
            {
                DestroyRoundStart();
            }

            _onRoundStart = value;
        }
    }

    private void InitRoundStart()
    {
        _roundStartRegistration = _demo.EntityEvents.CCSGameRulesProxy.AddChangeCallback(proxy => proxy.GameRules?.RoundStartCount, (_, _, _) =>
        {
            var syntheticEvent = new Source1RoundStartEvent(_demo);

            // Entity updates happen mid-tick.
            // Wait until the end of the command to ensure player deaths have happened.
            _demo.OnCommandFinish += () =>
            {
                _onRoundStart?.Invoke(syntheticEvent);
                Source1GameEvent?.Invoke(syntheticEvent);
            };
        });
    }

    private void DestroyRoundStart()
    {
        _demo.EntityEvents.CCSGameRulesProxy.RemoveChangeCallback(_roundStartRegistration);
    }
}
