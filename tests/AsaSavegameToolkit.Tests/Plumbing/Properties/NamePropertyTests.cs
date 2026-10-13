using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class NamePropertyTests : SaveTests
{
    // NameProperty == C1 22 B7 8F

    [TestMethod]
    public void CanRead_Version13_NameProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "ffc30bf3-4fc2-38da-f295-4fb0a962b11e");
        archive.Position = 0x176;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("NameProperty", tag.Type.TypeName.FullName);

        var property = NameProperty.Read(archive, tag);

        Assert.AreEqual("CustomDataName", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual("Dino", property.Value.FullName);
    }

    [TestMethod]
    public void CanRead_Version14_NameProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "ffc30bf3-4fc2-38da-f295-4fb0a962b11e");
        archive.Position = 0x24BE;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("NameProperty", tag.Type.TypeName.FullName);

        var property = NameProperty.Read(archive, tag);

        Assert.AreEqual("CustomDataName", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual("Dino", property.Value.FullName);
    }
}
