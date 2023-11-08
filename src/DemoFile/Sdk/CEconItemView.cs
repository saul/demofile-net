using DemoFile.GameStatic;

namespace DemoFile.Sdk;

public partial class CEconItemView
{
    private static readonly int PaintKitAttributeDefIndex = GameItems.Attributes.First(kvp => kvp.Value.Name == "set item texture prefab").Key;

    public string Name =>
        GameItems.ItemDefinitions.GetValueOrDefault(ItemDefinitionIndex)?.Name
        ?? $"{ItemDefinitionIndex}";

    public ItemQuality Quality => (ItemQuality)EntityQuality;
    public ItemRarity Rarity => (ItemRarity)EntityLevel;

    public PaintKit? PaintKit
    {
        get
        {
            var attribute = NetworkedDynamicAttributes.Attributes.FirstOrDefault(
                    attr => attr!.AttributeDefinitionIndex == PaintKitAttributeDefIndex);

            return attribute == null
                ? null
                : GameItems.PaintKits.GetValueOrDefault((int) attribute.Value);
        }
    }

    public IReadOnlyDictionary<string, CEconItemAttribute> Attributes =>
        NetworkedDynamicAttributes.Attributes.ToDictionary(attr =>
            GameItems.Attributes.GetValueOrDefault(attr!.AttributeDefinitionIndex)?.Name
            ?? $"{attr.AttributeDefinitionIndex}")!;

    public override string ToString() => $"{Name} {{ CustomName = {CustomName}, PaintKit = {PaintKit?.Name}, Quality = {Quality}, Rarity = {Rarity} }}";
}
