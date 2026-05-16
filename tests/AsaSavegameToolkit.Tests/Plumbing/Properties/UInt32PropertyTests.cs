using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class UInt32PropertyTests : SaveTests
{
    // UInt32Property == 5B 9E 09 B5

    [TestMethod]
    public void CanRead_Verion13_UInt32Property()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "d2c94931-423d-a030-a433-45991b2614af");
        archive.Position = 0x133;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("UInt32Property", tag.Type.TypeName.FullName);

        var property = UInt32Property.Read(archive, tag);

        Assert.AreEqual("StructureID", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual(518781630u, property.Value);
    }

    [TestMethod]
    public void CanRead_Verion14_UInt32Property()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "d2c94931-423d-a030-a433-45991b2614af");
        archive.Position = 0x105;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("UInt32Property", tag.Type.TypeName.FullName);

        var property = UInt32Property.Read(archive, tag);

        Assert.AreEqual("StructureID", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual(518781630u, property.Value);
    }
}
