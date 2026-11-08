using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Utilities;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Properties;

[TestClass]
public class BytePropertyTests : SaveTests
{
    // ByteProperty == B2 B4 4F 96

    [TestMethod]
    [DynamicData(nameof(CreatureColorCases.All), typeof(CreatureColorCases))]
    public void ColorSetIndices_MatchAllRegions(int version, string objectId, bool isCryopodded, byte?[] expected)
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, $"version_{version}", "TheIsland_WP.ark");
        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        var record = reader.ReadGameRecord(Guid.Parse(objectId), TestContext.CancellationToken);
        if (isCryopodded)
        {
            Assert.IsTrue(record.HasCryoCreature());
            using var cryoReader = new CryopodReader(TestContext.GetLogger());
            record = cryoReader.ReadCryopodData(record, TestContext.CancellationToken)
                .SelectMany(set => set).Single(r => r.IsCreature());
        }

        var colors = record.Properties.Where(p => p.Tag.Name.FullName == "ColorSetIndices").ToArray();
        Assert.HasCount(expected.Count(c => c.HasValue), colors);
        foreach (var property in colors)
        {
            var color = Assert.IsInstanceOfType<ByteProperty>(property);
            Assert.IsTrue(color.Tag.ArrayIndex >= 0 && color.Tag.ArrayIndex < expected.Length);
            Assert.AreEqual(expected[color.Tag.ArrayIndex], (byte?)color.Value, $"Region {color.Tag.ArrayIndex}");
        }
        for (var region = 0; region < expected.Length; region++)
        {
            Assert.AreEqual(expected[region].HasValue, colors.Any(p => p.Tag.ArrayIndex == region), $"Region {region}");
        }
    }

    [TestMethod]
    public void CanRead_Verion13_ByteProperty()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        archive.Position = 0x112;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);
        Assert.AreEqual("ColorSetIndices", tag.Name.FullName);
        Assert.AreEqual("ByteProperty", tag.Type.ToString());
        Assert.AreEqual(5, tag.ArrayIndex);

        var property = ByteProperty.Read(archive, tag);

        var byteProperty = Assert.IsInstanceOfType<ByteProperty>(property);

        Assert.AreEqual(143, byteProperty.Value);
    }

    [TestMethod]
    public void CanRead_Verion14_ByteProperty()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        archive.Position = 0x9E6;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);
        Assert.AreEqual("ColorSetIndices", tag.Name.FullName);
        Assert.AreEqual("ByteProperty", tag.Type.ToString());
        Assert.AreEqual(5, tag.ArrayIndex);

        var property = ByteProperty.Read(archive, tag);

        var byteProperty = Assert.IsInstanceOfType<ByteProperty>(property);
        Assert.AreEqual(143, byteProperty.Value);
    }
    
    [TestMethod]
    public void CanRead_Verion13_ByteProperty_Enum()
    {
        using var archive = GetArchive("version_13/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        archive.Position = 0x442;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);
        Assert.AreEqual("BabyCuddleType", tag.Name.FullName);
        Assert.AreEqual("ByteProperty", tag.Type.TypeName.ToString());
        Assert.AreEqual(0, tag.ArrayIndex);
        Assert.AreEqual(1, tag.Type.ParameterCount);
        Assert.AreEqual("EBabyCuddleType", tag.Type.Parameters[0].TypeName.ToString());

        var property = ByteProperty.Read(archive, tag);

        var byteProperty = Assert.IsInstanceOfType<ByteEnumProperty>(property);
        Assert.AreEqual("EBabyCuddleType::FOOD", byteProperty.Value.ToString());
    }

    [TestMethod]
    public void CanRead_Verion14_ByteProperty_Enum()
    {
        using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        archive.Position = 0x957;

        var tag = PropertyTag.Read(archive);
        Assert.IsNotNull(tag);
        Assert.AreEqual("BabyCuddleType", tag.Name.FullName);
        Assert.AreEqual("ByteProperty", tag.Type.TypeName.ToString());
        Assert.AreEqual(0, tag.ArrayIndex);
        Assert.AreEqual(1, tag.Type.ParameterCount);
        Assert.AreEqual("EBabyCuddleType", tag.Type.Parameters[0].TypeName.ToString());

        var property = ByteProperty.Read(archive, tag);

        var byteProperty = Assert.IsInstanceOfType<ByteEnumProperty>(property);
        Assert.AreEqual("EBabyCuddleType::FOOD", byteProperty.Value.ToString());
    }
}
