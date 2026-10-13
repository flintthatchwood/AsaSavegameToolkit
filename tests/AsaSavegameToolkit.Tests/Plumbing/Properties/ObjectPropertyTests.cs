using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class ObjectPropertyTests : SaveTests
{
    // ObjectProperty == 3B DA 82 0C

    [TestMethod]
    public void CanRead_Version13_ObjectProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "be2fb31d-487c-5bd6-e684-9999d4d751a5");
        archive.Position = 0x49;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("ObjectProperty", tag.Type.TypeName.FullName);

        var property = ObjectProperty.Read(archive, tag);

        Assert.AreEqual("MyItem", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual("5a892c07-40bd-d1f2-95e9-7391de3c3bb8", property.Value.Value);
    }

    [TestMethod]
    public void CanRead_Version14_ObjectProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "be2fb31d-487c-5bd6-e684-9999d4d751a5");
        archive.Position = 0x4A;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("ObjectProperty", tag.Type.TypeName.FullName);

        var property = ObjectProperty.Read(archive, tag);

        Assert.AreEqual("MyItem", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual("5a892c07-40bd-d1f2-95e9-7391de3c3bb8", property.Value.Value);
    }
}
