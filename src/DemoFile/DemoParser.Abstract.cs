using DemoFile.Sdk;

namespace DemoFile;

public partial class DemoParser<TGameParser>
{
    protected abstract IReadOnlyDictionary<string, EntityFactory<TGameParser>> EntityFactories { get; }

    protected internal abstract ref EntityEvents<CEntityInstance<TGameParser>, TGameParser> EntityInstanceEvents { get; }

    protected abstract DecoderSet CreateDecoderSet(IReadOnlyDictionary<SerializerKey, Serializer> serializers);

    protected abstract bool ParseNetMessage(int msgType, ReadOnlySpan<byte> msgBuf);
}
