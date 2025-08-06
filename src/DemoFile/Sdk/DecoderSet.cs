using System.Diagnostics.CodeAnalysis;

namespace DemoFile.Sdk;

public abstract class DecoderSet
{
    private readonly Dictionary<SerializerKey, object?> _cache = new(512);
    private readonly IReadOnlyDictionary<SerializerKey, Serializer> _serializers;

    protected DecoderSet(IReadOnlyDictionary<SerializerKey, Serializer> serializers)
    {
        _serializers = serializers;
    }

    public abstract bool TryGetDecoderByName(
        string className,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor), NotNullWhen(true)] out Type? classType,
        [NotNullWhen(true)] out SendNodeDecoder<object>? decoder);

    public abstract bool TryCreateFallbackDecoder(
        SerializableField field,
        DecoderSet decoderSet,
        [NotNullWhen(true)] out SendNodeDecoder<object>? decoder);

    protected abstract SendNodeDecoderFactory<T> GetFactory<T>();

    public SendNodeDecoder<object> GetDecoder(string className)
    {
        if (!TryGetDecoderByName(className, out _, out var decoder))
            throw new NotImplementedException($"Unknown send node class: {className}");

        return decoder;
    }

    public SendNodeDecoder<T> GetDecoder<T>(SerializerKey serializerKey)
    {
        if (!_cache.TryGetValue(serializerKey, out var decoder))
        {
            // null sentinel so that we're aware of cycles when calling GetDecoder
            _cache[serializerKey] = null;

            decoder = CreateDecoder<T>(serializerKey);
            _cache[serializerKey] = decoder;
        }

        if (decoder == null)
        {
            throw new Exception("Cycle detected!");
        }

        return (SendNodeDecoder<T>) decoder;
    }

    private SendNodeDecoder<T> CreateDecoder<T>(SerializerKey serializerKey)
    {
        if (!_serializers.TryGetValue(serializerKey, out var serializer))
        {
            return (T instance, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
            {
                throw new Exception($"Cannot deserialise field on {typeof(T).Name} (missing serializer)");
            };
        }

        var decoderFactory = GetFactory<T>();
        var fieldDecodersByIndex = serializer.Fields.Select(field => decoderFactory(field, this)).ToArray();

        return (T instance, ReadOnlySpan<int> path, ref BitBuffer buffer) =>
        {
#if DEBUG
            var _serializer = serializer;
#endif

            var fieldDecoder = fieldDecodersByIndex[path[0]];
            fieldDecoder(instance, path, ref buffer);
        };
    }
}
