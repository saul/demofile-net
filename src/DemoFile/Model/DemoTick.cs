using System.Diagnostics;

namespace DemoFile;

/// <summary>
/// Demos start at <see cref="Zero"/> from the moment the demo starts recording.
/// Note some demo messages are recorded at DemoTick -1 (<see cref="PreRecord"/>),
/// a fake tick indicating that the messages occurred before the demo started recording.
/// These include messages like <see cref="CSVCMsg_ServerInfo"/>.
/// </summary>
/// <param name="Value">Current demo tick. <c>-1</c> or greater.</param>
/// <seealso cref="GameTick"/>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly record struct DemoTick(int Value) : IComparable<DemoTick>
{
    public static readonly DemoTick Zero = default;
    public static readonly DemoTick PreRecord = new(-1);

    public int CompareTo(DemoTick other) => Value.CompareTo(other.Value);

    public override string ToString() => this == PreRecord ? "<pre record>" : $"{Value}";

    private string DebuggerDisplay => this == PreRecord ? "<pre record>" : $"Demo tick {Value}";

    public static DemoTick operator +(DemoTick tick, TimeSpan duration) => new((int)(tick.Value + duration.TotalSeconds * 64.0));
    public static DemoTick operator -(DemoTick tick, TimeSpan duration) => new((int)(tick.Value - duration.TotalSeconds * 64.0));

    public static bool operator <(DemoTick left, DemoTick right) => left.Value < right.Value;
    public static bool operator <=(DemoTick left, DemoTick right) => left.Value <= right.Value;
    public static bool operator >(DemoTick left, DemoTick right) => left.Value > right.Value;
    public static bool operator >=(DemoTick left, DemoTick right) => left.Value >= right.Value;
}
