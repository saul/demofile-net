namespace DemoFile.Sdk;

public delegate void SendNodeDecoder<in T>(
    T instance,
    ReadOnlySpan<int> fieldPath,
    ref BitBuffer buffer);

public delegate SendNodeDecoder<T> SendNodeDecoderFactory<in T>(
    SerializableField field,
    DecoderSet decoderSet);
