using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Tests.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class ObjectPropertyTests : SaveTests
{
    // ObjectProperty == 3B DA 82 0C

    [TestMethod]
    public void CanRead_Version13_ObjectProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "be2fb31d-487c-5bd6-e684-9999d4d751a5");
        archive.Position = 0x49;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("ObjectProperty", tag.Type.TypeName.FullName);

        var property = ObjectProperty.Read(archive, tag);

        Assert.AreEqual("MyItem", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual("5a892c07-40bd-d1f2-95e9-7391de3c3bb8", property.Value.Value);
    }

    [TestMethod]
    public void CanRead_Version14_ObjectProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "be2fb31d-487c-5bd6-e684-9999d4d751a5");
        archive.Position = 0x4A;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("ObjectProperty", tag.Type.TypeName.FullName);

        var property = ObjectProperty.Read(archive, tag);

        Assert.AreEqual("MyItem", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.AreEqual("5a892c07-40bd-d1f2-95e9-7391de3c3bb8", property.Value.Value);
    }

    [TestMethod]
    [DataRow((short)13)]
    [DataRow((short)14)]
    public void Cryopod_PathReference_IsAnInlineString_EvenWithANameTable(short saveVersion)
    {
        // from a cryopodded Gacha's ResourceProduction
        const string path = "BlueprintGeneratedClass /Game/Extinction/CoreBlueprints/Resources/PrimalItemResource_RedSap.PrimalItemResource_RedSap_C";
        var pathBytes = System.Text.Encoding.UTF8.GetBytes(path);

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(1);                     // path reference
            writer.Write(pathBytes.Length + 1);  // length incl. null terminator
            writer.Write(pathBytes);
            writer.Write((byte)0);
            writer.Write(0x12345678);            // next value
        }
        stream.Position = 0;
        using var archive = new AsaArchive(NullLogger.Instance, stream, "synthetic-cryo")
        {
            SaveVersion = saveVersion,
            IsCryopod = true,
            AllowDynamicNameTable = true,
            NameTable = new Dictionary<int, string> { [0] = "TribeName", [8] = "None" },
        };

        var reference = ObjectReference.Read(archive);

        Assert.IsTrue(reference.IsPath);
        Assert.AreEqual(path, reference.Value);
        Assert.AreEqual(stream.Length - 4, archive.Position, "should stop right after the path");
    }

    [TestMethod]
    [DataRow((short)13, new[] { 0, -1 }, -1, DisplayName = "v13: 0 then -1 (PlacedIceBridges element)")]
    [DataRow((short)13, new[] { 0, 1 }, 1, DisplayName = "v13: 0 then an index")]
    [DataRow((short)13, new[] { -1 }, -1, DisplayName = "v13: lone -1")]
    [DataRow((short)14, new[] { 0, -1 }, -1, DisplayName = "v14: 0 then -1 (PlacedIceBridges element)")]
    [DataRow((short)14, new[] { 0, 1 }, 1, DisplayName = "v14: 0 then an index")]
    [DataRow((short)14, new[] { -1 }, -1, DisplayName = "v14: lone -1")]
    public void Cryopod_ObjectReference_IsAnIndexNotAGuid(short saveVersion, int[] ints, int expectedIndex)
    {
        using var archive = CryoArchive(saveVersion, [.. ints, 0x12345678]); // then the next value

        var reference = ObjectReference.Read(archive);

        Assert.IsFalse(reference.IsPath);
        Assert.AreEqual(expectedIndex, reference.ObjectIndex);
        Assert.AreEqual(ints.Length * 4, archive.Position, "should stop right after the reference");
    }

    [TestMethod]
    [DataRow((short)13)]
    [DataRow((short)14)]
    public void Cryopod_ObjectArray_ReadsIndexes(short saveVersion)
    {
        // like PlacedIceBridges: count, then one reference per element
        using var archive = CryoArchive(saveVersion, [3, 0, -1, 0, 2, -1, 0x12345678]);
        var objectArray = FPropertyTypeName.Create(new FName(-1, 0, "ArrayProperty"), new FName(-1, 0, "ObjectProperty"));
        var tag = new PropertyTag { Name = new FName(-1, 0, "PlacedIceBridges"), Type = objectArray };

        var elements = ArrayProperty.ReadValue(archive, tag);

        CollectionAssert.AreEqual(new[] { -1, 2, -1 }, elements.Cast<ObjectReference>().Select(r => r.ObjectIndex).ToArray());
        Assert.AreEqual(0x12345678, archive.ReadInt32(), "should stop right after the last element");
    }

    private static AsaArchive CryoArchive(short saveVersion, int[] ints)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            foreach (var i in ints) writer.Write(i);
        }
        stream.Position = 0;
        return new AsaArchive(NullLogger.Instance, stream, "synthetic-cryo")
        {
            SaveVersion = saveVersion,
            IsCryopod = true,
        };
    }
}
