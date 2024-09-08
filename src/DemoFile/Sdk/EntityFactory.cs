namespace DemoFile.Sdk;

public delegate CEntityInstance<TGameParser> EntityFactory<TGameParser>(
    DemoParser<TGameParser>.EntityContext context,
    SendNodeDecoder<object> decoder)
    where TGameParser : DemoParser<TGameParser>, new();
