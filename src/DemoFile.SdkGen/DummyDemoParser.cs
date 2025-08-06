using System.Diagnostics.CodeAnalysis;
using DemoFile.Sdk;

namespace DemoFile.SdkGen;

public class DummyDemoParser : DemoParser<DummyDemoParser>
{
    protected override IReadOnlyDictionary<string, EntityFactory<DummyDemoParser>>
        EntityFactories => new Dictionary<string, EntityFactory<DummyDemoParser>>();

    protected override ref EntityEvents<CEntityInstance<DummyDemoParser>, DummyDemoParser> EntityInstanceEvents => throw new NotImplementedException();

    protected override DecoderSet CreateDecoderSet(IReadOnlyDictionary<SerializerKey, Serializer> serializers) =>
        new DummyDecoderSet(serializers);

    protected override bool ParseNetMessage(int msgType, ReadOnlySpan<byte> msgBuf) => false;
}

public class DummyDecoderSet : DecoderSet
{
    public DummyDecoderSet(IReadOnlyDictionary<SerializerKey, Serializer> serializers) : base(serializers)
    {
    }

    public override bool TryGetDecoderByName(string className, [NotNullWhen(true)] out Type? classType, [NotNullWhen(true)] out SendNodeDecoder<object>? decoder)
    {
        throw new NotImplementedException();
    }

    public override bool TryCreateFallbackDecoder(
        SerializableField field,
        DecoderSet decoderSet,
        [NotNullWhen(true)] out SendNodeDecoder<object>? decoder)
    {
        throw new NotImplementedException();
    }

    protected override SendNodeDecoderFactory<T> GetFactory<T>()
    {
        throw new NotImplementedException();
    }
}
