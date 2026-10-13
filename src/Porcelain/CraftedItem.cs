using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

namespace AsaSavegameToolkit.Porcelain;

public class CraftedItem : Item
{
    public string CrafterCharacterName { get; set; } = string.Empty;
    public string CrafterTribeName { get; set; } = string.Empty;
    public float CraftingSkill { get; set; }
    public float CraftingSkillBonus { get; set; }

    internal static new CraftedItem Create(GameObjectRecord record, ActorTransform? location = default)
    {
        var properties = record.Properties;
        var item = new CraftedItem
        {
            ClassName = record.GetClassName(),
        };
        item.PopulateFromRecord(record, location);

        item.CrafterCharacterName = properties.Get<string>("CrafterCharacterName") ?? "";
        item.CrafterTribeName = properties.Get<string>("CrafterTribeName") ?? "";
        item.CraftingSkill = properties.Get<float>("CraftingSkill");
        item.CraftingSkillBonus = properties.Get<float>("CraftedSkillBonus");

        // Preserve the crafted-item defaults for properties absent from the record.
        item.Durability ??= 0;
        item.ItemRating ??= 0;
        item.CustomItemName ??= "";
        item.CustomItemDescription ??= "";

        return item;
    }
}
