using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Porcelain;

[TestClass]
public class PlayerTests : SaveTests
{
    [TestMethod]
    public void CanParseAsPlayer()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP.ark");
        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        var records = reader.ReadGameRecords(TestContext.CancellationToken);

        var profileRecords = records.Where(r => r.Value.IsProfile()).ToList();
        Assert.IsNotNull(profileRecords);
        Assert.IsNotEmpty(profileRecords);

        var firstProfile = profileRecords[0].Value;
        var player = Player.Create(firstProfile, null);
        Assert.IsNotNull(player);
        Assert.IsNotNull(player.PlayerName);
        Assert.IsGreaterThanOrEqualTo(player.Level, 1);
        Assert.IsNotNull(player.Stats);
        Assert.HasCount(12, player.Stats);
    }

    [TestMethod]
    public void ReadFrom_PopulatesTribesAndPlayers()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/TheIsland_WP.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);

        Assert.IsNotNull(saveGame.Players);
        Assert.IsNotNull(saveGame.Tribes);
        Assert.IsNotNull(saveGame.SaveFilename);
        Assert.IsTrue(saveGame.SaveTimestamp <= DateTime.UtcNow);
    }

    [TestMethod]
    public void ReadFrom_GameModeCustomBytes_PopulatesTribes()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP_GameModCustomBytes.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);

        Assert.IsNotNull(saveGame.Tribes);
        Assert.IsNotEmpty(saveGame.Tribes);

        var tribe = saveGame.Tribes.Values.First();
        Assert.IsNotNull(tribe.TribeName);
        Assert.IsNotNull(tribe.MemberNames);
        Assert.IsNotNull(tribe.MemberIds);
    }

    [TestMethod]
    public void Item_ItemId_BitshiftsCorrectly()
    {
        uint id1 = 12345;
        uint id2 = 67890;
        long expected = ((long)id1 << 32) | id2;

        var ancestry = new Ancestry(0, expected, "Father", expected + 1, "Mother");
        Assert.AreEqual(0, ancestry.TreeDepth);
        Assert.AreEqual(expected, ancestry.MaleId);
        Assert.AreEqual("Father", ancestry.MaleName);
        Assert.AreEqual(expected + 1, ancestry.FemaleId);
        Assert.AreEqual("Mother", ancestry.FemaleName);
    }
}
