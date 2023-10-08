namespace DemoFile.Sdk;

internal delegate void SendNodeDecoder<in T>(
    T instance,
    ReadOnlySpan<int> fieldPath,
    ref BitBuffer buffer);

internal delegate SendNodeDecoder<T> SendNodeDecoderFactory<in T>(
    SerializableField field,
    DecoderSet decoderSet);