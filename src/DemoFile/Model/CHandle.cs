using DemoFile.Sdk;

namespace DemoFile;

public readonly record struct CHandle<T>(ulong Value)
    where T : CEntityInstance
{
    public override string ToString() => IsValid ? $"Index = {Index.Value}, Serial = {SerialNum}" : "<invalid>";

    public bool IsValid => this != default && Index.Value != (DemoParser.MaxEdicts - 1);

    public CEntityIndex Index => new((uint) (Value & (DemoParser.MaxEdicts - 1)));
    public uint SerialNum => (uint)(Value >> DemoParser.MaxEdictBits);

    public static CHandle<T> FromIndexSerialNum(CEntityIndex index, uint serialNum) =>
        new(((ulong)index.Value) | (serialNum << DemoParser.MaxEdictBits));

    public void Deconstruct(out ulong Value)
    {
        Value = this.Value;
    }
}