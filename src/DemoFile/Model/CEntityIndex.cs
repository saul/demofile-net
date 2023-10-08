namespace DemoFile;

public readonly record struct CEntityIndex(uint Value)
{
    public override string ToString() => $"Entity index {Value}";
}