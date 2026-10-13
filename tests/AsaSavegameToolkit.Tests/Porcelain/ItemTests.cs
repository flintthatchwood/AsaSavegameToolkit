using System.Text.Json;
using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Porcelain;

namespace AsaSavegameToolkit.Tests.Porcelain;

[TestClass]
public class ItemTests
{
    [TestMethod]
    [DataRow("CrafterCharacterName")]
    [DataRow("CrafterTribeName")]
    public void Create_CraftedItem_PopulatesCommonAndCraftingProperties(string crafterProperty)
    {
        var record = Record(
            new StructProperty
            {
                Tag = Tag("ItemID", "StructProperty"),
                Value = new List<Property>
                {
                    new UInt32Property { Tag = Tag("ItemID1", "UInt32Property"), Value = 0x87654321 },
                    new UInt32Property { Tag = Tag("ItemID2", "UInt32Property"), Value = 0xFEDCBA98 }
                }
            },
            new IntProperty { Tag = Tag("ItemQuantity", "IntProperty"), Value = 5 },
            new BoolProperty { Tag = Tag("bIsBlueprint", "BoolProperty"), Value = true },
            new BoolProperty { Tag = Tag("bIsEngram", "BoolProperty"), Value = true },
            new BoolProperty { Tag = Tag("bIsFoodRecipe", "BoolProperty"), Value = true },
            new BoolProperty { Tag = Tag("bIsInitialItem", "BoolProperty"), Value = true },
            new FloatProperty { Tag = Tag("SavedDurability", "FloatProperty"), Value = 0.75f },
            new FloatProperty { Tag = Tag("ItemRating", "FloatProperty"), Value = 4.5f },
            new UInt32Property { Tag = Tag("ItemStatValues", "UInt32Property"), Value = 42 },
            new Int16Property { Tag = Tag("ItemColorID", "Int16Property"), Value = 12 },
            new StrProperty { Tag = Tag("CustomItemName", "StrProperty"), Value = "Tool" },
            new StrProperty { Tag = Tag("CustomItemDescription", "StrProperty"), Value = "Description" },
            new FloatProperty { Tag = Tag("CraftingSkill", "FloatProperty"), Value = 1.5f },
            new FloatProperty { Tag = Tag("CraftedSkillBonus", "FloatProperty"), Value = 0.25f });
        var transform = new ActorTransform(new FVector(10, 20, 30), default);

        var ordinary = Item.Create(record, transform);
        record.Properties.Add(new StrProperty { Tag = Tag(crafterProperty, "StrProperty"), Value = "Crafter" });
        var crafted = Item.Create(record, transform);

        Assert.IsInstanceOfType<CraftedItem>(crafted);
        var crafting = (CraftedItem)crafted;
        Assert.IsTrue(crafted.IsCrafted);
        Assert.IsFalse(ordinary.IsCrafted);
        Assert.AreSame(record, crafted.Record);
        Assert.AreEqual(record.Uuid, crafted.Id);
        Assert.AreEqual(unchecked((long)0x87654321FEDCBA98UL), crafted.ItemId);
        Assert.AreEqual(5, crafted.Quantity);
        Assert.AreEqual(ordinary.ClassName, crafted.ClassName);
        Assert.AreEqual(ordinary.IsBlueprint, crafted.IsBlueprint);
        Assert.AreEqual(ordinary.IsEngram, crafted.IsEngram);
        Assert.AreEqual(ordinary.IsRecipe, crafted.IsRecipe);
        Assert.AreEqual(ordinary.IsInitialItem, crafted.IsInitialItem);
        Assert.AreEqual(ordinary.Durability, crafted.Durability);
        Assert.AreEqual(ordinary.ItemRating, crafted.ItemRating);
        CollectionAssert.AreEqual(ordinary.BaseStats, crafted.BaseStats);
        CollectionAssert.AreEqual(ordinary.Colors, crafted.Colors);
        Assert.AreEqual(ordinary.CustomItemName, crafted.CustomItemName);
        Assert.AreEqual(ordinary.CustomItemDescription, crafted.CustomItemDescription);
        Assert.AreEqual(transform.Location, crafted.Location);
        Assert.AreEqual(transform.Rotation, crafted.Rotation);
        Assert.AreEqual(1.5f, crafting.CraftingSkill);
        Assert.AreEqual(0.25f, crafting.CraftingSkillBonus);
        Assert.AreEqual(crafterProperty == "CrafterCharacterName" ? "Crafter" : "", crafting.CrafterCharacterName);
        Assert.AreEqual(crafterProperty == "CrafterTribeName" ? "Crafter" : "", crafting.CrafterTribeName);
    }

