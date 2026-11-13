using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Readers;

/// <summary>
/// Tests for <see cref="CryopodReader"/> at the plumbing layer.
/// These tests verify that raw cryo blobs can be decompressed and parsed into game object records.
/// Surfacing cryo creatures to the user is a porcelain-layer concern (see CreatureTests).
/// </summary>
[TestClass]
public class CryopodReaderTests : SaveTests
{
    // Known cryopod containing a Lystrosaurus in version_13/TheIsland_WP.ark
    private const string KnownCryopodUuid = "0028e3ab-4571-dea6-7107-a5a78963a986";
    private const string KnownSaveFile = "version_13/TheIsland_WP.ark";

    [TestMethod]
    public void HasCryoCreature_IsTrue_ForKnownCryopod()
    {
        using var reader = new AsaSaveReader(Path.Combine(TestSettings.AssetsDirectory, KnownSaveFile), TestContext.GetLogger());
        var records = reader.ReadGameRecords(TestContext.CancellationToken);

        var pod = records[Guid.Parse(KnownCryopodUuid)];
        Assert.IsTrue(pod.HasCryoCreature(), "Known cryopod should pass HasCryoCreature() filter");
    }

    [TestMethod]
    public void GetCryopodContents_ReturnsCreature_ForKnownCryopod()
    {
        using var reader = new AsaSaveReader(Path.Combine(TestSettings.AssetsDirectory, KnownSaveFile), TestContext.GetLogger());
        var records = reader.ReadGameRecords(TestContext.CancellationToken);

        var pod = records[Guid.Parse(KnownCryopodUuid)];
        using var cryopodReader = new CryopodReader(TestContext.GetLogger());
        var sets = cryopodReader.ReadCryopodData(pod);

        foreach (var set in sets)
        {
            Assert.IsNotEmpty(sets, "Expected at least one game object from cryopod contents");
            foreach (var obj in set)
            { 
                TestContext.WriteLine($"  {obj.ClassName.FullName} uuid={obj.Uuid} isCreature={obj.IsCreature()}");
            }
            Assert.IsTrue(set.Any(r => r.IsCreature()), "Expected at least one parsed creature record");
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    public void ReadCryopodData_SkipsEmptySlots(int slotLength)
    {
        // a Gargoyle cryopod on a server save had five extra slots holding a single 0 byte each
        using var reader = new AsaSaveReader(Path.Combine(TestSettings.AssetsDirectory, KnownSaveFile), TestContext.GetLogger());
        var pod = reader.ReadGameRecords(TestContext.CancellationToken)[Guid.Parse(KnownCryopodUuid)];
        using var cryopodReader = new CryopodReader(TestContext.GetLogger());
        var expected = cryopodReader.ReadCryopodData(pod).Count;

        var data = pod.Properties.Get<object[]>("CustomItemDatas")!.OfType<CustomItemDataRecord>().First();
        data.CustomDataBytes = [.. data.CustomDataBytes, new byte[slotLength], new byte[slotLength], new byte[slotLength], new byte[slotLength], new byte[slotLength]];

        Assert.HasCount(expected, cryopodReader.ReadCryopodData(pod));
    }

    [TestMethod]
    public void ReadCryopodData_FourByteSlotStillUsesTheParser()
    {
        using var reader = new AsaSaveReader(Path.Combine(TestSettings.AssetsDirectory, KnownSaveFile), TestContext.GetLogger());
        var pod = reader.ReadGameRecords(TestContext.CancellationToken)[Guid.Parse(KnownCryopodUuid)];
        using var cryopodReader = new CryopodReader(TestContext.GetLogger());
        var expected = cryopodReader.ReadCryopodData(pod).Count;
        var data = pod.Properties.Get<object[]>("CustomItemDatas")!.OfType<CustomItemDataRecord>().First();
        data.CustomDataBytes = [.. data.CustomDataBytes, [7, 0, 0, 0]];

        var sets = cryopodReader.ReadCryopodData(pod);

        Assert.HasCount(expected + 1, sets);
        Assert.IsEmpty(sets[^1]);
    }
}
