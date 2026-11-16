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
        SpawnGroupHandle = context.SpawnGroupHandle;
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

    /// <summary>
    /// Handle of the spawn group this entity was created in, as sent when the entity was created.
    /// </summary>
    /// <remarks>
    /// A spawn group is a unit of loaded world content: in CS2, the map (normally handle 1), its skybox and
    /// the UI prefabs placed in the map (team select, team intro, end-of-match lineup). Entities that aren't
    /// part of any spawn group, such as players, pawns, teams and the world, have handle 0.
    /// Handles are assigned per demo, so look up what a handle refers to with the demo's spawn group messages
    /// (e.g. <see cref="PacketEvents.NetSpawnGroupLoad"/>) rather than relying on fixed numbers.
    /// </remarks>
    public uint SpawnGroupHandle { get; }

    internal void ReadField(ReadOnlySpan<int> fieldPath, ref BitBuffer buffer)
    {
        _decoder(this, fieldPath, ref buffer);
    }
}
