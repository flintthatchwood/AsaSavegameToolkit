using AsaSavegameToolkit.Plumbing.Readers;

namespace AsaSavegameToolkit.Tests.Plumbing.Readers;

[TestClass]
public class AsaArchiveTests
{
    [TestMethod]
    public void Should_convert_bytes_to_guid()
    {
        var bytes = Convert.FromHexString("AD 48 4B FE 0D DE 96 4D 9B 9D E8 98 3A 41 E8 75".Replace(" ", ""));
        var guid = AsaArchive.ConvertToGuid(bytes);

        Assert.AreEqual("fe4b48ad-4d96-de0d-98e8-9d9b75e8413a", guid.ToString().ToLower());
    }
    [TestMethod]
    public void Should_convert_guid_to_bytes()
    {
        var guid = Guid.Parse("fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
        var bytes = AsaArchive.ConvertToBytes(guid);

        Assert.AreEqual("AD 48 4B FE 0D DE 96 4D 9B 9D E8 98 3A 41 E8 75", BitConverter.ToString(bytes).Replace('-', ' '));
    }
}
