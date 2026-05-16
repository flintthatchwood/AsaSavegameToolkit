using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class UInt16PropertyTests : SaveTests
{
    // UInt16Property == 5D 60 EB 5A

    [TestMethod]
    public void CanRead_Version13_UInt16Property()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "ee3dbd33-48fa-448b-cee8-efbc8654699c");
        archive.Position = 0x11E; // TODO: Find correct offset

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("UInt16Property", tag.Type.TypeName.FullName);

        var property = UInt16Property.Read(archive, tag);

        Assert.AreEqual("ItemStatValues", property.Tag.Name.FullName);
        Assert.AreEqual(2, property.Tag.ArrayIndex);
        Assert.AreEqual((ushort)14346, property.Value);
    }

    [TestMethod]
    public void CanRead_Version14_UInt16Property()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "ee3dbd33-48fa-448b-cee8-efbc8654699c");
        archive.Position = 0x156; // TODO: Find correct offset

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("UInt16Property", tag.Type.TypeName.FullName);

        var property = UInt16Property.Read(archive, tag);

        Assert.AreEqual("ItemStatValues", property.Tag.Name.FullName);
        Assert.AreEqual(2, property.Tag.ArrayIndex);
        Assert.AreEqual((ushort)14346, property.Value);
    }
}
