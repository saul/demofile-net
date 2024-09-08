using System.Diagnostics;
using DemoFile.Sdk;

namespace DemoFile;

public readonly record struct CHandle<T, TGameParser>(ulong Value)
    where T : CEntityInstance<TGameParser> where TGameParser : DemoParser<TGameParser>, new()
{
    public override string ToString() => IsValid ? $"Index = {Index.Value}, Serial = {SerialNum}" : "<invalid>";

    public bool IsValid => this != default && Index.Value != (DemoParser<TGameParser>.MaxEdicts - 1);

    public CEntityIndex Index => new((uint) (Value & (DemoParser<TGameParser>.MaxEdicts - 1)));
    public uint SerialNum => (uint)(Value >> DemoParser<TGameParser>.MaxEdictBits);

    public static CHandle<T, TGameParser> FromIndexSerialNum(CEntityIndex index, uint serialNum) =>
        new(((ulong)index.Value) | (serialNum << DemoParser<TGameParser>.MaxEdictBits));

    public static CHandle<T, TGameParser> FromEventStrictEHandle(uint value)
    {
        // EHandles in events are serialised differently than networked handles.
        //
        // Empirically the bit structure appears to be:
        //   1100100 0011110000 0 00001011101011
        //   ^^^^^^^ ^^^^^^^^^^ ^ ^^^^^^^^^^^^^^
        //   |       |          | \__ ent index
        //   |       |          \__ always zero?
        //   |       \__ serial number
        //   \__ unknown, varies

        Debug.Assert(value == uint.MaxValue || (value & (1 << 14)) == 0);

        var index = value & (DemoParser<TGameParser>.MaxEdicts - 1);
        var serialNum = (value >> 15) & ((1 << 10) - 1);

        return FromIndexSerialNum(new CEntityIndex(index), serialNum);
    }

    public T? Get(TGameParser demo) => demo.GetEntityByHandle(this);

    public TEntity? Get<TEntity>(TGameParser demo) where TEntity : T => demo.GetEntityByHandle(this) as TEntity;
}
