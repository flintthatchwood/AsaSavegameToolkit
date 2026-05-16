using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Records;

[TestClass]
public class GameModeCustomBytesTests : SaveTests
{
    [TestMethod]
    public void CanRead_Version13_GameModeCustomBytes()
    {
        var savePath = "version_13/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "GameModeCustomBytes");

        var record = GameModeCustomBytesRecord.Read(archive);
    }

    [TestMethod]
    public void CanRead_Version14_GameModeCustomBytes()
    {
        var savePath = "version_14/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "GameModeCustomBytes");

        var record = GameModeCustomBytesRecord.Read(archive);
    }
}