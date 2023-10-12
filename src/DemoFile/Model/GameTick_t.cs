namespace DemoFile;

/// <summary>
/// Ticks represent the smallest simulation step of the game.
/// CS2 is hardcoded to 64 ticks per second (tickrate).
/// </summary>
/// <param name="Value">Tick number.</param>
/// <seealso cref="DemoTick"/>
public readonly record struct GameTick_t(uint Value) : IComparable<GameTick_t>
{
    // CS2 is hardcoded as 64-tick
    public GameTime_t ToGameTime() => new(Value / 64.0f);

    public int CompareTo(GameTick_t other) => Value.CompareTo(other.Value);

    public override string ToString() => $"Tick {Value} ({ToGameTime()})";
}
