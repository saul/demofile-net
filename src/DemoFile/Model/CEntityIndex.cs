namespace DemoFile;

public readonly record struct CEntityIndex(uint Value)
{
    public static readonly CEntityIndex Invalid = new(unchecked((uint)-1));

    public override string ToString() => this == Invalid ? $"<invalid>" : $"Entity index {Value}";
}
