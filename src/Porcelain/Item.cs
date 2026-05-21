using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using System.Runtime.InteropServices;

namespace AsaSavegameToolkit.Porcelain;

/// <summary>
/// Represents an individual item in an ARK save file (weapon, resource, consumable, etc.).
/// </summary>
public class Item
{
    
    /// <summary>
    /// Unique object ID of this item.
    /// </summary>
    public Guid Id { get; set; }

    public long ItemId { get; set; } = 0;

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
    public bool IsBlueprint { get; set; } = false;
    public bool IsEngram { get; set; } = false;
    public bool IsRecipe { get; set; } = false;
    public bool IsInitialItem { get; set; } = false;
    /// <summary>
    /// Remaining durability as a fraction from 0.0 to 1.0.
    /// </summary>
    public float Durability { get; set; } = 0;

    /// <summary>
    /// Item quality from 0.0 (primitive) to higher values (ascendant).
    /// </summary>
    public float ItemRating { get; set; } = 0;

    public byte[] BaseStats { get; set; } = [];
    public int[] Colors { get; set; } = [];


    internal static Item Create(GameObjectRecord record)
    {
        var properties = record.Properties;

        var className = record.GetClassName();

        if (properties == null || properties.Count == 0)
        {
            return new Item

            {
                Id = record.Uuid,
                ClassName = className,
                Quantity = 1,
                IsBlueprint = false,
                Durability = 0,
                ItemRating = 0
            };
        }

        if (properties.HasAny("EggNumberOfLevelUpPointsApplied"))
        {
            int eggLevel = 0;
            for (int i = 0; i < 12; i++)
            {
                eggLevel+=properties.Get<byte>("EggNumberOfLevelUpPointsApplied",i);
            }

            if(eggLevel>0)
                return FertileEggItem.Create(record);
        }

        //crafted items
        var crafterTribeName = properties.Get<string>("CrafterTribeName") ?? "";
        var crafterCharacterName = properties.Get<string>("CrafterCharacterName") ?? "";
        if (crafterTribeName.Length > 0 || crafterCharacterName.Length > 0)
            return CraftedItem.Create(record);


        //all other items
        uint itemId1 = 0;
        uint itemId2 = 0; 
        int itemQuantity = 1;
        bool isBlueprint = false;
        bool isEngram = false;
        bool isInitialItem = false;
        bool isCustomRecipe = false;
        bool isFoodRecipe = false;
        bool allowRemovalFromInventory = false;
        float savedDurability = 0;        
        float itemRating = 0;
        byte itemQualityIndex = 0;
        string customItemName = string.Empty;
        string customItemDescription = string.Empty;
        int[] itemColors = new int[6];

        
        // Start of basic item data
        var itemIdProperties = (List<Property>?)properties.Get<StructProperty>("ItemID")?.Value;
        if (itemIdProperties != null)
        {
            itemId1 = itemIdProperties.Get<uint>("ItemID1");
            itemId2 = itemIdProperties.Get<uint>("ItemID2");
        }
        var itemId = (itemId1 << 32) | itemId2;


        itemQuantity = properties.Get<int>("ItemQuantity");
        if (itemQuantity == 0)
            itemQuantity = 1;


        isBlueprint = properties.Get<bool>("bIsBlueprint");
        isEngram = properties.Get<bool>("bIsEngram");
        isCustomRecipe = properties.Get<bool>("bIsCustomRecipe");
        isFoodRecipe = properties.Get<bool>("bIsFoodRecipe");
        isInitialItem = properties.Get<bool>("bIsInitialItem");
        allowRemovalFromInventory = properties.Get<bool>("bAllowRemovalFromInventory");
        // End of basic item data


        //saddles/structures/armor/tools/weapons etc.
        savedDurability = properties.Get<float>("SavedDurability"); 
        itemRating = properties.Get<float>("ItemRating");       
        itemQualityIndex = properties.Get<byte>("ItemQualityIndex");


        //ItemStatValues //uint16
        byte[] baseStatValues = new byte[8];
        for (int i = 0; i<baseStatValues.Length; i++)
        {
            baseStatValues[i] =  (byte)properties.Get<uint>("ItemStatValues",i);
        }


        //ItemColorID //int
        if (properties.HasAny("ItemColorID"))
        {
            for (int i = 0; i < itemColors.Length; i++)
            {
                itemColors[i] = properties.Get<short>("ItemColorID", i);
            }
        }


        return new Item
        {
            Id = record.Uuid,
            ClassName = className,
            ItemId = itemId,
            Quantity = itemQuantity,
            IsBlueprint = isBlueprint,
            IsEngram = isEngram,
            IsRecipe = isCustomRecipe || isFoodRecipe,
            IsInitialItem = isInitialItem,            
            Durability = savedDurability,
            ItemRating = itemRating,
            BaseStats = baseStatValues,
            Colors = itemColors
        };
    }

    public override string ToString()
    {
        var bp = IsBlueprint ? " (BP)" : "";
        var qty = Quantity > 1 ? $" x{Quantity}" : "";
        return $"{ClassName}{bp}{qty}";
    }
}
