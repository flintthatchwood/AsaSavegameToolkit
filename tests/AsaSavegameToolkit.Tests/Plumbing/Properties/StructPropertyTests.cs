using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class StructPropertyTests : SaveTests
{
    // StructProperty == 70 C6 A4 FA

    [TestMethod]
    public void CanRead_Version13_StructProperty_Generic()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "ee3dbd33-48fa-448b-cee8-efbc8654699c");
        archive.Position = 0x41;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("ItemID", tag.Name.FullName);
        Assert.AreEqual("StructProperty", tag.Type.TypeName.ToString());
        Assert.AreEqual("ItemNetID", tag.Type.Parameters[0].TypeName.ToString());
        Assert.AreEqual(0, tag.ArrayIndex);

        var property = StructProperty.Read(archive, tag);
        var properties = Assert.IsInstanceOfType<List<Property>>(property.Value);
        Assert.HasCount(2, properties);

        var first = Assert.IsInstanceOfType<UInt32Property>(properties[0]);
        Assert.AreEqual("ItemID1", first.Tag.Name.FullName);
        Assert.AreEqual(339304800u, first.Value);

        var second = Assert.IsInstanceOfType<UInt32Property>(properties[1]);
        Assert.AreEqual("ItemID2", second.Tag.Name.FullName);
        Assert.AreEqual(111026336u, second.Value);
    }

    [TestMethod]
    public void CanRead_Version14_StructProperty_Generic()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "ee3dbd33-48fa-448b-cee8-efbc8654699c");
        archive.Position = 0x42;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("ItemID", tag.Name.FullName);
        Assert.AreEqual("StructProperty", tag.Type.TypeName.ToString());
        Assert.AreEqual("ItemNetID", tag.Type.Parameters[0].TypeName.ToString());
        Assert.AreEqual(0, tag.ArrayIndex);

        var property = StructProperty.Read(archive, tag);
        var properties = Assert.IsInstanceOfType<List<Property>>(property.Value);
        Assert.HasCount(2, properties);

        var first = Assert.IsInstanceOfType<UInt32Property>(properties[0]);
        Assert.AreEqual("ItemID1", first.Tag.Name.FullName);
        Assert.AreEqual(339304800u, first.Value);

        var second = Assert.IsInstanceOfType<UInt32Property>(properties[1]);
        Assert.AreEqual("ItemID2", second.Tag.Name.FullName);
        Assert.AreEqual(111026336u, second.Value);
    }

    [TestMethod]
    public void CanRead_Version13_StructProperty_Quat()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "feecca8b-4859-3347-cf62-c5ae4d0a92f8");
        archive.Position = 0x471;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("CharacterSavedDynamicBaseRelativeRotation", tag.Name.FullName);
        Assert.AreEqual("StructProperty", tag.Type.TypeName.ToString());
        Assert.AreEqual("Quat", tag.Type.Parameters[0].TypeName.ToString());
        Assert.AreEqual(0, tag.ArrayIndex);

        var property = StructProperty.Read(archive, tag);
        var value = Assert.IsInstanceOfType<FQuat>(property.Value);

        Assert.AreEqual(0, value.X);
        Assert.AreEqual(0, value.Y);
        Assert.AreEqual(0, value.Z);
        Assert.AreEqual(1, value.W);
    }

    [TestMethod]
    public void CanRead_Version14_StructProperty_Quat()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "feecca8b-4859-3347-cf62-c5ae4d0a92f8");
        archive.Position = 0x3CB;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("CharacterSavedDynamicBaseRelativeRotation", tag.Name.FullName);
        Assert.AreEqual("StructProperty", tag.Type.TypeName.ToString());
        Assert.AreEqual("Quat", tag.Type.Parameters[0].TypeName.ToString());
        Assert.AreEqual(0, tag.ArrayIndex);

        var property = StructProperty.Read(archive, tag);
        var value = Assert.IsInstanceOfType<FQuat>(property.Value);

        Assert.AreEqual(0, value.X);
        Assert.AreEqual(0, value.Y);
        Assert.AreEqual(0, value.Z);
        Assert.AreEqual(1, value.W);
    }
}