    [TestMethod]
    public void Create_MissingProperties_PreservesDefaults()
    {
        var ordinary = Item.Create(Record());
        var crafted = CraftedItem.Create(Record());

        Assert.IsNull(ordinary.Durability);
        Assert.IsNull(ordinary.ItemRating);
        Assert.IsNull(ordinary.CustomItemName);
        Assert.IsNull(ordinary.CustomItemDescription);
        Assert.AreEqual(1, ordinary.Quantity);
        Assert.AreEqual(1, crafted.Quantity);
        Assert.AreEqual(0f, crafted.Durability);
        Assert.AreEqual(0f, crafted.ItemRating);
        Assert.AreEqual("", crafted.CustomItemName);
        Assert.AreEqual("", crafted.CustomItemDescription);
        Assert.IsTrue(crafted.IsCrafted);
    }

    [TestMethod]
    [DataRow(0, "Lvl 8")]
    [DataRow(1, "Male - Lvl 8")]
    [DataRow(2, "Female - Lvl 8")]
    public void Create_FertileEgg_PreservesCommonAndEggProperties(int gender, string expectedName)
    {
        var record = Record(
            new StructProperty
            {
                Tag = Tag("ItemID", "StructProperty"),
                Value = new List<Property>
                {
                    new UInt32Property { Tag = Tag("ItemID1", "UInt32Property"), Value = 0x87654321 },
                    new UInt32Property { Tag = Tag("ItemID2", "UInt32Property"), Value = 0xFEDCBA98 }
                }
            },
            new IntProperty { Tag = Tag("ItemQuantity", "IntProperty"), Value = 3 },
            new FloatProperty { Tag = Tag("SavedDurability", "FloatProperty"), Value = 0.5f },
            new FloatProperty { Tag = Tag("ItemRating", "FloatProperty"), Value = 2f },
            new StrProperty { Tag = Tag("CustomItemName", "StrProperty"), Value = "Ignored" },
            new StrProperty { Tag = Tag("CustomItemDescription", "StrProperty"), Value = "Parents" },
            new BoolProperty { Tag = Tag("bIsBlueprint", "BoolProperty"), Value = true },
            new BoolProperty { Tag = Tag("bIsEngram", "BoolProperty"), Value = true },
            new BoolProperty { Tag = Tag("bIsCustomRecipe", "BoolProperty"), Value = true },
            new BoolProperty { Tag = Tag("bIsInitialItem", "BoolProperty"), Value = true },
            new ByteProperty { Tag = Tag("EggNumberOfLevelUpPointsApplied", "ByteProperty"), Value = 5 },
            new ByteProperty { Tag = Tag("EggNumberOfMutationsApplied", "ByteProperty"), Value = 2 },
            new ByteProperty { Tag = Tag("EggColorSetIndices", "ByteProperty"), Value = 7 },
            new IntProperty { Tag = Tag("EggRandomMutationsFemale", "IntProperty"), Value = 4 },
            new IntProperty { Tag = Tag("EggRandomMutationsMale", "IntProperty"), Value = 6 },
            new IntProperty { Tag = Tag("EggGenderOverride", "IntProperty"), Value = gender },
            new ArrayProperty
            {
                Tag = Tag("EggDinoGeneTraits", "ArrayProperty"),
                Value = new List<FName> { new(-1, 0, "Trait") }
            });
        var transform = new ActorTransform(new FVector(10, 20, 30), default);

        var item = Item.Create(record, transform);

        Assert.IsInstanceOfType<FertileEggItem>(item);
        var egg = (FertileEggItem)item;
        Assert.AreSame(record, egg.Record);
        Assert.AreEqual(record.Uuid, egg.Id);
        Assert.AreEqual(record.ClassName.FullName, egg.ClassName);
        Assert.AreEqual(unchecked((long)0x87654321FEDCBA98UL), egg.ItemId);
        Assert.AreEqual(3, egg.Quantity);
        Assert.AreEqual(transform.Location, egg.Location);
        Assert.AreEqual(transform.Rotation, egg.Rotation);
        Assert.AreEqual(0.5f, egg.Durability);
        Assert.AreEqual(2f, egg.ItemRating);
        Assert.AreEqual("Parents", egg.CustomItemDescription);
        Assert.AreEqual(expectedName, egg.CustomItemName);
        Assert.HasCount(12, egg.BaseStats);
        Assert.AreEqual((byte)5, egg.BaseStats[0]);
        Assert.AreEqual((byte)2, egg.MutationStats[0]);
        Assert.AreEqual(7, egg.Colors[0]);
        Assert.AreEqual(4, egg.RandomMutationsFemale);
        Assert.AreEqual(6, egg.RandomMutationsMale);
        Assert.AreEqual(gender == 2, egg.IsFemale);
        Assert.HasCount(1, egg.GeneTraits);
        Assert.AreEqual("Trait", egg.GeneTraits[0]);
        Assert.IsFalse(egg.IsBlueprint);
        Assert.IsFalse(egg.IsEngram);
        Assert.IsFalse(egg.IsRecipe);
        Assert.IsFalse(egg.IsInitialItem);
        Assert.IsFalse(egg.IsCrafted);
    }

