using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Records
{
    [TestClass]
    public class DinoAncestorsEntryRecordTest : SaveTests
    {
        [TestMethod]
        public void CanRead_DinoAncestorsEntryRecord()
        {
            using var archive = GetArchive("version_14/TheIsland_WP.ark", "game", "fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");

            var uuid = Guid.Parse("fe4b48ad-4d96-de0d-98e8-9d9b75e8413a");
            archive.Position = 0; // Start of file

            var gameObject = GameObjectRecord.Read(archive, uuid);
            var ancestors = gameObject.Properties.Get<object[]>("DinoAncestors")?.OfType<DinoAncestorsEntryRecord>();

            Assert.IsNotNull(ancestors);
            DinoAncestorsEntryRecord first = ancestors.First();
            Assert.IsNotNull(first.MaleName);
            Assert.Contains("T4", first.MaleName);
            Assert.AreEqual(156527770u, first.MaleId1);
            Assert.AreEqual(431563979u, first.MaleId2);
        }
    }
}
