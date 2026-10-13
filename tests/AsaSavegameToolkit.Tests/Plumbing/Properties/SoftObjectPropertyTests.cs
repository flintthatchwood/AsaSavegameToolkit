using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Tests.Helpers;

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
}
