namespace DemoFile;

public readonly record struct NetworkedString(string Value)
{
    public static implicit operator string(NetworkedString @this) => @this.Value;
}