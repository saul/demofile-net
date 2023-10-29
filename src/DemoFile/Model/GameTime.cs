namespace DemoFile;

public readonly record struct GameTime(float Value) : IComparable<GameTime>
{
    public TimeSpan ToTimeSpan() => TimeSpan.FromSeconds(Value);

    public int CompareTo(GameTime other) => Value.CompareTo(other.Value);

    public override string ToString() => ToTimeSpan().ToString();

    public static GameTime operator +(GameTime time, TimeSpan duration) => new(time.Value + (float)duration.TotalSeconds);
    public static GameTime operator -(GameTime time, TimeSpan duration) => new(time.Value - (float)duration.TotalSeconds);
}
