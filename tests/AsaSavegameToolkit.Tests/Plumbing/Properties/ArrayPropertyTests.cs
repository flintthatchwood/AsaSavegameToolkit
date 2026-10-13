using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class ArrayPropertyTests : SaveTests
{
    // ArrayProperty == 6B C5 FD 58

    [TestMethod]
    public void CanRead_Version13_ArrayOfGenericStruct()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        archive.Position = 0x4FB;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("DinoAncestors", tag.Name.FullName);
        Assert.AreEqual("ArrayProperty", tag.Type.TypeName.FullName);
        Assert.AreEqual("StructProperty", tag.Type.Parameters[0].TypeName.FullName);
        Assert.AreEqual(0, tag.ArrayIndex);

        var property = ArrayProperty.Read(archive, tag);
        Assert.AreEqual("DinoAncestorsEntry", property.Tag.Type.Parameters[0].Parameters[0].TypeName.FullName);

        Assert.HasCount(3, property.Value);
    }

    [TestMethod]
    public void CanRead_Version14_ArrayOfGenericStruct()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        archive.Position = 0x2EF;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("DinoAncestorsMale", tag.Name.FullName);
        Assert.AreEqual("ArrayProperty", tag.Type.TypeName.FullName);
        Assert.AreEqual("StructProperty", tag.Type.Parameters[0].TypeName.FullName);
        Assert.AreEqual("DinoAncestorsEntry", tag.Type.Parameters[0].Parameters[0].TypeName.FullName);
        Assert.AreEqual(0, tag.ArrayIndex);

        var property = ArrayProperty.Read(archive, tag);
        Assert.HasCount(3, property.Value);
    }

    [TestMethod]
    public void CanRead_Version13_ArrayOfByte()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "c5107cf9-4568-21aa-6eca-86b5207afcb7");
        archive.Position = 0x8E;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("DinoData", tag.Name.FullName);
        Assert.AreEqual("ArrayProperty", tag.Type.TypeName.FullName);
        Assert.AreEqual("ByteProperty", tag.Type.Parameters[0].TypeName.FullName);
        Assert.AreEqual(0, tag.ArrayIndex);
        Assert.AreEqual(727, tag.Size);

        var property = ArrayProperty.Read(archive, tag);
        var typedValue = Assert.IsInstanceOfType<byte[]>(property.Value);
        Assert.HasCount(723, typedValue);
    }

    [TestMethod]
    public void CanRead_Version14_ArrayOfByte()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "c5107cf9-4568-21aa-6eca-86b5207afcb7");
        archive.Position = 0x6C;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("DinoData", tag.Name.FullName);
        Assert.AreEqual("ArrayProperty", tag.Type.TypeName.FullName);
        Assert.AreEqual("ByteProperty", tag.Type.Parameters[0].TypeName.FullName);
        Assert.AreEqual(0, tag.ArrayIndex);
        Assert.AreEqual(727, tag.Size);

        var property = ArrayProperty.Read(archive, tag);
        var typedValue = Assert.IsInstanceOfType<byte[]>(property.Value);
        Assert.HasCount(723, typedValue);
    }

    [TestMethod]
    public void CanRead_Version13_ArrayOfKnownStruct()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "d2c94931-423d-a030-a433-45991b2614af");
        archive.Position = 0x63;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("FeatherColors", tag.Name.FullName);
        Assert.AreEqual("ArrayProperty", tag.Type.TypeName.FullName);
        Assert.AreEqual("StructProperty", tag.Type.Parameters[0].TypeName.FullName);
        Assert.AreEqual(0, tag.ArrayIndex);
        Assert.AreEqual(149, tag.Size);

        var property = ArrayProperty.Read(archive, tag);
        // We only know the inner type of a v13 struct property after we start reading it.
        Assert.AreEqual("LinearColor", property.Tag.Type.Parameters[0].Parameters[0].TypeName.FullName);

        Assert.HasCount(6, property.Value);
        var typedValue = Assert.IsInstanceOfType<FLinearColor>(property.Value[0]);
    }

    [TestMethod]
    public void CanRead_Version14_ArrayOfKnownStruct()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "d2c94931-423d-a030-a433-45991b2614af");
        archive.Position = 0x64;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("FeatherColors", tag.Name.FullName);
        Assert.AreEqual("ArrayProperty", tag.Type.TypeName.FullName);
        Assert.AreEqual("StructProperty", tag.Type.Parameters[0].TypeName.FullName);
        Assert.AreEqual("LinearColor", tag.Type.Parameters[0].Parameters[0].TypeName.FullName);
        Assert.AreEqual(0, tag.ArrayIndex);
        Assert.AreEqual(100, tag.Size);

        var property = ArrayProperty.Read(archive, tag);
        Assert.HasCount(6, property.Value);
        var typedValue = Assert.IsInstanceOfType<FLinearColor>(property.Value[0]);
    }
}
