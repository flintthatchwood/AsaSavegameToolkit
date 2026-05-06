using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Porcelain;

[TestClass]
public class CreatureTests : SaveTests
{
    // Yutyrannus from game object record tests - confirmed present in both versions
    private const string YutyrannusId = "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a";
    private const string RexId = "f2ad10f5-4317-3b46-36b2-afb639b20329";

    [TestMethod]
    public void CanParseCreatureData()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP.ark");
        //var saveFile = @"C:\Users\cyber\Downloads\LostColony_WP\LostColony_WP.ark";


        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        var records = reader.ReadGameRecords(TestContext.CancellationToken);
        var actorTransforms = reader.ReadActorTransforms(TestContext.CancellationToken).Transforms;

        //10 wilds
        var wildRecords = records.Where(r => r.Value.IsCreature() && !r.Value.IsTamed()).Take(10).ToList();
        foreach( var wildRecord in wildRecords)
        {
            ActorTransform? wildTransform = actorTransforms.FirstOrDefault(t => t.Key == wildRecord.Value.Uuid).Value;
            var wildCreature = Creature.Create(wildRecord.Value, wildTransform);
            var wildStatusRef = wildRecord.Value.Properties.Get<ObjectProperty>("MyCharacterStatusComponent")?.Value?.ObjectId;
            if (wildStatusRef != null)
            {
                var wildStatusRecord = records[wildStatusRef.Value];
                wildCreature.IngestStatusRecord(wildStatusRecord);
            }
        }


        //stored wilds
        var storedWildRecords = records.Where(r => r.Value.IsCreature() && !r.Value.IsTamed() && r.Value.Properties.HasAny("IsStored")).Take(10).ToList();
        foreach (var storedRecord in storedWildRecords)
        {
            Guid containerId = storedRecord.Value.Uuid;
            if (storedRecord.Value.Properties.HasAny("IsStored"))
            {
                containerId = storedRecord.Value.Properties.Get<ObjectProperty>("CryoContainer").Value.ObjectId;
            }

            ActorTransform? tamedTransform = actorTransforms.FirstOrDefault(t => t.Key == containerId).Value;
            var storedWildCreature = Creature.Create(storedRecord.Value, tamedTransform);
            var storedStatusRef = storedRecord.Value.Properties.Get<ObjectProperty>("MyCharacterStatusComponent")?.Value?.ObjectId;
            if (storedStatusRef != null)
            {
                var tamedStatusRecord = records[storedStatusRef.Value];
                storedWildCreature.IngestStatusRecord(tamedStatusRecord);
            }


        }


        //10 tames
        var tamedRecords = records.Where(r => r.Value.IsCreature() && r.Value.IsTamed() && !r.Value.Properties.HasAny("IsStored")).Take(10).ToList();
        foreach (var tamedRecord in tamedRecords)
        {
            Guid containerId = tamedRecord.Value.Uuid;
            if (tamedRecord.Value.Properties.HasAny("IsStored"))
            {
                containerId = tamedRecord.Value.Properties.Get<ObjectProperty>("CryoContainer").Value.ObjectId;
            }

            ActorTransform? tamedTransform = actorTransforms.FirstOrDefault(t => t.Key == containerId).Value;
            var tamedCreature = Creature.Create(tamedRecord.Value, tamedTransform);
            var tamedStatusRef = tamedRecord.Value.Properties.Get<ObjectProperty>("MyCharacterStatusComponent")?.Value?.ObjectId;
            if (tamedStatusRef != null)
            {
                var tamedStatusRecord = records[tamedStatusRef.Value];
                tamedCreature.IngestStatusRecord(tamedStatusRecord);
            }
            var tamedInventoryRef = tamedRecord.Value.Properties.Get<ObjectProperty>("MyInventoryComponent")?.Value?.ObjectId;
            if (tamedInventoryRef != null)
            {
                var tamedInventoryRecord = records[tamedInventoryRef.Value];
                var inventoryItems = ReadInventory(TestContext.CancellationToken, records, tamedInventoryRecord);
                var creatureInventory = Inventory.Create(inventoryItems);
                tamedCreature.IngestInventory(creatureInventory);
            }
        }

