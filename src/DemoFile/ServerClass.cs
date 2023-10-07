using DemoFile.Sdk;

namespace DemoFile;

public record ServerClass(
    string Name,
    int ServerClassId,
    Func<EntityContext, CEntityInstance> EntityFactory)
{
    public override string ToString() => $"{Name} ({ServerClassId})";
}