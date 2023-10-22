namespace DemoFile;

/// <summary>
/// Ticks represent the smallest simulation step of the game.
/// CS2 is hardcoded to 64 ticks per second (tickrate).
/// </summary>
/// <param name="Value">Tick number.</param>
/// <seealso cref="DemoTick"/>
public readonly record struct GameTick(uint Value) : IComparable<GameTick>
{
    // CS2 is hardcoded as 64-tick
    public GameTime ToGameTime() => new(Value / 64.0f);

    public int CompareTo(GameTick other) => Value.CompareTo(other.Value);

    public override string ToString() => $"Tick {Value} ({ToGameTime()})";
}
