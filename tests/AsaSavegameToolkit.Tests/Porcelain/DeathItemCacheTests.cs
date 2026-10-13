using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Porcelain;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace AsaSavegameToolkit.Tests.Porcelain;

[TestClass]
public class DeathItemCacheTests
{
    [TestMethod]
    [DataRow("DeathItemCache_C", true)]
    [DataRow("/Game/PrimalEarth/Structures/DeathItemCache.DeathItemCache_C", true)]
    [DataRow("/Game/Test/DeathItemCache_Player.DeathItemCache_Player_C", true)]
    [DataRow("/Game/Test/DeathItemCache_Chest.StorageBox_C", false)]
    [DataRow("/Game/Test/Chest.Chest_C", false)]
    public void Classification_UsesShortClassNameAndExcludesOnlyCaches(string className, bool expectedCache)
    {
        var record = Record(className, ["Cache"], Text("OwnerName", "Owner"), Bool("bHasResetDecayTime", true));

        Assert.AreEqual(expectedCache, record.IsDeathItemCache());
        Assert.AreEqual(!expectedCache, record.IsStructure());
    }

    [TestMethod]
    public void Classification_DeadCreatureRemainsCreature()
    {
        var record = Record("/Game/Test/Raptor.Raptor_Character_BP_C", ["Raptor"],
            Bool("bServerInitializedDino", true), Bool("bIsDead", true));

        Assert.IsTrue(record.IsCreature());
        Assert.IsFalse(record.IsDeathItemCache());
    }

    [TestMethod]
    public void Create_PopulatesCacheInventoryTransformAndTiming()
    {
        var itemRecord = Record("/Game/Test/Item.Item_C", ["Item"]);
        var item = Item.Create(itemRecord);
        var inventory = Record("/Game/Test/Inventory.Inventory_C", ["Inventory", "Cache"],
            new ArrayProperty
            {
                Tag = Tag("InventoryItems", "ArrayProperty"),
                Value = new ObjectReference[] { new() { ObjectId = item.Id } }
            });
        var record = Record("/Game/PrimalEarth/Structures/DeathItemCache.DeathItemCache_C", ["Cache"],
            Reference("MyInventoryComponent", inventory.Uuid),
            Text("OwnerName", "Raptor - Lvl 120"),
            Int("TargetingTeam", 5),
            Int("OwningPlayerID", 123),
            Double("OriginalCreationTime", 100000800),
            Double("NetDestructionTime", 100001100));
        record.Components["Inventory"] = inventory;
        var transform = new ActorTransform(new FVector(10, 20, -30), new FQuat(0, 0, 0.5, 0.5));

        var cache = DeathItemCache.Create(record, transform, new Dictionary<Guid, Item> { [item.Id] = item }, NullLogger.Instance);
        var timestamp = new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);
        cache.RefreshTimestamps(timestamp, 100001000);

