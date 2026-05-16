using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Tests.Helpers;

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
}
