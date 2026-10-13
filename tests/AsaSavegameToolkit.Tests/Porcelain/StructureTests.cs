using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Porcelain;

namespace AsaSavegameToolkit.Tests.Porcelain;

[TestClass]
public class StructureTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(50000)]
    [DataRow(1000000000)]
    public void Create_PopulatesSharedAndPlayerProperties(int team)
    {
        var record = Record("Test_C",
            Int("TargetingTeam", team),
            Text("BoxName", "Box"),
            Text("StructureName", "Fallback structure"),
            Text("OwnerName", "Owner"),
            Text("TribeName", "Fallback tribe"),
            Bool("bHasResetDecayTime", true),
            Bool("bContainerActivated", true),
            Double("LastActivatedTime", 900),
            Double("LastDeactivatedTime", 950),
            Int("OwningPlayerID", 123),
            Int("OriginalPlacerID", 456),
            Text("OriginalPlacedTimeStamp", "Placed"),
            Double("OriginalCreationTime", 800),
            Double("LastAllyInRangeTimeSerialized", 850),
            Bool("bHasFuel", true),
            Bool("bLastToggleActivated", true),
            Bool("bIsPinLocked", true),
            Bool("bIsLocked", true),
            Bool("bIsWatered", true),
            Bool("bIsPinPowered", true),
            Bool("bIsSeeded", true),
            Bool("bIsFertilized", true));
        var transform = new ActorTransform(new FVector(10, 20, 30), new FQuat(0, 0, 0.5, 0.5));

        var structure = Structure.Create(record, transform);

        Assert.AreSame(record, structure.Record);
        Assert.AreEqual(record.Uuid, structure.Id);
        Assert.AreEqual(record.ClassName.FullName, structure.ClassName);
        Assert.AreEqual("Box", structure.StructureName);
        Assert.AreEqual("Owner", structure.TribeName);
        Assert.IsTrue(structure.IsPlayerBuilt);
        Assert.IsTrue(structure.IsActivated);
        Assert.AreEqual(900.0, structure.LastActivatedGameTime);
        Assert.AreEqual(950.0, structure.LastDeactivatedGameTime);
        Assert.AreEqual(transform.Location, structure.Location);
        Assert.AreEqual(transform.Rotation, structure.Rotation);

        var map = new MapDefinition { CenterX = -10, CenterY = 30, ScaleX = 2, ScaleY = 4 };
        var coordinates = map.GetCoordinates(structure.Location);
        Assert.IsNotNull(coordinates);
        Assert.AreEqual(60.0, coordinates.Value.Longitude);
        Assert.AreEqual(47.5, coordinates.Value.Latitude);
        Assert.AreEqual("default", coordinates.Value.Region?.Name);

        var saveTimestamp = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);
        structure.RefreshTimestamps(saveTimestamp, 1000);
        Assert.AreEqual(saveTimestamp.AddSeconds(-100), structure.LastActivatedTimestamp);
        Assert.AreEqual(saveTimestamp.AddSeconds(-50), structure.LastDeactivatedTimestamp);

        if (team == 0)
        {
            Assert.AreEqual(typeof(Structure), structure.GetType());
        }
        else
        {
            Assert.IsInstanceOfType<PlayerStructure>(structure);
            var player = (PlayerStructure)structure;
            Assert.AreEqual((long)team, player.TribeId);
            Assert.AreEqual(123L, player.OwningPlayerId);
            Assert.AreEqual(456L, player.PlacerId);
            Assert.AreEqual("Placed", player.PlacedTime);
            Assert.AreEqual(800.0, player.CreationGameTime);
            Assert.AreEqual(850.0, player.LastAllyInRangeGameTime);
            Assert.AreEqual(saveTimestamp.AddSeconds(-200), player.CreationTimestamp);
            Assert.AreEqual(saveTimestamp.AddSeconds(-150), player.LastAllyInRangeTimestamp);
            Assert.IsTrue(player.HasFuel);
            Assert.IsTrue(player.LastToggleActivated);
            Assert.IsTrue(player.IsPinLocked);
            Assert.IsTrue(player.IsLocked);
            Assert.IsTrue(player.IsWatered);
            Assert.IsTrue(player.IsPowered);
            Assert.IsTrue(player.IsSeeded);
            Assert.IsTrue(player.IsFertilized);
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(50000)]
    public void Create_UsesFallbackNames(int team)
    {
        var record = Record("Test_C", Int("TargetingTeam", team),
            Text("StructureName", "Structure"), Text("TribeName", "Tribe"));

        var structure = Structure.Create(record, null);

        Assert.AreEqual("Structure", structure.StructureName);
        Assert.AreEqual("Tribe", structure.TribeName);
        Assert.IsNull(structure.Location);
        Assert.IsNull(structure.Rotation);
    }

    [TestMethod]
    [DataRow("Test_C", 49999, false, false, false)]
    [DataRow("Test_C", 50000, false, true, false)]
    [DataRow("Test_C", 0, true, false, true)]
    [DataRow("Test_C", 1000000000, true, true, true)]
    [DataRow("Raft_BP_C", 0, false, false, true)]
    [DataRow("Raft_BP_C", 50000, false, true, true)]
    [DataRow("MotorRaft_BP_C", 0, false, false, true)]
    [DataRow("MotorRaft_BP_C", 1000000000, false, true, true)]
    public void Create_PreservesIndependentSubtypeAndPlayerBuiltRules(
        string className, int team, bool decayReset, bool expectedPlayer, bool expectedPlayerBuilt)
    {
        var record = Record(className, Int("TargetingTeam", team), Bool("bHasResetDecayTime", decayReset));

        var structure = Structure.Create(record, null);

        Assert.AreEqual(expectedPlayer, structure is PlayerStructure);
        Assert.AreEqual(expectedPlayerBuilt, structure.IsPlayerBuilt);
    }

    [TestMethod]
    public void Create_MissingPropertiesPreservesDefaultsForBothFactories()
    {
        var record = Record("Test_C");
        var player = PlayerStructure.Create(record, null);
        Structure[] structures = [Structure.Create(record, null), player];

        foreach (var structure in structures)
        {
            Assert.AreSame(record, structure.Record);
            Assert.AreEqual(record.Uuid, structure.Id);
            Assert.AreEqual(record.ClassName.FullName, structure.ClassName);
            Assert.IsNull(structure.StructureName);
            Assert.IsNull(structure.TribeName);
            Assert.IsFalse(structure.IsPlayerBuilt);
            Assert.IsFalse(structure.IsActivated);
            Assert.AreEqual(0.0, structure.LastActivatedGameTime);
            Assert.AreEqual(0.0, structure.LastDeactivatedGameTime);
            Assert.IsNull(structure.Location);
            Assert.IsNull(structure.Rotation);
            Assert.IsNull(structure.Inventory);
            structure.RefreshTimestamps(DateTime.UtcNow, 1000);
            Assert.IsNull(new MapDefinition { ScaleX = 1, ScaleY = 1 }.GetCoordinates(structure.Location));
            Assert.IsNull(structure.LastActivatedTimestamp);
            Assert.IsNull(structure.LastDeactivatedTimestamp);
        }

        Assert.AreEqual(0L, player.TribeId);
        Assert.AreEqual(0L, player.OwningPlayerId);
        Assert.AreEqual(0L, player.PlacerId);
        Assert.AreEqual("", player.PlacedTime);
        Assert.AreEqual(0.0, player.CreationGameTime);
        Assert.AreEqual(0.0, player.LastAllyInRangeGameTime);
        Assert.IsNull(player.CreationTimestamp);
        Assert.IsNull(player.LastAllyInRangeTimestamp);
        Assert.IsFalse(player.HasFuel);
        Assert.IsFalse(player.LastToggleActivated);
        Assert.IsFalse(player.IsPinLocked);
        Assert.IsFalse(player.IsLocked);
        Assert.IsFalse(player.IsWatered);
        Assert.IsFalse(player.IsPowered);
        Assert.IsFalse(player.IsSeeded);
        Assert.IsFalse(player.IsFertilized);
    }

    private static GameObjectRecord Record(string className, params Property[] properties) =>
        new(Guid.NewGuid(), new FName(-1, 0, $"/Game/Test/Test.{className}"), ["Structure"], [.. properties],
            0, ObjectTypeFlags.None, extraGuids: []);

    private static IntProperty Int(string name, int value) =>
        new() { Tag = Tag(name, "IntProperty"), Value = value };

    private static StrProperty Text(string name, string value) =>
        new() { Tag = Tag(name, "StrProperty"), Value = value };

    private static BoolProperty Bool(string name, bool value) =>
        new() { Tag = Tag(name, "BoolProperty"), Value = value };

    private static DoubleProperty Double(string name, double value) =>
        new() { Tag = Tag(name, "DoubleProperty"), Value = value };

    private static PropertyTag Tag(string name, string type) => new()
    {
        Name = new FName(-1, 0, name),
        Type = FPropertyTypeName.Create(new FName(-1, 0, type))
    };
}
