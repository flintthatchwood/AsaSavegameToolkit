using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class Int8PropertyTests : SaveTests
{
    // Int8Property == F2 75 A8 38
    
    [TestMethod]
    public void CanRead_Version13_Int8Property()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "185d191a-45d4-4896-fcd7-90b0695b7709");
        archive.Position = 0x54;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("Int8Property", tag.Type.TypeName.FullName);

        var property = Int8Property.Read(archive, tag);

        Assert.AreEqual("DoorOpenState", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual((sbyte)1, property.Value);
    }

    [TestMethod]
    public void CanRead_Version14_Int8Property()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "185d191a-45d4-4896-fcd7-90b0695b7709");
        archive.Position = 0x3B;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("Int8Property", tag.Type.TypeName.FullName);

        var property = Int8Property.Read(archive, tag);

        Assert.AreEqual("DoorOpenState", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual((sbyte)1, property.Value);
    }
}
