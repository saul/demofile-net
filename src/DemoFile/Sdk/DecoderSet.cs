namespace DemoFile.Sdk;

internal partial class DecoderSet
{
    private readonly IReadOnlyDictionary<SerializerKey, Serializer> _serializers;
    private readonly Dictionary<SerializerKey, object?> _decoders = new(512);

    public DecoderSet(IReadOnlyDictionary<SerializerKey, Serializer> serializers)
    {
        _serializers = serializers;
    }

    public SendNodeDecoder<object> GetDecoder(string className)
    {
        if (!TryGetDecoder(className, out _, out var decoder))
            throw new NotImplementedException($"Unknown send node class: {className}");

        return decoder;
    }

    public SendNodeDecoder<T> GetDecoder<T>(SerializerKey serializerKey)
    {
        if (!_decoders.TryGetValue(serializerKey, out var decoder))
        {
            // null sentinel so that we're aware of cycles when calling GetDecoder
            _decoders[serializerKey] = null;

            decoder = CreateDecoder<T>(serializerKey);
            _decoders[serializerKey] = decoder;
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

        var decoderFactory = SendNodeDecoders.GetFactory<T>();
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
