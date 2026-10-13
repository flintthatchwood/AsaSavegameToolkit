using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class BytePropertyTests : SaveTests
{
    // ByteProperty == B2 B4 4F 96

    [TestMethod]
    public void CanRead_Verion13_ByteProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        archive.Position = 0x112;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);
        Assert.AreEqual("ColorSetIndices", tag.Name.FullName);
        Assert.AreEqual("ByteProperty", tag.Type.ToString());
        Assert.AreEqual(5, tag.ArrayIndex);

        var property = ByteProperty.Read(archive, tag);

        var byteProperty = Assert.IsInstanceOfType<ByteProperty>(property);

        Assert.AreEqual(143, byteProperty.Value);
    }

    [TestMethod]
    public void CanRead_Verion14_ByteProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        archive.Position = 0x9E6;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);
        Assert.AreEqual("ColorSetIndices", tag.Name.FullName);
        Assert.AreEqual("ByteProperty", tag.Type.ToString());
        Assert.AreEqual(5, tag.ArrayIndex);

        var property = ByteProperty.Read(archive, tag);

        var byteProperty = Assert.IsInstanceOfType<ByteProperty>(property);
        Assert.AreEqual(143, byteProperty.Value);
    }
    
    [TestMethod]
    public void CanRead_Verion13_ByteProperty_Enum()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        archive.Position = 0x442;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);
        Assert.AreEqual("BabyCuddleType", tag.Name.FullName);
        Assert.AreEqual("ByteProperty", tag.Type.TypeName.ToString());
        Assert.AreEqual(0, tag.ArrayIndex);
        Assert.AreEqual(1, tag.Type.ParameterCount);
        Assert.AreEqual("EBabyCuddleType", tag.Type.Parameters[0].TypeName.ToString());

        var property = ByteProperty.Read(archive, tag);

        var byteProperty = Assert.IsInstanceOfType<ByteEnumProperty>(property);
        Assert.AreEqual("EBabyCuddleType::FOOD", byteProperty.Value.ToString());
    }

    [TestMethod]
    public void CanRead_Verion14_ByteProperty_Enum()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        archive.Position = 0x957;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);
        Assert.AreEqual("BabyCuddleType", tag.Name.FullName);
        Assert.AreEqual("ByteProperty", tag.Type.TypeName.ToString());
        Assert.AreEqual(0, tag.ArrayIndex);
        Assert.AreEqual(1, tag.Type.ParameterCount);
        Assert.AreEqual("EBabyCuddleType", tag.Type.Parameters[0].TypeName.ToString());

        var property = ByteProperty.Read(archive, tag);

        var byteProperty = Assert.IsInstanceOfType<ByteEnumProperty>(property);
        Assert.AreEqual("EBabyCuddleType::FOOD", byteProperty.Value.ToString());
    }
}
