using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class SetPropertyTests : SaveTests
{
    // SetProperty == B5 DF F7 4A

    [TestMethod]
    public void CanRead_Version13_SetProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "3b5743f3-44ca-3990-3996-60b9f2d395f6");
        archive.Position = 0x125;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("WirelessSources", tag.Name.FullName);
        Assert.AreEqual("SetProperty", tag.Type.TypeName.FullName);
        Assert.AreEqual(0, tag.ArrayIndex);

        var property = SetProperty.Read(archive, tag);

        Assert.IsNotEmpty(property.Value);
        Assert.IsInstanceOfType<ObjectReference>(property.Value[0]);
    }

    [TestMethod]
    public void CanRead_Version14_SetProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "3b5743f3-44ca-3990-3996-60b9f2d395f6");
        archive.Position = 0x143; // TODO: Find correct offset

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("WirelessSources", tag.Name.FullName);
        Assert.AreEqual("SetProperty", tag.Type.TypeName.FullName);
        Assert.AreEqual(0, tag.ArrayIndex);

        var property = SetProperty.Read(archive, tag);

        Assert.IsNotEmpty(property.Value);
        Assert.IsInstanceOfType<ObjectReference>(property.Value[0]);
    }
}
