namespace DemoFile;

public readonly record struct GameTime_t(float Value) : IComparable<GameTime_t>
{
    public TimeSpan ToTimeSpan() => TimeSpan.FromSeconds(Value);

    public int CompareTo(GameTime_t other) => Value.CompareTo(other.Value);

    public override string ToString() => ToTimeSpan().ToString();
}