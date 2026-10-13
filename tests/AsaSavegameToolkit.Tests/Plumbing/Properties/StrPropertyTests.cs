using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class StrPropertyTests : SaveTests
{
    // StrProperty == 80 FF 9F A8

    [TestMethod]
    public void CanRead_Version13_StrProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "be2fb31d-487c-5bd6-e684-9999d4d751a5");
        archive.Position = 0x74;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("StrProperty", tag.Type.TypeName.FullName);

        var property = StrProperty.Read(archive, tag);

        Assert.AreEqual("DroppedByName", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual("U1 - Lvl 203", property.Value);
    }

    [TestMethod]
    public void CanRead_Version14_StrProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "be2fb31d-487c-5bd6-e684-9999d4d751a5");
        archive.Position = 0x75;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("StrProperty", tag.Type.TypeName.FullName);

        var property = StrProperty.Read(archive, tag);

        Assert.AreEqual("DroppedByName", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual("U1 - Lvl 203", property.Value);
    }
}
