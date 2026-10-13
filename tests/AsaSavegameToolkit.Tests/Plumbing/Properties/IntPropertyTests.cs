using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class IntPropertyTests : SaveTests
{
    // IntProperty == 71 B2 FA F3

    [TestMethod]
    public void CanRead_Version13_IntProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "be2fb31d-487c-5bd6-e684-9999d4d751a5");
        archive.Position = 0xBF;

        var tag = PropertyTag.Read(archive);

        Assert.IsNotNull(tag);
        Assert.AreEqual("IntProperty", tag.Type.TypeName.FullName);

        var property = IntProperty.Read(archive, tag);

        Assert.AreEqual("TargetingTeam", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual(1472106223, property.Value);
    }

    [TestMethod]
    public void CanRead_Version14_IntProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "be2fb31d-487c-5bd6-e684-9999d4d751a5");
        archive.Position = 0xE1;

        var tag = PropertyTag.Read(archive);

        Assert.IsNotNull(tag);
        Assert.AreEqual("IntProperty", tag.Type.TypeName.FullName);

        var property = IntProperty.Read(archive, tag);

        Assert.AreEqual("TargetingTeam", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual(1472106223, property.Value);
    }
}
