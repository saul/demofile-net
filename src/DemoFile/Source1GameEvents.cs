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

public partial class Source1GameEvents
{
    // todo(net8): store as a FrozenDictionary
    private Dictionary<int, Action<CMsgSource1LegacyGameEvent>> _handlers = new();

    internal void ParseSource1GameEvent(CMsgSource1LegacyGameEvent @event)
    {
        if (_handlers.TryGetValue(@event.Eventid, out var handler))
        {
            handler(@event);
        }
    }
}
