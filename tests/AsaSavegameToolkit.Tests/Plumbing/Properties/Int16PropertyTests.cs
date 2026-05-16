using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class Int16PropertyTests : SaveTests
{
    // Int16Property == B5 75 0B A0

    [TestMethod]
    public void CanRead_Version13_Int16Property()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "ee3dbd33-48fa-448b-cee8-efbc8654699c");
        archive.Position = 0x156; // TODO: Find correct offset

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("Int16Property", tag.Type.TypeName.FullName);

        var property = Int16Property.Read(archive, tag);

        Assert.AreEqual("ItemColorID", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual((short)106, property.Value);
    }

    [TestMethod]
    public void CanRead_Version14_Int16Property()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "ee3dbd33-48fa-448b-cee8-efbc8654699c");
        archive.Position = 0x13B; // TODO: Find correct offset

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("Int16Property", tag.Type.TypeName.FullName);

        var property = Int16Property.Read(archive, tag);

        Assert.AreEqual("ItemColorID", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual((short)106, property.Value);
    }
}
