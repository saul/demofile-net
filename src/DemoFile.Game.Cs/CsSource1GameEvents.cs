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

public abstract partial class CsSource1GameEventBase
{
    protected readonly DemoParser<CsDemoParser> _demo;

    protected CsSource1GameEventBase(DemoParser<CsDemoParser> demo)
    {
        _demo = demo;
    }

    public abstract string GameEventName { get; }
}

public partial class CsSource1GameEvents
{
    private readonly CsDemoParser _demo;

    // todo(net8): store as a FrozenDictionary
    private Dictionary<int, Action<CsDemoParser, CMsgSource1LegacyGameEvent>> _handlers = new();

    public Action<CsSource1GameEventBase>? Source1GameEvent;

    internal CsSource1GameEvents(CsDemoParser demo)
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
