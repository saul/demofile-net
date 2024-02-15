using DemoFile.Sdk;

namespace DemoFile;

public partial class DemoParser
{
    public Action<Source1RoundStartEvent>? _onRoundStart;
    private EntityEventRegistration<CCSGameRulesProxy> _roundStartRegistration;

    public Action<Source1RoundStartEvent>? OnRoundStart
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
        _roundStartRegistration = EntityEvents.CCSGameRulesProxy.AddChangeCallback(proxy => proxy.GameRules?.RoundStartCount, (_, _, _) =>
        {
            var syntheticEvent = new Source1RoundStartEvent(this);

            // Entity updates happen mid-tick.
            // Wait until the end of the command to ensure player deaths have happened.
            OnCommandFinish += () =>
            {
                _onRoundStart?.Invoke(syntheticEvent);
            };
        });

        Source1GameEvents.RoundStart += OnRoundStartEvent;
    }

    private void DestroyRoundStart()
    {
        EntityEvents.CCSGameRulesProxy.RemoveChangeCallback(_roundStartRegistration);
        Source1GameEvents.RoundStart -= OnRoundStartEvent;
    }

    private void OnRoundStartEvent(Source1RoundStartEvent e) => _onRoundStart?.Invoke(e);
}