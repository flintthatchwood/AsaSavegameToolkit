using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class FloatPropertyTests : SaveTests
{
    // FloatProperty == 3A 23 FD 69

    [TestMethod]
    public void CanRead_Version13_FloatProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "d2c94931-423d-a030-a433-45991b2614af");
        archive.Position = 0x150;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("FloatProperty", tag.Type.ToString());

        var property = FloatProperty.Read(archive, tag);

        Assert.AreEqual("MaxHealth", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual(100f, property.Value);
    }

    [TestMethod]
    public void CanRead_Version14_FloatProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "d2c94931-423d-a030-a433-45991b2614af");
        archive.Position = 0x13B;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("FloatProperty", tag.Type.ToString());

        var property = FloatProperty.Read(archive, tag);

        Assert.AreEqual("MaxHealth", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual(100f, property.Value);
    }
}
