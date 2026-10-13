using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

namespace AsaSavegameToolkit.Porcelain;

/// <summary>
/// Represents an inventory component attached to a creature, player, or structure.
/// </summary>
/// <remarks>
/// Inventories are separate <see cref="GameObjectRecord"/> objects linked to their
/// owner via ObjectProperty. Each item in the inventory is also a separate record.
/// </remarks>
public class Inventory
{
    public List<Item> Items { get; set; } = new List<Item>();

    public override string ToString() => $"Inventory: {Items.Count} items";

    public static Inventory Create(List<Item> items)
    {
        return new Inventory
        {
            Items = items
        };
    }
}
