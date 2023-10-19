namespace DemoFile;

/// <summary>
/// Demo ticks start at 0 from the moment the demo starts recording.
/// Some demo messages are recorded at DemoTick -1, a fake tick indicating
/// that the messages occurred before the demo started recording.
/// These include messages like <see cref="CSVCMsg_ServerInfo"/>.
/// </summary>
/// <param name="Value">Current demo tick. <c>-1</c> or greater.</param>
/// <seealso cref="GameTick_t"/>
public readonly record struct DemoTick(int Value) : IComparable<DemoTick>
{
    public int CompareTo(DemoTick other) => Value.CompareTo(other.Value);

    public override string ToString() => Value == -1 ? "<pre record>" : $"Demo tick {Value}";
}