        Assert.AreSame(record, cache.Record);
        Assert.AreEqual(record.Uuid, cache.Id);
        Assert.AreEqual(record.ClassName.FullName, cache.ClassName);
        Assert.AreEqual("Raptor - Lvl 120", cache.OwnerName);
        Assert.AreEqual(5, cache.TargetingTeam);
        Assert.AreEqual(123, cache.OwningPlayerId);
        Assert.AreEqual(transform.Location, cache.Location);
        Assert.AreEqual(transform.Rotation, cache.Rotation);
        Assert.AreEqual(timestamp.AddSeconds(-200), cache.CreationTimestamp);
        Assert.AreEqual(timestamp.AddSeconds(100), cache.ExpirationTimestamp);
        Assert.IsNotNull(cache.Inventory);
        Assert.AreSame(inventory, cache.Inventory.Record);
        Assert.HasCount(1, cache.Inventory.Items);
        Assert.AreSame(item, cache.Inventory.Items[item.Id]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Create_MissingOrZeroDataDoesNotInventOwnershipLocationOrTimes(bool zeroTimes)
    {
        var record = Record("DeathItemCache_C", ["Cache"]);
        if (zeroTimes)
        {
            record.Properties.Add(Double("OriginalCreationTime", 0));
            record.Properties.Add(Double("NetDestructionTime", 0));
        }

        var cache = DeathItemCache.Create(record, null, new Dictionary<Guid, Item>(), NullLogger.Instance);
        cache.RefreshTimestamps(DateTime.UtcNow, 100001000);

        Assert.IsNull(cache.OwnerName);
        Assert.IsNull(cache.TargetingTeam);
        Assert.IsNull(cache.OwningPlayerId);
        Assert.IsNull(cache.Location);
        Assert.IsNull(cache.Rotation);
        Assert.IsNull(cache.Inventory);
        Assert.IsNull(cache.CreationTimestamp);
        Assert.IsNull(cache.ExpirationTimestamp);
    }

    [TestMethod]
    public void Create_UnresolvedInventoryLogsWarning()
    {
        var record = Record("DeathItemCache_C", ["Cache"], Reference("MyInventoryComponent", Guid.NewGuid()));
        var logger = new Mock<ILogger>();

        var cache = DeathItemCache.Create(record, null, new Dictionary<Guid, Item>(), logger.Object);

        Assert.IsNull(cache.Inventory);
        AssertWarning(logger);
    }

    [TestMethod]
    public void Inventory_ResolvesGuidReferencesWithoutCopyingOrDuplicatingItems()
    {
        var item = Item.Create(Record("Item_C", ["Item"]));
        var inventoryRecord = Record("Inventory_C", ["Inventory"],
            new ArrayProperty
            {
                Tag = Tag("InventoryItems", "ArrayProperty"),
                Value = new ObjectReference[]
                {
                    new() { ObjectId = item.Id }, new() { ObjectId = item.Id }, new()
                }
            });

        var inventory = Inventory.Create(inventoryRecord, new Dictionary<Guid, Item> { [item.Id] = item });

        Assert.HasCount(1, inventory.Items);
        Assert.AreSame(item, inventory.Items[item.Id]);
    }

    [TestMethod]
    public void Inventory_MissingItemLogsWarning()
    {
        var inventoryRecord = Record("Inventory_C", ["Inventory"],
            new ArrayProperty
            {
                Tag = Tag("InventoryItems", "ArrayProperty"),
                Value = new ObjectReference[] { new() { ObjectId = Guid.NewGuid() } }
            });
        var logger = new Mock<ILogger>();

        var inventory = Inventory.Create(inventoryRecord, new Dictionary<Guid, Item>(), logger.Object);

        Assert.IsEmpty(inventory.Items);
        AssertWarning(logger);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Inventory_UnsupportedReferenceThrows(bool pathReference)
    {
        var inventoryRecord = Record("Inventory_C", ["Inventory"],
            new ArrayProperty
            {
                Tag = Tag("InventoryItems", "ArrayProperty"),
                Value = pathReference
                    ? new object[] { new ObjectReference { IsPath = true } }
                    : new object[] { "unexpected" }
            });

        Assert.ThrowsExactly<InvalidDataException>(() => Inventory.Create(inventoryRecord, new Dictionary<Guid, Item>()));
    }

    private static void AssertWarning(Mock<ILogger> logger) =>
        Assert.IsTrue(logger.Invocations.Any(call =>
            call.Method.Name == nameof(ILogger.Log) && Equals(call.Arguments[0], LogLevel.Warning)));

    private static GameObjectRecord Record(string className, string[] names, params Property[] properties) =>
        new(Guid.NewGuid(), new FName(-1, 0, className), names, [.. properties], 0, ObjectTypeFlags.None, extraGuids: []);

    private static StrProperty Text(string name, string value) => new() { Tag = Tag(name, "StrProperty"), Value = value };
    private static IntProperty Int(string name, int value) => new() { Tag = Tag(name, "IntProperty"), Value = value };
    private static BoolProperty Bool(string name, bool value) => new() { Tag = Tag(name, "BoolProperty"), Value = value };
    private static DoubleProperty Double(string name, double value) => new() { Tag = Tag(name, "DoubleProperty"), Value = value };
    private static ObjectProperty Reference(string name, Guid id) =>
        new() { Tag = Tag(name, "ObjectProperty"), Value = new ObjectReference { ObjectId = id } };

    private static PropertyTag Tag(string name, string type) => new()
    {
        Name = new FName(-1, 0, name),
        Type = FPropertyTypeName.Create(new FName(-1, 0, type))
    };
}
