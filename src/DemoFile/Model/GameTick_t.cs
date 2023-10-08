namespace DemoFile;

public readonly record struct GameTick_t(uint Value) : IComparable<GameTick_t>
{
    // CS2 is hardcoded as 64-tick
    public GameTime_t ToGameTime() => new(Value / 64.0f);

    public int CompareTo(GameTick_t other) => Value.CompareTo(other.Value);

    public override string ToString() => $"Tick {Value} ({ToGameTime()})";
}