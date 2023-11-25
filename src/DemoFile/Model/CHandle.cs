using System.Diagnostics;
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

    public static CHandle<T> FromEventStrictEHandle(uint value)
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

        var index = value & (DemoParser.MaxEdicts - 1);
        var serialNum = (value >> 15) & ((1 << 10) - 1);

        return FromIndexSerialNum(new CEntityIndex(index), serialNum);
    }

    public T? Get(DemoParser demo) => demo.GetEntityByHandle(this);

    public TEntity? Get<TEntity>(DemoParser demo) where TEntity : T => demo.GetEntityByHandle(this) as TEntity;
}