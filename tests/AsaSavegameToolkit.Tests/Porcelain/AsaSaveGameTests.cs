using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

namespace AsaSavegameToolkit.Tests.Porcelain;

[TestClass]
public class AsaSaveGameTests : SaveTests
{
    [TestMethod]
    public void ReadFrom_DeathCachesAreExposedWithInventoriesOutsideStructures()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14", "ScorchedEarth_WP.ark");
        var save = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);

        Assert.HasCount(4, save.DeathItemCaches);
        Assert.IsFalse(save.Structures.Values.Any(s => s.Record!.IsDeathItemCache()));
        foreach (var (id, cache) in save.DeathItemCaches)
        {
            Assert.AreEqual(id, cache.Id);
            Assert.IsFalse(save.Structures.ContainsKey(id));
            Assert.IsNotNull(cache.Record);
            Assert.IsTrue(cache.Record.IsDeathItemCache());
            Assert.IsNotNull(cache.OwnerName);
            Assert.IsNotNull(cache.Location);
            Assert.IsNotNull(cache.Inventory);
            Assert.IsNotNull(cache.Inventory.Record);
            Assert.HasCount(cache.Record.Properties.Get<int>("CurrentItemCount"), cache.Inventory.Items);
            var references = cache.Inventory.Record.Properties.Get<ArrayProperty>("InventoryItems")!
                .Value.Cast<ObjectReference>().Select(r => r.ObjectId).ToArray();
            CollectionAssert.AreEquivalent(references, cache.Inventory.Items.Keys.ToArray());
            Assert.IsNotNull(cache.CreationTimestamp);
            Assert.IsNotNull(cache.ExpirationTimestamp);
            Assert.IsTrue(cache.ExpirationTimestamp > cache.CreationTimestamp);
            foreach (var item in cache.Inventory.Items.Values)
            {
                Assert.IsNotNull(item.Record);
                var owner = item.Record.Properties.Get<ObjectReference>("OwnerInventory");
                Assert.IsNotNull(owner);
                Assert.AreEqual(cache.Inventory.Id, owner.ObjectId);
                Assert.IsNull(item.Location);
            }
        }
        Assert.IsTrue(save.DeathItemCaches.Values.Any(c => c.TargetingTeam == 5));
        Assert.IsNotNull(save.Map);
        var raptorCache = save.DeathItemCaches[Guid.Parse("c8c54735-4e23-cdb0-ea51-5da503e24559")];
        Assert.AreEqual("Raptor - Lvl 120", raptorCache.OwnerName);
        Assert.HasCount(10, raptorCache.Inventory!.Items);
        Assert.IsNotNull(save.Map.GetCoordinates(raptorCache.Location));
    }

     [TestMethod]
    public void ReadFrom_Version13_CreaturesAreRead()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_13/TheIsland_WP.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);
        Assert.IsNotNull(saveGame.WildCreatures);
        Assert.IsNotEmpty(saveGame.WildCreatures);
    }

    [TestMethod]
    public void ReadFrom_Version14_CreaturesAreRead()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/TheIsland_WP.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);

        var mutated = saveGame.TamedCreatures.Values.Where(x => x.TotalMutations > 0).ToArray();
        Assert.IsNotEmpty(mutated);
        Assert.IsNotNull(saveGame.Map);
        Assert.AreEqual("theisland_wp", saveGame.Map.Name);
        Assert.AreEqual(6858.0, saveGame.Map.ScaleX);

        var locations = saveGame.TamedCreatures.Values.Select(c => c.Location)
            .Concat(saveGame.WildCreatures.Values.Select(c => c.Location))
            .Concat(saveGame.Players.Values.Select(p => p.Location))
            .Concat(saveGame.Structures.Values.Select(s => s.Location))
            .Concat(saveGame.DroppedItems.Values.Select(i => i.Location))
            .Where(location => location.HasValue)
            .ToArray();
        Assert.IsNotEmpty(locations);
        foreach (var location in locations)
        {
            Assert.IsNotNull(location);
            var coordinates = saveGame.Map.GetCoordinates(location);
            Assert.IsNotNull(coordinates);
            Assert.AreEqual(50 + location.Value.Y / 6858, coordinates.Value.Latitude, 1e-10);
            Assert.AreEqual(50 + location.Value.X / 6858, coordinates.Value.Longitude, 1e-10);
            Assert.AreEqual("default", coordinates.Value.Region?.Name);
        }
    }

    [TestMethod]
    public void ReadFrom_CanReadAllSaves()
    {
        var allSaves = Directory.GetFiles(TestSettings.AssetsDirectory, "*.ark", SearchOption.AllDirectories);
        foreach (var saveFile in allSaves)
        {
            try
            {
                var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);
            }
            catch
            {
                TestContext.WriteLine($"Failed to read save file '{saveFile}'");
                throw;
            }
        }
    }

    [TestMethod]
    public void CanRead_Version13_GameTime()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_13/TheIsland_WP.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);
        Assert.AreEqual(2587656.7352575, saveGame.GameTime.TotalSeconds, 0.0000001);
    }

    [TestMethod]
    public void ReadFrom_MapOverridesAreApplied()
    {
        var settings = new AsaReaderSettings
        {
            MapOverrides =
            [
                new MapDefinition
                {
                    Name = "THEISLAND_WP", CenterX = 1234, CenterY = -4567, ScaleX = 1000, ScaleY = 2000,
                    Regions = [new MapRegion { Name = "Override" }]
                }
            ]
        };
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14", "TheIsland_WP.ark");

        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), settings, TestContext.CancellationToken);

        Assert.IsNotNull(saveGame.Map);
        Assert.AreEqual("THEISLAND_WP", saveGame.Map.Name);
        Assert.AreEqual(1000.0, saveGame.Map.ScaleX);
        Assert.AreEqual(2000.0, saveGame.Map.ScaleY);
        var location = saveGame.WildCreatures.Values.First(c => c.Location.HasValue).Location;
        Assert.IsNotNull(location);
        var coordinates = saveGame.Map.GetCoordinates(location);
        Assert.IsNotNull(coordinates);
        Assert.AreEqual(50 + (location.Value.Y + 4567) / 2000, coordinates.Value.Latitude, 1e-10);
        Assert.AreEqual(50 + (location.Value.X - 1234) / 1000, coordinates.Value.Longitude, 1e-10);
        Assert.AreEqual("Override", coordinates.Value.Region?.Name);
    }

    [TestMethod]
    public void CanRead_Version14_GameTime()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/TheIsland_WP.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);
        Assert.AreEqual(2587663.71728921, saveGame.GameTime.TotalSeconds, 0.0000001);
    }

    [TestMethod]
    public void NestComponents_KeepsObjectsThatShareAName()
    {
        // seen in real saves: LadderBP structures and InstancedFoliageActors sharing a name
        var ladderA = Record("LadderBP_GEN_VARIABLE_LadderBP_C_CAT_5");
        var ladderB = Record("LadderBP_GEN_VARIABLE_LadderBP_C_CAT_5");
        var bed = Record("Bed_1");
        var bedInventory = Record("PrimalInventory", "Bed_1");
        var ladderInventory = Record("PrimalInventory", "LadderBP_GEN_VARIABLE_LadderBP_C_CAT_5");

        var topLevel = AsaSaveGame.NestComponents([ladderA, ladderB, bed, bedInventory, ladderInventory], NullLogger.Instance);

        CollectionAssert.AreEquivalent(new[] { ladderA, ladderB, bed }, topLevel);
        Assert.AreSame(bedInventory, bed.Components["PrimalInventory"]);
        Assert.AreEqual(0, ladderA.Components.Count + ladderB.Components.Count, "the parent of a shared name is ambiguous");
    }

    [TestMethod]
    public void ReadFrom_RaftIsAStructureNotACreature()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/TheIsland_WP.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);

        Assert.IsFalse(saveGame.TamedCreatures.Values.Any(c => c.ClassName.EndsWith(".Raft_BP_C")));
        var raft = saveGame.Structures.Values.Single(s => s.ClassName.EndsWith(".Raft_BP_C"));
        Assert.IsTrue(raft.IsPlayerBuilt);
    }

    private static GameObjectRecord Record(params string[] names) =>
        new(Guid.NewGuid(), new FName(-1, 0, "/Game/Test/Test.Test_C"), names, [], 0, ObjectTypeFlags.None, extraGuids: []);
}
