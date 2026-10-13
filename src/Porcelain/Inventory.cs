using System.IO;

using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AsaSavegameToolkit.Porcelain;

/// <summary>
/// Represents an inventory component attached to a creature, player, structure, or death cache.
/// </summary>
/// <remarks>
/// Inventories are separate <see cref="GameObjectRecord"/> objects linked to their
/// owner via ObjectProperty. Each item in the inventory is also a separate record.
/// </remarks>
public class Inventory
{
    /// <summary>
    /// The underlying raw record for direct property access.
    /// </summary>
    public GameObjectRecord? Record { get; set; }

    /// <summary>
    /// Unique object ID of this inventory.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The blueprint class name (e.g., "PrimalInventoryBP_C").
    /// </summary>
    public required string ClassName { get; set; }

    public Dictionary<Guid, Item> Items { get; } = [];

    public override string ToString() => ClassName;

    public static Inventory Create(GameObjectRecord record, IDictionary<Guid, Item> inventoryItems)
    {
        return Create(record, inventoryItems, NullLogger.Instance);
    }

    public static Inventory Create(GameObjectRecord record, IDictionary<Guid, Item> inventoryItems, ILogger logger)
    {
        var inventory = new Inventory
        {
            Record = record,
            Id = record.Uuid,
            ClassName = record.ClassName.FullName
        };

        if (record.Properties.TryGet("InventoryItems", out ArrayProperty? itemsProp))
        {
            foreach (var value in itemsProp.Value)
            {
                if (value is not ObjectReference reference || reference.IsPath)
                    throw new InvalidDataException($"Inventory {record.Uuid} contains an unsupported item reference.");

                if (reference.ObjectId == Guid.Empty)
                    continue;

                if (inventoryItems.TryGetValue(reference.ObjectId, out var item))
                {
                    inventory.Items[item.Id] = item;
                }
                else
                {
                    logger.LogWarning("Inventory {InventoryId} references missing item {ItemId}. The inventory is incomplete.",
                        record.Uuid, reference.ObjectId);
                }
            }
        }

        return inventory;
    }

}
