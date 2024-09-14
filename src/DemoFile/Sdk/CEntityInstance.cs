namespace DemoFile.Sdk;

public partial class CEntityInstance<TGameParser>
    where TGameParser : DemoParser<TGameParser>, new()
{
    private readonly SendNodeDecoder<object> _decoder;
    protected readonly TGameParser Demo;

    public CEntityInstance(DemoParser<TGameParser>.EntityContext context, SendNodeDecoder<object> decoder)
    {
        _decoder = decoder;
        Demo = context.Demo;
        EntityIndex = context.EntityIndex;
        ServerClass = context.ServerClass;
        SerialNumber = context.SerialNumber;
    }

    public CEntityIndex EntityIndex { get; }

    public CHandle<CEntityInstance<TGameParser>, TGameParser> EntityHandle => CHandle<CEntityInstance<TGameParser>, TGameParser>.FromIndexSerialNum(EntityIndex, SerialNumber);

    /// <summary>
    /// Is this entity within the recording player's PVS?
    /// For GOTV demos, this is always <c>true</c>
    /// </summary>
    public bool IsActive { get; internal set; }

    public ServerClass<TGameParser> ServerClass { get; }
    public uint SerialNumber { get; }

    internal void ReadField(ReadOnlySpan<int> fieldPath, ref BitBuffer buffer)
    {
        _decoder(this, fieldPath, ref buffer);
    }
}
