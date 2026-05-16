using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class BoolPropertyTests : SaveTests
{
    // BoolProperty == AA 26 82 4D

    [TestMethod]
    public void CanRead_Version13_BoolProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "feecca8b-4859-3347-cf62-c5ae4d0a92f8");
        archive.Position = 0x3C2;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("bIgnoreNPCCountVolumes", tag.Name.NameString);
        Assert.AreEqual("BoolProperty", tag.Type.TypeName.NameString);
        Assert.AreEqual(0, tag.ArrayIndex);

        var property = BoolProperty.Read(archive, tag);
        Assert.IsTrue(property.Value);
    }

    [TestMethod]
    public void CanRead_Version14_BoolProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "feecca8b-4859-3347-cf62-c5ae4d0a92f8");
        archive.Position = 0x3B2;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("bIgnoreNPCCountVolumes", tag.Name.NameString);
        Assert.AreEqual("BoolProperty", tag.Type.TypeName.NameString);
        Assert.AreEqual(0, tag.ArrayIndex);

        var property = BoolProperty.Read(archive, tag);
        Assert.IsTrue(property.Value);
    }
}
