using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class StructPropertyTests : SaveTests
{
    [TestMethod]
    [DataRow((short)13, false)]
    [DataRow((short)14, false)]
    [DataRow((short)15, false)]
    [DataRow((short)7, true)]
    [DataRow((short)14, true)]
    public void Read_UsesFormatSpecificGuidWithoutConsumingFollowingData(short saveVersion, bool isArkFile)
    {
        var guid = Guid.Parse("54c8647e-4dd4-4cb4-bd28-a87ab450cc06");
        var type = FPropertyTypeName.Create(
            new FName(-1, 0, "StructProperty"),
            new FName(-1, 0, "IntPoint"),
            new FName(-1, 0, guid.ToString()));
        var tag = new PropertyTag { Name = new FName(-1, 0, "Coordinates"), Type = type, Size = 8 };

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            if (!isArkFile && saveVersion < 14)
                writer.Write(AsaArchive.ConvertToBytes(guid));
            writer.Write(123);
            writer.Write(456);
            writer.Write(0x12345678);
        }
        stream.Position = 0;
        using var archive = new AsaArchive(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, stream, "synthetic-struct")
        {
            SaveVersion = saveVersion,
            IsArkFile = isArkFile
        };

        var property = StructProperty.Read(archive, tag);

        Assert.AreSame(tag, property.Tag);
        Assert.AreEqual(isArkFile ? (Guid?)null : guid, property.StructGuid);
        var value = Assert.IsInstanceOfType<FIntPoint>(property.Value);
        Assert.AreEqual(123, value.X);
        Assert.AreEqual(456, value.Y);
        Assert.AreEqual(stream.Length - 4, archive.Position);
        Assert.AreEqual(0x12345678, archive.ReadInt32());
    }

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
    public void StructProperty_CanRead_IntPoint()
    {
        var type = FPropertyTypeName.Create(new FName(-1, 0, "StructProperty"), new FName(-1, 0, "IntPoint"));
        var tag = new PropertyTag { Name = new FName(-1, 0, "Coordinates"), Type = type, Size = 8 };

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(123);
            writer.Write(456);
        }
        stream.Position = 0;
        using var archive = new AsaArchive(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, stream, "synthetic")
        {
            SaveVersion = 14
        };

        var value = Assert.IsInstanceOfType<FIntPoint>(StructProperty.ReadValue(archive, tag));
        Assert.AreEqual(123, value.X);
        Assert.AreEqual(456, value.Y);
        Assert.AreEqual("IntPoint(123, 456)", value.ToString());
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
