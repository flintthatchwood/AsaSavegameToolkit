using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Records;

[TestClass]
public class SaveHeaderRecordTests : SaveTests
{
    [TestMethod]
    public void CanRead_Verion13_SaveHeader()
    {
        var savePath = "version_13/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "SaveHeader");

        var saveHeader = SaveHeaderRecord.Read(archive);
    }

    [TestMethod]
    public void CanRead_Verion14_SaveHeader()
    {
        var savePath = "version_14/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "SaveHeader");

        var saveHeader = SaveHeaderRecord.Read(archive);
    }

    [TestMethod]
    public void CanRead_Version13_GameTime()
    {
        var savePath = "version_13/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "SaveHeader");
        var saveHeader = SaveHeaderRecord.Read(archive);
        Assert.IsNotNull(saveHeader);
        Assert.AreEqual(102587656.7352575, saveHeader.GameTime, 0.0000001);
    }

    [TestMethod]
    public void CanRead_Version14_GameTime()
    {
        var savePath = "version_14/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "SaveHeader");
        var saveHeader = SaveHeaderRecord.Read(archive);
        Assert.IsNotNull(saveHeader);
        Assert.AreEqual(102587663.71728921, saveHeader.GameTime, 0.0000001);
    }
}
