namespace DemoFile;

public abstract partial class Source1GameEventBase
{
    protected readonly DemoParser<CsDemoParser> _demo;

    protected Source1GameEventBase(DemoParser<CsDemoParser> demo)
    {
        _demo = demo;
    }

    public abstract string GameEventName { get; }
}

public partial class Source1GameEvents
{
    private readonly CsDemoParser _demo;

    // todo(net8): store as a FrozenDictionary
    private Dictionary<int, Action<CsDemoParser, CMsgSource1LegacyGameEvent>> _handlers = new();

    public Action<Source1GameEventBase>? Source1GameEvent;

    internal Source1GameEvents(CsDemoParser demo)
    {
        _demo = demo;
    }

    internal void ParseSource1GameEvent(CsDemoParser demo, CMsgSource1LegacyGameEvent @event)
    {
        if (_handlers.TryGetValue(@event.Eventid, out var handler))
        {
            handler(demo, @event);
        }
    }
}
