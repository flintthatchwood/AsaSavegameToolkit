using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

namespace AsaSavegameToolkit.Tests.Porcelain;

[TestClass]
public class CreatureTests : SaveTests
{
    // Yutyrannus from game object record tests - confirmed present in both versions
    private const string YutyrannusId = "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a";

    [TestMethod]
    public void IsCreature_Version13_ReturnsTrue()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_13/TheIsland_WP.ark");
        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        var record = reader.ReadGameRecord(Guid.Parse(YutyrannusId), TestContext.CancellationToken);

        Assert.IsTrue(record.IsCreature());
    }

    [TestMethod]
    public void IsCreature_Version14_ReturnsTrue()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/TheIsland_WP.ark");
        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        var record = reader.ReadGameRecord(Guid.Parse(YutyrannusId), TestContext.CancellationToken);

        Assert.IsTrue(record.IsCreature());
    }

    [TestMethod]
    public void CanWrapCreature_Version13_ClassNameIsCorrect()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_13/TheIsland_WP.ark");
        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        var record = reader.ReadGameRecord(Guid.Parse(YutyrannusId), TestContext.CancellationToken);

        var creature = Creature.Create(record, transform: null);

        Assert.Contains("Yutyrannus", creature.ClassName);
    }

    [TestMethod]
    public void CanWrapCreature_Version14_ClassNameIsCorrect()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/TheIsland_WP.ark");
        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        var record = reader.ReadGameRecord(Guid.Parse(YutyrannusId), TestContext.CancellationToken);

        var creature = Creature.Create(record, transform: null);

        Assert.Contains("Yutyrannus", creature.ClassName);
    }

    [TestMethod]
    public void CreatureProperties_Version13_DoNotThrow()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_13/TheIsland_WP.ark");
        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        var record = reader.ReadGameRecord(Guid.Parse(YutyrannusId), TestContext.CancellationToken);

        var creature = Creature.Create(record, transform: null);

        // Validate all accessors return without throwing
        _ = creature.Id;
        _ = creature.ClassName;
        _ = creature.IsTamed;
        _ = creature.IsFemale;
        _ = creature.TamedName;
        _ = creature.TribeName;
        _ = creature.TamerString;
        _ = creature.BaseLevel;
        _ = creature.ExtraLevel;
        _ = creature.TotalLevel;
        _ = creature.MutationsMale;
        _ = creature.MutationsFemale;
        _ = creature.TotalMutations;
        _ = creature.ImprintQuality;
        _ = creature.ImprinterName;
        _ = creature.BabyAge;
        _ = creature.IsJuvenile;
        _ = creature.TamedAtTime;
        _ = creature.DinoId1;
        _ = creature.DinoId2;
        _ = creature.ColorRegions;
        _ = creature.ToString();
    }

    [TestMethod]
    public void CreatureProperties_Version14_DoNotThrow()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/TheIsland_WP.ark");
        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        var record = reader.ReadGameRecord(Guid.Parse(YutyrannusId), TestContext.CancellationToken);

        var creature = Creature.Create(record, transform: null);

        _ = creature.Id;
        _ = creature.ClassName;
        _ = creature.IsTamed;
        _ = creature.IsFemale;
        _ = creature.TamedName;
        _ = creature.TribeName;
        _ = creature.TamerString;
        _ = creature.BaseLevel;
        _ = creature.ExtraLevel;
        _ = creature.TotalLevel;
        _ = creature.MutationsMale;
        _ = creature.MutationsFemale;
        _ = creature.TotalMutations;
        _ = creature.ImprintQuality;
        _ = creature.ImprinterName;
        _ = creature.BabyAge;
        _ = creature.IsJuvenile;
        _ = creature.TamedAtTime;
        _ = creature.DinoId1;
        _ = creature.DinoId2;
        _ = creature.ColorRegions;
        _ = creature.ToString();
    }

    [TestMethod]
    public void AsaSaveGame_IncludesCryoCreatures()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_13/TheIsland_WP.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);

        var cryoCreatures = saveGame.CryopoddedCreatures;
        TestContext.WriteLine($"Found {cryoCreatures.Count} cryo creatures in AsaSaveGame.CryopoddedCreatures");

        Assert.IsNotEmpty(cryoCreatures, "Expected extracted cryo creatures to be surfaced in AsaSaveGame.CryopoddedCreatures");

        foreach (var c in cryoCreatures.Values)
        {
            TestContext.WriteLine($"  {c.ClassName} '{c.TamedName}' lv={c.TotalLevel} tribe={c.TribeName} isInCryo={c.IsInCryo}");
            Assert.IsTrue(c.IsInCryo, $"Cryo creature {c.Id} should have IsInCryo=true");
        }
    }

    [TestMethod]
    public void AsaSaveGame_KeepsCryopoddedCopiesOfTheSameCreature()
    {
        // Aberration_14 has two cryopods holding the same creature
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/Aberration_14.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);

        Assert.HasCount(65, saveGame.CryopoddedCreatures);
        Assert.AreEqual(64, saveGame.CryopoddedCreatures.Values.Select(c => c.Id).Distinct().Count());

        // keyed by the pod holding each copy
        var records = ReadGameRecords(saveFile);
        Assert.IsTrue(saveGame.CryopoddedCreatures.Keys.All(id => records.TryGetValue(id, out var pod) && pod.HasCryoCreature()));
    }

    [TestMethod]
    public void AsaSaveGame_CryopoddedCreaturesWearTheirSaddles()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/TheIsland_14.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);

        var saddled = saveGame.CryopoddedCreatures.Values.Count(c => c.Inventory?.Items.Values.Any(i => i.ClassName.Contains("Saddle")) == true);
        Assert.AreEqual(35, saddled);

        // each creature wears exactly what its own pod holds after the creature's data (saddle, costume, hat)
        var records = ReadGameRecords(saveFile);
        using var cryoReader = new CryopodReader(TestContext.GetLogger(), null);
        foreach (var (podId, creature) in saveGame.CryopoddedCreatures)
        {
            var podItems = cryoReader.ReadCryopodData(records[podId]).Skip(1).SelectMany(set => set).Select(Describe).Order().ToList();
            var worn = creature.Inventory?.Items.Values.Select(i => Describe(i.Record!)).Order().ToList() ?? [];
            CollectionAssert.AreEqual(podItems, worn, $"pod {podId}");
        }
    }

    [TestMethod]
    [DataRow("creature, creature status", DisplayName = "creature and its status")]
    [DataRow("creature, player, creature status, player status", DisplayName = "a Carcha holding a player")]
    [DataRow("creature, player, player status, creature status", DisplayName = "statuses the other way round")]
    public void CreaturesFromCryopod_UsesTheCreaturesOwnStatus(string order)
    {
        var records = new Dictionary<string, GameObjectRecord>
        {
            ["creature"] = Record(["Carcha_Character_BP_C_1", "PersistentLevel"], BoolProp("bServerInitializedDino", true)),
            ["player"] = Record(["PlayerPawnTest_Male_C_2", "PersistentLevel"]),
            ["creature status"] = Record(["DinoCharacterStatus_BP_Carcha_C1", "Carcha_Character_BP_C_1", "PersistentLevel"], ByteProp("NumberOfLevelUpPointsApplied", 7)),
            ["player status"] = Record(["PlayerCharacterStatus", "PlayerPawnTest_Male_C_2", "PersistentLevel"], ByteProp("NumberOfLevelUpPointsApplied", 1)),
        };
        GameObjectRecord[] set = [.. order.Split(", ").Select(name => records[name])];

        var creatures = AsaSaveGame.CreaturesFromCryopod(Pod(), [set], NullLogger.Instance);

        Assert.HasCount(1, creatures);
        Assert.AreEqual((byte)7, creatures[0].WildLevels[0]);
        Assert.IsNull(creatures[0].Inventory, "no saddle made from the player's status component");
    }

    [TestMethod]
    public void CreaturesFromCryopod_WearsEveryItemAfterTheCreature()
    {
        var rex = Record(["Rex_Character_BP_C_1", "PersistentLevel"], BoolProp("bServerInitializedDino", true));
        var rexStatus = Record(["DinoCharacterStatus_BP_Rex_C1", "Rex_Character_BP_C_1", "PersistentLevel"]);
        var costume = Record(["PrimalItemCostume_C_2"]);
        var saddle = Record(["PrimalItemArmor_RexSaddle_C_3"]);

        var creatures = AsaSaveGame.CreaturesFromCryopod(Pod(), [[rex, rexStatus], [costume], [saddle]], NullLogger.Instance);

        Assert.HasCount(1, creatures);
        CollectionAssert.AreEquivalent(new[] { costume, saddle }, creatures[0].Inventory!.Items.Values.Select(i => i.Record).ToArray());
    }

    private IReadOnlyDictionary<Guid, GameObjectRecord> ReadGameRecords(string saveFile)
    {
        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        reader.ReadSaveHeader(TestContext.CancellationToken);
        return reader.ReadGameRecords(TestContext.CancellationToken);
    }

    // item records read from a cryopod are all class "Unknown", so compare them by their properties
    private static string Describe(GameObjectRecord item) =>
        string.Join(", ", item.Properties.Select(p => $"{p.Tag.Name.FullName}[{p.Tag.ArrayIndex}]={p.GetValue()}"));

    private static GameObjectRecord Pod() => Record(["PrimalItem_WeaponEmptyCryopod_C_1"]);

    private static GameObjectRecord Record(string[] names, params Property[] properties) =>
        new(Guid.NewGuid(), new FName(-1, 0, "/Game/Test/Test.Test_C"), names, [.. properties], 0, ObjectTypeFlags.None, extraGuids: []);

    private static PropertyTag Tag(string name, string type) => new()
    {
        Name = new FName(-1, 0, name),
        Type = FPropertyTypeName.Create(new FName(-1, 0, type)),
    };

    private static BoolProperty BoolProp(string name, bool value) => new() { Tag = Tag(name, "BoolProperty"), Value = value };

    private static ByteProperty ByteProp(string name, byte value) => new() { Tag = Tag(name, "ByteProperty"), Value = value };
}
