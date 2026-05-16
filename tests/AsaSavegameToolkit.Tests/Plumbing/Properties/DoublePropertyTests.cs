using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class DoublePropertyTests : SaveTests
{
    // DoubleProperty == 0D D9 8C 90

    [TestMethod]
    public void CanRead_Version13_DoubleProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "be2fb31d-487c-5bd6-e684-9999d4d751a5");
        archive.Position = 0xDC;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("DoubleProperty", tag.Type.TypeName.FullName);

        var property = DoubleProperty.Read(archive, tag);

        Assert.AreEqual("OriginalCreationTime", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual(102586123.91819647, property.Value);
    }

    [TestMethod]
    public void CanRead_Version14_DoubleProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "be2fb31d-487c-5bd6-e684-9999d4d751a5");
        archive.Position = 0xC0;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("DoubleProperty", tag.Type.TypeName.FullName);

        var property = DoubleProperty.Read(archive, tag);

        Assert.AreEqual("OriginalCreationTime", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual(102586123.91819647, property.Value);
    }
}
