using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsaSavegameToolkit.Porcelain
{
    public class CraftedItem : Item
    {
        public string CrafterCharacterName { get; set; } = string.Empty;    
        public string CrafterTribeName{ get; set; } = string.Empty;
        public float CraftingSkill { get; set; } = 0;
        public float CraftingSkillBonus { get; set; } = 0;

        internal static new CraftedItem Create(GameObjectRecord record, ActorTransform? location = default)
        {
            var className = record.GetClassName();

            var properties = record.Properties;

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
            string crafterTribeName = "";
            string crafterCharacterName = "";
            float craftingSkill = 0;
            float craftingSkillBonus = 0;
            string customItemName = string.Empty;
            string customItemDescription = string.Empty;
            int[] itemColors = new int[6];

            var itemIdProperties = (List<Property>?)properties.Get<StructProperty>("ItemID")?.Value;
            if (itemIdProperties != null)
            {
                itemId1 = itemIdProperties.Get<uint>("ItemID1");
                itemId2 = itemIdProperties.Get<uint>("ItemID2");
            }
            var itemId = long.Parse($"{itemId1}{itemId2}");

            itemQuantity = properties.Get<int>("ItemQuantity");
            if (itemQuantity == 0)
                itemQuantity = 1;

            isBlueprint = properties.Get<bool>("bIsBlueprint");
            isEngram = properties.Get<bool>("bIsEngram");
            isCustomRecipe = properties.Get<bool>("bIsCustomRecipe");
            isFoodRecipe = properties.Get<bool>("bIsFoodRecipe");
            isInitialItem = properties.Get<bool>("bIsInitialItem");
            allowRemovalFromInventory = properties.Get<bool>("bAllowRemovalFromInventory");


            //saddles/structures/armor/tools/weapons etc.
            savedDurability = properties.Get<float>("SavedDurability");
            itemRating = properties.Get<float>("ItemRating");
            itemQualityIndex = properties.Get<byte>("ItemQualityIndex");


            //ItemStatValues //uint16
            byte[] baseStatValues = new byte[8];
            for (int i = 0; i < baseStatValues.Length; i++)
            {
                baseStatValues[i] = (byte)properties.Get<uint>("ItemStatValues", i);
            }


            //ItemColorID //int
            if (properties.HasAny("ItemColorID"))
            {
                for (int i = 0; i < itemColors.Length; i++)
                {
                    itemColors[i] = properties.Get<short>("ItemColorID", i);
                }
            }

            //crafted items
            crafterTribeName = properties.Get<string>("CrafterTribeName") ?? "";
            crafterCharacterName = properties.Get<string>("CrafterCharacterName") ?? "";
            craftingSkill = properties.Get<float>("CraftingSkill");
            craftingSkillBonus = properties.Get<float>("CraftedSkillBonus");
            customItemName = properties.Get<string>("CustomItemName") ?? "";
            customItemDescription = properties.Get<string>("CustomItemDescription") ?? "";


            return new CraftedItem
            {
                Id = record.Uuid,
                ClassName = className,
                ItemId = itemId,
                Quantity = itemQuantity,
                IsBlueprint = isBlueprint,
                IsEngram = isEngram,
                IsRecipe = isCustomRecipe || isFoodRecipe,
                Durability = savedDurability,
                ItemRating = itemRating,
                BaseStats = baseStatValues,
                Colors = itemColors,
                CrafterCharacterName = crafterCharacterName,
                CrafterTribeName = crafterTribeName,
                CraftingSkill = craftingSkill,
                CraftingSkillBonus = craftingSkillBonus
            };
        }
    }
}