    [TestMethod]
    public void Create_FertileEggMissingProperties_PreservesDefaults()
    {
        var egg = FertileEggItem.Create(Record());

        Assert.AreEqual(1, egg.Quantity);
        Assert.AreEqual(0f, egg.Durability);
        Assert.AreEqual(0f, egg.ItemRating);
        Assert.AreEqual("", egg.CustomItemDescription);
        Assert.AreEqual("Lvl 1", egg.CustomItemName);
        Assert.HasCount(12, egg.BaseStats);
        Assert.HasCount(12, egg.MutationStats);
        Assert.HasCount(6, egg.Colors);
        Assert.IsEmpty(egg.FemaleAncestry);
        Assert.IsEmpty(egg.MaleAncestry);
        Assert.IsEmpty(egg.GeneTraits);
    }

    [TestMethod]
    public void Serialize_BaseItemCollection_IncludesIsCrafted()
    {
        Item[] items =
        [
            new Item { ClassName = "Resource" },
            new CraftedItem { ClassName = "Tool" },
            new FertileEggItem { ClassName = "Egg" }
        ];

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(items));
        Assert.IsFalse(json.RootElement[0].GetProperty("IsCrafted").GetBoolean());
        Assert.IsTrue(json.RootElement[1].GetProperty("IsCrafted").GetBoolean());
        Assert.IsFalse(json.RootElement[2].GetProperty("IsCrafted").GetBoolean());
    }

    [TestMethod]
    public void Create_WorldTransformsAreSeparateFromPositionForEveryItemType()
    {
        var record = Record();
        var transform = new ActorTransform(new FVector(10, 20, -30), new FQuat(0, 0, 0.5, 0.5));
        Item[] items =
        [
            Item.Create(record, transform),
            CraftedItem.Create(record, transform),
            FertileEggItem.Create(record, transform)
        ];
        var map = new MapDefinition { ScaleX = 2, ScaleY = 4 };

        foreach (var item in items)
        {
            Assert.AreEqual(transform.Location, item.Location);
            Assert.AreEqual(transform.Rotation, item.Rotation);
            var coordinates = map.GetCoordinates(item.Location);
            Assert.IsNotNull(coordinates);
            Assert.AreEqual(55.0, coordinates.Value.Latitude);
            Assert.AreEqual(55.0, coordinates.Value.Longitude);
            Assert.AreEqual("default", coordinates.Value.Region?.Name);
        }
    }

    [TestMethod]
    public void Create_InventoryItemsHaveNoFabricatedWorldTransform()
    {
        var record = Record(new IntProperty { Tag = Tag("ItemQuantity", "IntProperty"), Value = 2 });
        Item[] items =
        [
            Item.Create(Record()),
            Item.Create(record),
            CraftedItem.Create(record),
            FertileEggItem.Create(record),
            Item.FromCryoSaddle(record)
        ];

        foreach (var item in items)
        {
            Assert.IsNull(item.Location);
            Assert.IsNull(item.Rotation);
        }
    }

    [TestMethod]
    public void Serialize_RawLocationAndConvertedCoordinatesHaveSeparateShapes()
    {
        var item = new Item { ClassName = "Item", Location = new FVector(10, 20, -30) };
        var map = new MapDefinition { ScaleX = 2, ScaleY = 4 };

        using var itemJson = JsonDocument.Parse(JsonSerializer.Serialize(item));
        var position = itemJson.RootElement.GetProperty("Location");
        Assert.HasCount(3, position.EnumerateObject().ToArray());
        Assert.AreEqual(10.0, position.GetProperty("X").GetDouble());
        Assert.AreEqual(20.0, position.GetProperty("Y").GetDouble());
        Assert.AreEqual(-30.0, position.GetProperty("Z").GetDouble());
        Assert.IsFalse(itemJson.RootElement.TryGetProperty("GPSLocation", out _));

        using var coordinatesJson = JsonDocument.Parse(JsonSerializer.Serialize(map.GetCoordinates(item.Location)));
        var coordinates = coordinatesJson.RootElement;
        Assert.HasCount(3, coordinates.EnumerateObject().ToArray());
        Assert.AreEqual(55.0, coordinates.GetProperty("Latitude").GetDouble());
        Assert.AreEqual(55.0, coordinates.GetProperty("Longitude").GetDouble());
        Assert.AreEqual("default", coordinates.GetProperty("Region").GetProperty("Name").GetString());
    }

    private static GameObjectRecord Record(params Property[] properties) =>
        new(Guid.NewGuid(), new FName(-1, 0, "/Game/Test/Test.Test_C"), ["Item"], [.. properties],
            0, ObjectTypeFlags.None, extraGuids: []);

    private static PropertyTag Tag(string name, string type) => new()
    {
        Name = new FName(-1, 0, name),
        Type = FPropertyTypeName.Create(new FName(-1, 0, type))
    };
}
