using DemoFile.Sdk;

namespace DemoFile;

public record ServerClass<TGameParser>(
    string Name,
    int ServerClassId,
    Func<DemoParser<TGameParser>.EntityContext, CEntityInstance<TGameParser>> EntityFactory)
    where TGameParser : DemoParser<TGameParser>, new()
{
    public override string ToString() => $"{Name} ({ServerClassId})";
}
