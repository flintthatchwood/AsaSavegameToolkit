using AsaSavegameToolkit.Plumbing;
using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class PropertyReadTests
{
    [TestMethod]
    [DataRow((short)13)]
    [DataRow((short)14)]
    [DataRow((short)15)]
    public void Read_StructIncludesOnlyTheFormatSpecificGuid(short saveVersion)
    {
        using var archive = CreateStructArchive(saveVersion, 8, out var expectedEnd);

        var property = Assert.IsInstanceOfType<StructProperty>(Property.Read(archive));

        Assert.AreEqual("Coordinates", property.Tag.Name.FullName);
        var point = Assert.IsInstanceOfType<FIntPoint>(property.Value);
        Assert.AreEqual(123, point.X);
        Assert.AreEqual(456, point.Y);
        Assert.AreEqual(saveVersion < 14 ? (Guid?)Guid.Empty : null, property.StructGuid);
        Assert.AreEqual(expectedEnd, archive.Position);
        Assert.IsNull(Property.Read(archive));
        Assert.AreEqual(0x12345678, archive.ReadInt32());
    }

    [TestMethod]
    [DataRow((short)13, 4)]
    [DataRow((short)13, 12)]
    [DataRow((short)14, 4)]
    [DataRow((short)14, 12)]
    public void Read_WorldStructSizeMismatchStillThrowsAndResetsPosition(short saveVersion, int size)
    {
        using var archive = CreateStructArchive(saveVersion, size, out var expectedEnd);

        Assert.ThrowsExactly<AsaDataException>(() => Property.Read(archive));
        Assert.AreEqual(expectedEnd, archive.Position);
    }

    private static AsaArchive CreateStructArchive(short saveVersion, int size, out long expectedEnd)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            WriteName(writer, 0);
            WriteName(writer, 1);
            if (saveVersion >= 14)
            {
                writer.Write(1);
                WriteName(writer, 2);
                writer.Write(0);
                writer.Write(size);
                writer.Write((byte)0);
            }
            else
            {
                writer.Write(size);
                writer.Write(0);
                WriteName(writer, 2);
                writer.Write((byte)0);
                writer.Write(AsaArchive.ConvertToBytes(Guid.Empty));
            }

            expectedEnd = stream.Position + size;
            writer.Write(123);
            writer.Write(456);
            WriteName(writer, 3);
            writer.Write(0x12345678);
        }

        return new AsaArchive(NullLogger.Instance, stream.ToArray(), "synthetic-property")
        {
            SaveVersion = saveVersion,
            NameTable = new Dictionary<int, string>
            {
                [0] = "Coordinates",
                [1] = "StructProperty",
                [2] = "IntPoint",
                [3] = "None"
            }
        };
    }

    private static void WriteName(BinaryWriter writer, int index)
    {
        writer.Write(index);
        writer.Write(0);
    }
}