        var storedTamedRecords = records.Where(r => r.Value.IsCreature() && r.Value.IsTamed() && r.Value.Properties.HasAny("IsStored")).ToList();
        foreach (var storedRecord in storedTamedRecords)
        {
            Guid containerId = storedRecord.Value.Uuid;
            if (storedRecord.Value.Properties.HasAny("IsStored"))
            {
                containerId = storedRecord.Value.Properties.Get<ObjectProperty>("CryoContainer").Value.ObjectId;
            }

            ActorTransform? tamedTransform = actorTransforms.FirstOrDefault(t => t.Key == containerId).Value;
            var tamedCreature = Creature.Create(storedRecord.Value, tamedTransform);
            var tamedStatusRef = storedRecord.Value.Properties.Get<ObjectProperty>("MyCharacterStatusComponent")?.Value?.ObjectId;
            if (tamedStatusRef != null)
            {
                var tamedStatusRecord = records[tamedStatusRef.Value];
                tamedCreature.IngestStatusRecord(tamedStatusRecord);
            }

            var storedInventoryRef = storedRecord.Value.Properties.Get<ObjectProperty>("MyInventoryComponent")?.Value?.ObjectId;
            if (storedInventoryRef != null)
            {
                var storedInventoryRecord = records[storedInventoryRef.Value];
                var inventoryItems = ReadInventory(TestContext.CancellationToken, records, storedInventoryRecord);
                var creatureInventory = Inventory.Create(inventoryItems);
                tamedCreature.IngestInventory(creatureInventory);
            }

        }

    }

    private List<Item> ReadInventory(CancellationToken cancellationToken, IReadOnlyDictionary<Guid,GameObjectRecord> gameObjects, GameObjectRecord inventoryRecord)
    {

        List<Item> inventoryItems = new List<Item>();

        //ItemSlots
        var itemSlots = inventoryRecord.Properties.Get<ArrayProperty>("ItemSlots")?.Value;
        if (itemSlots != null && itemSlots.Count > 0)
        {
            foreach (ObjectReference itemReference in itemSlots)
            {
                if (itemReference.ObjectId == Guid.Empty)
                {
                    continue; //skip empty slots
                }
                var itemRecord = gameObjects[itemReference.ObjectId];
                var item = Item.Create(itemRecord);
                inventoryItems.Add(item);
            }
        }

        //InventoryItems
        var inventorySlots = inventoryRecord.Properties.Get<ArrayProperty>("InventoryItems")?.Value;
        if (inventorySlots != null && inventorySlots.Count > 0)
        {
            foreach (ObjectReference itemReference in inventorySlots)
            {
                if (itemReference.ObjectId == Guid.Empty)
                {
                    continue; //skip empty slots
                }
                var itemRecord = gameObjects[itemReference.ObjectId];
                var item = Item.Create(itemRecord);
                inventoryItems.Add(item);
            }
        }

        //EquippedItems
        var equippedSlots = inventoryRecord.Properties.Get<ArrayProperty>("EquippedItems")?.Value;
        if (equippedSlots != null && equippedSlots.Count > 0)
        {
            foreach (ObjectReference itemReference in equippedSlots)
            {
                if (itemReference.ObjectId == Guid.Empty)
                {
                    continue; //skip empty slots
                }
                var itemRecord = gameObjects[itemReference.ObjectId];
                var item = Item.Create(itemRecord);
                inventoryItems.Add(item);
            }
        }

        return inventoryItems; 
    }

    [TestMethod]
    public void IsCryoRelease_DataMatched()
    {

        var cryoSaveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP_Cryo.ark");
        using var reader1 = new AsaSaveReader(cryoSaveFile, TestContext.GetLogger());
        var records1 = reader1.ReadGameRecords(TestContext.CancellationToken);


        var releaseSaveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP_Free.ark");
        using var reader2 = new AsaSaveReader(releaseSaveFile, TestContext.GetLogger());
        var records2 = reader2.ReadGameRecords(TestContext.CancellationToken);

        var freeCreatureRecord = records2[Guid.Parse(RexId)];
        Assert.IsNotNull(freeCreatureRecord, "Free creature cannot be found.");
        var freeCreature = Creature.Create(freeCreatureRecord, transform: null);
        
        Creature? cryoCreature = null;

        foreach (var record in records1.Values)
        {
            var dinoId = long.Parse($"{record.Properties.Get<uint>("DinoID1")}{record.Properties.Get<uint>("DinoID2")}");
            if(dinoId == freeCreature.DinoId)
            {
                cryoCreature = Creature.Create(record, transform: null);
                break;
            }
        }
        
        Assert.IsNotNull(cryoCreature, "Cryo creature cannot be found.");
        
        Assert.IsTrue(cryoCreature.IsCryo, "Cryo creature should have IsCryo=true");
        Assert.IsFalse(freeCreature.IsCryo, "Free creature should have IsCryo=false");
        Assert.AreEqual(cryoCreature.DinoId, freeCreature.DinoId, "DinoId should match between cryo and free versions of the same creature");
        Assert.AreEqual(cryoCreature.ClassName, freeCreature.ClassName, "ClassName should match between cryo and free versions of the same creature");       
        Assert.AreEqual(cryoCreature.BaseLevel, freeCreature.BaseLevel, "BaseLevel should match between cryo and free versions of the same creature");
    }

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
        _ = creature.IsFemale;
        _ = creature.BaseLevel;
        _ = creature.TotalLevel;
        _ = creature.MutationsMale;
        _ = creature.MutationsFemale;
        _ = creature.TotalMutations;
        _ = creature.BabyAge;
        _ = creature.IsJuvenile;
        _ = creature.DinoId;
        _ = creature.ColorRegions;
        _ = creature.ToString();

        if(creature is CreatureTamed tamed)
        {
            _ = tamed.TamedName;
            _ = tamed.TribeName;
            _ = tamed.TamerString;
            _ = tamed.ImprintQuality;
            _ = tamed.ImprinterName;
            _ = tamed.TamedAtGameTime;
        }

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
        _ = creature.IsFemale;
        _ = creature.BaseLevel;
        _ = creature.TotalLevel;
        _ = creature.MutationsMale;
        _ = creature.MutationsFemale;
        _ = creature.TotalMutations;
        _ = creature.BabyAge;
        _ = creature.IsJuvenile;
        _ = creature.DinoId;
        _ = creature.ColorRegions;
        _ = creature.ToString();

        if(creature is CreatureTamed tamed)
        {
            _ = tamed.TamedName;
            _ = tamed.TribeName;
            _ = tamed.TamerString;
            _ = tamed.ImprintQuality;
            _ = tamed.ImprinterName;
            _ = tamed.TamedAtGameTime;
        }
    }

    [TestMethod]
    public void AsaSaveGame_IncludesCryoCreatures()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);

        var cryoCreatures = saveGame.TamedCreatures.Values.Where(c=>c.IsCryo == true).ToList();
        TestContext.WriteLine($"Found {cryoCreatures.Count} cryo creatures in AsaSaveGame.");

        Assert.IsNotEmpty(cryoCreatures, "Expected extracted cryo creatures to be surfaced in AsaSaveGame.");

        foreach (var c in cryoCreatures.OfType<CreatureTamed>())
        {
            TestContext.WriteLine($"  {c.ClassName} '{c.TamedName}' lv={c.TotalLevel} tribe={c.TribeName} isInCryo={c.IsCryo}");
            Assert.IsTrue(c.IsCryo, $"Cryo creature {c.Id} should have IsInCryo=true");
        }
    }
}
