using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

namespace AsaSavegameToolkit.Tests.Porcelain;

[TestClass]
public class AsaSaveGameTests : SaveTests
{
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

    private static GameObjectRecord Record(params string[] names) =>
        new(Guid.NewGuid(), new FName(-1, 0, "/Game/Test/Test.Test_C"), names, [], 0, ObjectTypeFlags.None, extraGuids: []);
}
