using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Tests.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

namespace AsaSavegameToolkit.Tests.Plumbing.Records;

[TestClass]
public class GameObjectRecordTests : SaveTests
{
    [TestMethod]
    public void CanRead_Version13_GameObject()
    {
        // This is the example from GameObject.md docs
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        
        var uuid = Guid.Parse("fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        archive.Position = 0; // Start of file
        
        var gameObject = GameObjectRecord.Read(archive, uuid);
        
        Assert.IsNotNull(gameObject);
        Assert.AreEqual(uuid, gameObject.Uuid);
        Assert.Contains("Yutyrannus", gameObject.ClassName.FullName);
        Assert.AreEqual(0, gameObject.ClassName.InstanceNumber);
        Assert.HasCount(1, gameObject.Names);
        Assert.AreEqual("Yutyrannus_Character_BP_C_2145493141", gameObject.Names[0]);
        
        // Should have properties
        Assert.IsNotEmpty(gameObject.Properties);
    }
    
    [TestMethod]
    public void CanRead_Version14_GameObject()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        
        var uuid = Guid.Parse("fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        archive.Position = 0; // Start of file
        
        var gameObject = GameObjectRecord.Read(archive, uuid);
        
        Assert.IsNotNull(gameObject);
        Assert.AreEqual(uuid, gameObject.Uuid);
        Assert.Contains("Yutyrannus", gameObject.ClassName.FullName);
        Assert.AreEqual(0, gameObject.ClassName.InstanceNumber);
        Assert.HasCount(1, gameObject.Names);
        Assert.AreEqual("Yutyrannus_Character_BP_C_2145493141", gameObject.Names[0]);
        
        // Should have properties
        Assert.IsNotEmpty(gameObject.Properties);
    }

    [TestMethod]
    [DataRow((short)13, 0)]
    [DataRow((short)14, 0)]
    [DataRow((short)16, 0)]
    [DataRow((short)13, 5)]
    [DataRow((short)14, 5)]
    [DataRow((short)16, 5)]
    public void ClassNameMissingFromTheNameTable_ReadsAsUnresolved(short saveVersion, int instanceNumber)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(0x2F17FCD1); // class name index, not in the table
            writer.Write(instanceNumber); // its number
            writer.Write(0);          // unknown Int32
            writer.Write(1);          // one name
            writer.Write(11);         // its length, the terminator included
            writer.Write("Thing_C_42"u8.ToArray());
            writer.Write((byte)0);
            writer.Write(0);          // data file index
            writer.Write((byte)0x08); // actor
            if (saveVersion >= 14)
            {
                writer.Write((byte)0); // padding
            }
            writer.Write(1);          // property list: None
            writer.Write(0);
            writer.Write(0);          // no extra GUIDs
        }
        stream.Position = 0;
        using var archive = new AsaArchive(NullLogger.Instance, stream, "synthetic")
        {
            SaveVersion = saveVersion,
            NameTable = new Dictionary<int, string> { [1] = "None" },
        };

        var record = GameObjectRecord.Read(archive, Guid.NewGuid());

        Assert.IsFalse(record.ClassNameResolved);
        Assert.IsEmpty(record.ClassName.Name);
        Assert.IsEmpty(record.ClassName.FullName);
        Assert.AreEqual(0x2F17FCD1, record.ClassName.NameIndex);
        CollectionAssert.AreEqual(new[] { "Thing_C_42" }, record.Names.ToArray());
        Assert.IsEmpty(record.Properties);
        Assert.AreEqual(archive.Length, archive.Position);

        archive.NameTable[0x2F17FCD1] = "Test_C";
        archive.Position = 0;
        var resolvedRecord = GameObjectRecord.Read(archive, Guid.NewGuid());

        Assert.IsTrue(resolvedRecord.ClassNameResolved);
        Assert.AreEqual(instanceNumber == 0 ? "Test_C" : $"Test_C_{instanceNumber}", resolvedRecord.ClassName.FullName);
    }
}
