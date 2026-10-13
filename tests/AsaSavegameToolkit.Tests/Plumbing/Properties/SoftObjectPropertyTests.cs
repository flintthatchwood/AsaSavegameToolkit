using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Tests.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class SoftObjectPropertyTests : SaveTests
{
    // SoftObjectProperty == 10 21 84 ED

    [TestMethod]
    public void CanRead_Version13_SoftObjectProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "2ea18a1e-4e31-9cee-e925-a1937e4dd3fa");
        archive.Position = 0xAF5;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("SoftObjectProperty", tag.Type.TypeName.FullName);

        var property = SoftObjectProperty.Read(archive, tag);

        Assert.AreEqual("SavedSleepAnim", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.HasCount(1, property.Value);
        Assert.AreEqual("/Game/PrimalEarth/Human/Male/Animations/ASA/TPV/Ragdoll_Deaths/HM_TPV_Idle_Death_Ctr_05.HM_TPV_Idle_Death_Ctr_05", property.Value[0]);
    }

    [TestMethod]
    public void CanRead_Version14_SoftObjectProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "2ea18a1e-4e31-9cee-e925-a1937e4dd3fa");
        archive.Position = 0x18E;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);

        Assert.AreEqual("SoftObjectProperty", tag.Type.TypeName.FullName);

        var property = SoftObjectProperty.Read(archive, tag);

        Assert.AreEqual("SavedSleepAnim", property.Tag.Name.FullName);
        Assert.AreEqual(0, property.Tag.ArrayIndex);
        Assert.HasCount(1, property.Value);
        Assert.AreEqual("/Game/PrimalEarth/Human/Male/Animations/ASA/TPV/Ragdoll_Deaths/HM_TPV_Idle_Death_Ctr_05.HM_TPV_Idle_Death_Ctr_05", property.Value[0]);
    }

    [TestMethod]
    [DataRow((short)13, 0)]
    [DataRow((short)14, 0)]
    [DataRow((short)16, 0)]
    [DataRow((short)13, 7)]
    [DataRow((short)14, 7)]
    [DataRow((short)16, 7)]
    public void NameMissingFromTheNameTable_ReadsAsEmpty(short saveVersion, int instanceNumber)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(2);          // a name in the table
            writer.Write(2);          // preserve the resolved name's instance number
            writer.Write(0x2F17FCD1); // a name index not in the table
            writer.Write(instanceNumber);
            writer.Write(0);          // terminator
            writer.Write(0x12345678);
        }
        stream.Position = 0;
        using var archive = new AsaArchive(NullLogger.Instance, stream, "synthetic")
        {
            SaveVersion = saveVersion,
            NameTable = new Dictionary<int, string> { [1] = "None", [2] = "/Game/Thing.Thing" },
        };

        var value = SoftObjectProperty.ReadValue(archive);

        CollectionAssert.AreEqual(new[] { "/Game/Thing.Thing_2", "" }, value);
        Assert.AreEqual(0x12345678, archive.ReadInt32());
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(3)]
    public void CryopodMissingName_WithDynamicTable_KeepsPlaceholder(int instanceNumber)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(0xABCD);
            writer.Write(instanceNumber);
            writer.Write(0); // terminator
            writer.Write(0x12345678);
        }
        stream.Position = 0;
        using var archive = new AsaArchive(NullLogger.Instance, stream, "synthetic-cryo")
        {
            SaveVersion = 14,
            IsCryopod = true,
            AllowDynamicNameTable = true,
            NameTable = new Dictionary<int, string> { [1] = "None" },
        };

        var value = SoftObjectProperty.ReadValue(archive);
        var expected = instanceNumber == 0 ? "Name_0000ABCD" : $"Name_0000ABCD_{instanceNumber}";

        CollectionAssert.AreEqual(new[] { expected }, value);
        Assert.AreEqual("Name_0000ABCD", archive.NameTable[0xABCD]);
        Assert.AreEqual(0x12345678, archive.ReadInt32());
        archive.Position = 0;
        Assert.AreEqual(expected, archive.ReadFName().FullName);
    }
}
