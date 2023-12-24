using System.Diagnostics;

namespace DemoFile;

/// <summary>
/// Ticks represent the smallest simulation step of the game.
/// CS2 is hardcoded to 64 ticks per second (tickrate).
/// </summary>
/// <param name="Value">Tick number.</param>
/// <seealso cref="DemoTick"/>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly record struct GameTick(uint Value) : IComparable<GameTick>
{
    // CS2 is hardcoded as 64-tick
    public GameTime ToGameTime() => new(Value / 64.0f);

    public int CompareTo(GameTick other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString();

    private string DebuggerDisplay => $"Tick {Value} ({ToGameTime()})";

    public static GameTick operator +(GameTick tick, TimeSpan duration) => new((uint)(tick.Value + duration.TotalSeconds / 64.0));
    public static GameTick operator -(GameTick tick, TimeSpan duration) => new((uint)(tick.Value - duration.TotalSeconds / 64.0));

    public static bool operator <(GameTick left, GameTick right) => left.Value < right.Value;
    public static bool operator <=(GameTick left, GameTick right) => left.Value <= right.Value;
    public static bool operator >(GameTick left, GameTick right) => left.Value > right.Value;
    public static bool operator >=(GameTick left, GameTick right) => left.Value >= right.Value;
}
