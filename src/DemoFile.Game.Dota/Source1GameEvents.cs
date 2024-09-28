namespace DemoFile.Game.Dota;

public abstract partial class Source1GameEventBase
{
    protected readonly DotaDemoParser _demo;

    protected Source1GameEventBase(DemoParser<DotaDemoParser> demo)
    {
        _demo = (DotaDemoParser)demo;
    }

    public abstract string GameEventName { get; }
}

public partial class Source1GameEvents
{
    private readonly DotaDemoParser _demo;

    // todo(net8): store as a FrozenDictionary
    private Dictionary<int, Action<DotaDemoParser, CMsgSource1LegacyGameEvent>> _handlers = new();

    public Action<Source1GameEventBase>? Source1GameEvent;

    internal Source1GameEvents(DotaDemoParser demo)
    {
        _demo = demo;
    }

    internal void ParseSource1GameEvent(DotaDemoParser demo, CMsgSource1LegacyGameEvent @event)
    {
        if (_handlers.TryGetValue(@event.Eventid, out var handler))
        {
            handler(demo, @event);
        }
    }
}
