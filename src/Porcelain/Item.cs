using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

namespace AsaSavegameToolkit.Porcelain;

/// <summary>
/// Represents an individual item in an ARK save file (weapon, resource, consumable, etc.).
/// </summary>
public class Item
{
    /// <summary>
    /// The underlying raw record for direct property access.
    /// </summary>
    public GameObjectRecord? Record { get; set; }

    /// <summary>
    /// Unique object ID of this item.
    /// </summary>
    public Guid Id { get; set; }

    public long ItemId { get; set; }

    /// <summary>
    /// The blueprint class name (e.g., "PrimalItem_WeaponSword_C").
    /// </summary>
    public required string ClassName { get; set; }

    /// <summary>
    /// The stack quantity of this item.
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// True if this item is a blueprint (not a craftable/usable copy).
    /// </summary>
    public bool IsBlueprint { get; set; }

    /// <summary>
    /// True when this item is a <see cref="CraftedItem"/>.
    /// </summary>
    public bool IsCrafted => this is CraftedItem;

    public bool IsEngram { get; set; }

    public bool IsRecipe { get; set; }

    public bool IsInitialItem { get; set; }

    /// <summary>
    /// Remaining durability as a fraction from 0.0 to 1.0.
    /// </summary>
    public float? Durability { get; set; }

    /// <summary>
    /// Item quality from 0.0 (primitive) to higher values (ascendant).
    /// </summary>
    public float? ItemRating { get; set; }

    public byte[] BaseStats { get; set; } = [];

    public int[] Colors { get; set; } = [];

    public string? CustomItemName { get; set; }

    public string? CustomItemDescription { get; set; }

    /// <summary>
    /// World-space position, or null for items without an actor transform.
    /// </summary>
    public FVector? Location { get; set; }
    public FQuat? Rotation { get; set; }

    internal static Item Create(GameObjectRecord record, ActorTransform? location = default)
    {
        var properties = record.Properties;
        var className = record.GetClassName();

        if (properties == null || properties.Count == 0)
        {
            return new Item
            {
                Record = record,
                Id = record.Uuid,
                ClassName = className,
                Quantity = 1,
                Location = location?.Location,
                Rotation = location?.Rotation
            };
        }

        if (properties.HasAny("EggNumberOfLevelUpPointsApplied"))
        {
            var eggLevel = 0;
            for (var i = 0; i < 12; i++)
            {
                eggLevel += properties.Get<byte>("EggNumberOfLevelUpPointsApplied", i);
            }

            if (eggLevel > 0)
            {
                return FertileEggItem.Create(record, location);
            }
        }

        var crafterTribeName = properties.Get<string>("CrafterTribeName") ?? "";
        var crafterCharacterName = properties.Get<string>("CrafterCharacterName") ?? "";
        if (crafterTribeName.Length > 0 || crafterCharacterName.Length > 0)
        {
            return CraftedItem.Create(record, location);
        }

        var item = new Item { ClassName = className };
        item.PopulateFromRecord(record, location);
        return item;
    }

    protected void PopulateFromRecord(GameObjectRecord record, ActorTransform? location)
    {
        var properties = record.Properties;
        uint itemId1 = 0;
        uint itemId2 = 0;
        var itemIdProperties = (List<Property>?)properties.Get<StructProperty>("ItemID")?.Value;
        if (itemIdProperties != null)
        {
            itemId1 = itemIdProperties.Get<uint>("ItemID1");
            itemId2 = itemIdProperties.Get<uint>("ItemID2");
        }
        var itemId = ((long)itemId1 << 32) | (itemId2 & 0xFFFFFFFFL);

        var itemQuantity = properties.Get<int>("ItemQuantity");
        if (itemQuantity == 0)
            itemQuantity = 1;

        var isBlueprint = properties.Get<bool>("bIsBlueprint");
        var isEngram = properties.Get<bool>("bIsEngram");
        var isCustomRecipe = properties.Get<bool>("bIsCustomRecipe");
        var isFoodRecipe = properties.Get<bool>("bIsFoodRecipe");
        var isInitialItem = properties.Get<bool>("bIsInitialItem");

        var savedDurability = properties.TryGet<float>("SavedDurability", out var dur) ? dur : (float?)null;
        var itemRating = properties.TryGet<float>("ItemRating", out var rating) ? rating : (float?)null;

        var baseStatValues = new byte[8];
        for (var i = 0; i < baseStatValues.Length; i++)
        {
            baseStatValues[i] = (byte)properties.Get<uint>("ItemStatValues", i);
        }

        var itemColors = new int[6];
        if (properties.HasAny("ItemColorID"))
        {
            for (var i = 0; i < itemColors.Length; i++)
            {
                itemColors[i] = properties.Get<short>("ItemColorID", i);
            }
        }

        var customItemName = properties.Get<string>("CustomItemName");
        var customItemDescription = properties.Get<string>("CustomItemDescription");

        Record = record;
        Id = record.Uuid;
        ClassName = record.GetClassName();
        ItemId = itemId;
        Quantity = itemQuantity;
        IsBlueprint = isBlueprint;
        IsEngram = isEngram;
        IsRecipe = isCustomRecipe || isFoodRecipe;
        IsInitialItem = isInitialItem;
        Durability = savedDurability;
        ItemRating = itemRating;
        BaseStats = baseStatValues;
        Colors = itemColors;
        CustomItemName = customItemName;
        CustomItemDescription = customItemDescription;
        Location = location?.Location;
        Rotation = location?.Rotation;
    }

    internal static Item FromCryoSaddle(GameObjectRecord record)
    {
        var className = record.Properties.TryGet<ObjectReference>("ItemArchetype", out var archetype) ? archetype.Value : "Unknown Saddle";

        return new Item
        {
            Record = record,
            Id = record.Uuid == default ? Guid.NewGuid() : record.Uuid,
            ClassName = className,
            Quantity = 1,
            IsBlueprint = false,
            Durability = record.Properties.TryGet<float>("ItemDurability", out var dur) ? dur : null,
            ItemRating = record.Properties.TryGet<float>("ItemRating", out var rating) ? rating : null
        };
    }

    // TODO: expose crafting stats, custom name once property interpretation (2.2) is implemented.

    public override string ToString()
    {
        var bp = IsBlueprint ? " (BP)" : "";
        var qty = Quantity > 1 ? $" x{Quantity}" : "";
        return $"{ClassName}{bp}{qty}";
    }
}
