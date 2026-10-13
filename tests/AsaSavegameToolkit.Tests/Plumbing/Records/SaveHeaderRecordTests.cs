using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace AsaSavegameToolkit.Tests.Plumbing.Records;

[TestClass]
public class SaveHeaderRecordTests : SaveTests
{
    [TestMethod]
    [DataRow((short)11)]
    [DataRow((short)12)]
    [DataRow((short)13)]
    [DataRow((short)14)]
    [DataRow((short)15)]
    public void Read_DelegatesAcrossHeaderLayoutBoundaries(short saveVersion)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(saveVersion);
            if (saveVersion >= 14)
                writer.Write(0L);

            var offsetPosition = stream.Position;
            writer.Write(0);
            writer.Write(1234.5);
            if (saveVersion >= 12)
                writer.Write(0);

            writer.Write(1);
            WriteString(writer, "SyntheticMap");

            var nameTableOffset = stream.Position;
            writer.Write(1);
            writer.Write(42);
            WriteString(writer, "TestName");
            writer.Write(0x12345678);

            stream.Position = offsetPosition;
            writer.Write(checked((int)nameTableOffset));
        }

        stream.Position = 0;
        using var archive = new AsaArchive(NullLogger.Instance, stream, "synthetic-header");
        var header = SaveHeaderRecord.Read(archive);

        Assert.AreEqual(saveVersion, header.SaveVersion);
        Assert.AreEqual(1234.5, header.GameTime);
        Assert.AreEqual("SyntheticMap", header.MapName);
        Assert.HasCount(1, header.NameTable);
        Assert.AreEqual("TestName", header.NameTable[42]);
        Assert.AreEqual(stream.Length - 4, archive.Position);
        Assert.AreEqual(0x12345678, archive.ReadInt32());
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length + 1);
        writer.Write(bytes);
        writer.Write((byte)0);
    }

    [TestMethod]
    public void CanRead_Verion13_SaveHeader()
    {
        var savePath = "version_13/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "SaveHeader");

        var saveHeader = SaveHeaderRecord.Read(archive);
        Assert.AreEqual("TheIsland_WP", saveHeader.MapName);
    }

    [TestMethod]
    public void CanRead_Verion14_SaveHeader()
    {
        var savePath = "version_14/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "SaveHeader");

        var saveHeader = SaveHeaderRecord.Read(archive);
        Assert.AreEqual("TheIsland_WP", saveHeader.MapName);
    }

    [TestMethod]
    public void CanRead_Version13_GameTime()
    {
        var savePath = "version_13/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "SaveHeader");
        var saveHeader = SaveHeaderRecord.Read(archive);
        Assert.IsNotNull(saveHeader);
        Assert.AreEqual(102587656.7352575, saveHeader.GameTime, 0.0000001);
    }

    [TestMethod]
    public void CanRead_Version14_GameTime()
    {
        var savePath = "version_14/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "SaveHeader");
        var saveHeader = SaveHeaderRecord.Read(archive);
        Assert.IsNotNull(saveHeader);
        Assert.AreEqual(102587663.71728921, saveHeader.GameTime, 0.0000001);
    }
}
