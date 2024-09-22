namespace DemoFile.Game.Deadlock;

public abstract partial class Source1GameEventBase
{
    protected readonly DemoParser<DeadlockDemoParser> _demo;

    protected Source1GameEventBase(DemoParser<DeadlockDemoParser> demo)
    {
        _demo = demo;
    }

    public abstract string GameEventName { get; }
}

public partial class Source1GameEvents
{
    private readonly DeadlockDemoParser _demo;

    // todo(net8): store as a FrozenDictionary
    private Dictionary<int, Action<DeadlockDemoParser, CMsgSource1LegacyGameEvent>> _handlers = new();

    public Action<Source1GameEventBase>? Source1GameEvent;

    internal Source1GameEvents(DeadlockDemoParser demo)
    {
        _demo = demo;
    }

    internal void ParseSource1GameEvent(DeadlockDemoParser demo, CMsgSource1LegacyGameEvent @event)
    {
        if (_handlers.TryGetValue(@event.Eventid, out var handler))
        {
            handler(demo, @event);
        }
    }
}
