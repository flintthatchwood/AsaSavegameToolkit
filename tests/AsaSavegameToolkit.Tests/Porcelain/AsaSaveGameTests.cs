using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;

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
}
