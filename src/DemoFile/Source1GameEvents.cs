namespace DemoFile;

public enum GameEventKeyType
{
    String = 1,
    Float = 2,
    Long = 3,
    Short = 4,
    Byte = 5,
    Bool = 6,
    UInt64 = 7,
    StrictEHandle = 8,
    PlayerController = 9,
}

public abstract partial class Source1GameEventBase
{
    protected readonly DemoParser _demo;

    protected Source1GameEventBase(DemoParser demo)
    {
        _demo = demo;
    }

    public abstract string GameEventName { get; }
}

public partial class Source1GameEvents
{
    public Action<Source1GameEventBase>? Source1GameEvent;

    // todo(net8): store as a FrozenDictionary
    private Dictionary<int, Action<DemoParser, CMsgSource1LegacyGameEvent>> _handlers = new();

    internal void ParseSource1GameEvent(DemoParser demo, CMsgSource1LegacyGameEvent @event)
    {
        if (_handlers.TryGetValue(@event.Eventid, out var handler))
        {
            handler(demo, @event);
        }
    }
}
