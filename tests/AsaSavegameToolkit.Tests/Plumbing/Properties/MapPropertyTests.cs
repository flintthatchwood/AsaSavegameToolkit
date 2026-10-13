using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class MapPropertyTests : SaveTests
{
    // MapProperty == 65 15 1E 9B

    [TestMethod]
    public void CanRead_Version13_MapProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "3b5743f3-44ca-3990-3996-60b9f2d395f6");
        archive.Position = 0xEC;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);
        Assert.AreEqual("MapProperty<NameProperty, StructProperty>", tag.Type.ToString());
        Assert.AreEqual("WirelessExchangeRefs", tag.Name.FullName);
        Assert.AreEqual(0, tag.ArrayIndex);

        var property = MapProperty.Read(archive, tag);
        Assert.IsNotNull(property.Value);
        Assert.HasCount(1, property.Value);
    }

    [TestMethod]
    public void CanRead_Version14_MapProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "3b5743f3-44ca-3990-3996-60b9f2d395f6");
        archive.Position = 0xEA;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);
        Assert.AreEqual("MapProperty<NameProperty, StructProperty<PrimalWirelessReferences</Script/ShooterGame>>>", tag.Type.ToString());
        Assert.AreEqual("WirelessExchangeRefs", tag.Name.FullName);
        Assert.AreEqual(0, tag.ArrayIndex);

        var property = MapProperty.Read(archive, tag);
        Assert.IsNotNull(property.Value);
        Assert.HasCount(1, property.Value);
    }
}
