using DemoFile.Sdk;

namespace DemoFile;

public partial class DemoParser<TGameParser>
{
    protected abstract IReadOnlyDictionary<string, EntityFactory<TGameParser>> EntityFactories { get; }

    protected internal abstract ref EntityEvents<CEntityInstance<TGameParser>, TGameParser> EntityInstanceEvents { get; }

    protected abstract DecoderSet CreateDecoderSet(IReadOnlyDictionary<SerializerKey, Serializer> serializers);

    protected abstract bool ParseNetMessage(int msgType, ReadOnlySpan<byte> msgBuf);

    /// <summary>
    /// Called after parser state has been restored from a full packet (when seeking, or when reading a range
    /// of the demo in parallel). Any state that depends on the commands in between is no longer valid.
    /// </summary>
    protected internal virtual void OnFullPacketRestored()
    {
    }
}
