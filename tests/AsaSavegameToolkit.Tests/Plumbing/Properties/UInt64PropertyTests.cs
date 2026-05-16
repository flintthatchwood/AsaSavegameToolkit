using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class UInt64PropertyTests : SaveTests
{
    // UInt64Property == 74 85 93 B2

    [TestMethod]
    public void CanRead_Version13_UInt64Property()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "be2fb31d-487c-5bd6-e684-9999d4d751a5");
        archive.Position = 0x9E;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("UInt64Property", tag.Type.TypeName.FullName);

        var property = UInt64Property.Read(archive, tag);

        Assert.AreEqual("DroppedByPlayerID", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual(389727754u, property.Value);
    }

    [TestMethod]
    public void CanRead_Version14_UInt64Property()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "be2fb31d-487c-5bd6-e684-9999d4d751a5");
        archive.Position = 0x9F;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("UInt64Property", tag.Type.TypeName.FullName);

        var property = UInt64Property.Read(archive, tag);

        Assert.AreEqual("DroppedByPlayerID", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual(389727754u, property.Value);
    }
}
