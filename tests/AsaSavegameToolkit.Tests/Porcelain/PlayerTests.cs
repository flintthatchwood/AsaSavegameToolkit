using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsaSavegameToolkit.Tests.Porcelain
{
    [TestClass]
    public class PlayerTests : SaveTests
    {

        [TestMethod]
        public void CanParseAsPlayer()
        {
            var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP.ark");
            using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
            var records = reader.ReadGameRecords(TestContext.CancellationToken);

            var profileRecords = records.Where(r => r.Value.IsProfile()).ToList();
            Assert.IsNotNull(profileRecords);
            Assert.IsGreaterThan(0, profileRecords.Count);


        }


    }
}
