using DemoFile.GameStatic;

namespace DemoFile.Game.Cs;

public partial class CEconItemAttribute
{
    public string Name =>
        GameItems.Attributes.GetValueOrDefault(AttributeDefinitionIndex)?.Name
        ?? $"{AttributeDefinitionIndex}";

    public override string ToString() => $"{Name} = {Value}";
}
