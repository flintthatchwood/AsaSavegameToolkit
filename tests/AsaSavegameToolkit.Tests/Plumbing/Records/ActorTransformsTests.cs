using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Records;

[TestClass]
public class ActorTransformsTests : SaveTests
{
    [TestMethod]
    public void CanRead_Version13_ActorTransformsRecord()
    {
        var savePath = "version_13/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "ActorTransforms");

        var actorTransforms = ActorTransformsRecord.Read(archive);
    }

    [TestMethod]
    public void CanRead_Version14_ActorTransformsRecord()
    {
        var savePath = "version_14/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "ActorTransforms");

        var actorTransforms = ActorTransformsRecord.Read(archive);
    }
}
