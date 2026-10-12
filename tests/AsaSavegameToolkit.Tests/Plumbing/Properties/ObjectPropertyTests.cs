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
}
