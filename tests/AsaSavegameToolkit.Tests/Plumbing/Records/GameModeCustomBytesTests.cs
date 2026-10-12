using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Tests.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

namespace AsaSavegameToolkit.Tests.Plumbing.Records;

[TestClass]
public class GameModeCustomBytesTests : SaveTests
{
    [TestMethod]
    public void CanRead_Version13_GameModeCustomBytes()
    {
        var savePath = "version_13/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "GameModeCustomBytes");

        var record = GameModeCustomBytesRecord.Read(archive);
    }

    [TestMethod]
    public void CanRead_Version14_GameModeCustomBytes()
    {
        var savePath = "version_14/TheIsland_WP.ark";
        using var archive = GetArchive(savePath, "custom", "GameModeCustomBytes");

        var record = GameModeCustomBytesRecord.Read(archive);
    }

    [TestMethod]
    [DataRow("version_14/Aberration_14.ark", 2)]
    [DataRow("version_14/TheIsland_14.ark", 2)]
    [DataRow("version_14/LostColony_WP.ark", 1)]
    public void Version14_GameModeCustomBytes_ReadsEveryProfile(string savePath, int profileCount)
    {
        using var archive = GetArchive(savePath, "custom", "GameModeCustomBytes");

        var record = GameModeCustomBytesRecord.Read(archive);

        Assert.HasCount(profileCount, record.Profiles);
        foreach (var profile in record.Profiles)
        {
            // blobs start with their archive version
            Assert.IsGreaterThan(4, profile.RawBlob.Length, $"Profile {profile.EosId} has no data");
            Assert.AreEqual(7, BitConverter.ToInt32(profile.RawBlob, 0));
        }
        Assert.AreEqual(profileCount, record.Profiles.Select(p => p.EosId).Distinct().Count());
    }

    [TestMethod]
    public void ProfileHeaderOffset_IsRelativeToTheEndOfTheProfileSectionHeader()
    {
        byte[] tribe = [7, 0, 0, 0, 0xA1, 0xA2];
        byte[] profileA = [7, 0, 0, 0, 0xB1, 0xB2, 0xB3];
        byte[] profileB = [6, 0, 0, 0, 0xC1];

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(1);                // flags
            writer.Write(tribe.Length);     // tribe header offset, from the tribe data start
            writer.Write(1);                // tribe count
            writer.Write(tribe);
            writer.Write(42u);              // tribe id
            writer.Write(0);                // unknown
            writer.Write(0);                // offset
            writer.Write(tribe.Length);     // size

            writer.Write(profileA.Length + profileB.Length); // profile header offset, from the profile data start
            writer.Write(2);                                 // profile count
            writer.Write(profileA);
            writer.Write(profileB);
            writer.Write(1111L);
            writer.Write(0);
            writer.Write(profileA.Length);
            writer.Write(2222L);
            writer.Write(profileA.Length);
            writer.Write(profileB.Length);
        }
        stream.Position = 0;
        using var archive = new AsaArchive(NullLogger.Instance, stream, "synthetic/GameModeCustomBytes");

        var record = GameModeCustomBytesRecord.Read(archive);

        Assert.HasCount(1, record.Tribes);
        Assert.AreEqual(42u, record.Tribes[0].TribeId);
        CollectionAssert.AreEqual(tribe, record.Tribes[0].RawBlob);
        Assert.HasCount(2, record.Profiles);
        Assert.AreEqual(1111L, record.Profiles[0].EosId);
        CollectionAssert.AreEqual(profileA, record.Profiles[0].RawBlob);
        Assert.AreEqual(2222L, record.Profiles[1].EosId);
        CollectionAssert.AreEqual(profileB, record.Profiles[1].RawBlob);
    }
}