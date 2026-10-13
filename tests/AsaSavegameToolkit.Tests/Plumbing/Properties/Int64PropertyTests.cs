using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class Int64PropertyTests : SaveTests
{
    // Int64Property == 9C 90 73 48

    [TestMethod]
    public void CanRead_Version13_Int64Property()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "e8559986-4f00-de4a-d3bf-20bc21ebe371");
        archive.Position = 0x25A;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("Int64Property", tag.Type.TypeName.FullName);

        var property = Int64Property.Read(archive, tag);

        Assert.AreEqual("CustomCosmeticModSkinReplacementID", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual(974054, property.Value);
    }

    [TestMethod]
    public void CanRead_Version14_Int64Property()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "e8559986-4f00-de4a-d3bf-20bc21ebe371");
        archive.Position = 0x176;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("Int64Property", tag.Type.TypeName.FullName);

        var property = Int64Property.Read(archive, tag);

        Assert.AreEqual("CustomCosmeticModSkinReplacementID", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual(974054, property.Value);
    }
}
