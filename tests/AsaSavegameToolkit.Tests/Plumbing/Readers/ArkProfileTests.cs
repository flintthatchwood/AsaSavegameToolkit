using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AsaSavegameToolkit.Tests.Plumbing.Readers
{
    [TestClass]
    public class ArkProfileTests : SaveTests
    {
        [TestMethod]
        public void Read_SingleProfileFile()
        {
            var filePath = Path.Combine(TestSettings.AssetsDirectory, "version_14/0002079ae0244af7919a026db2b53ab0.arkprofile");
            var record = ArkProfileRecord.ReadFromFile(filePath, Guid.NewGuid());
            Assert.IsNotNull(record);
        }

        [TestMethod]
        public void ReadFrom_ArkProfile()
        {
            var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP.ark");

            var readerSettings = new AsaReaderSettings
            {
                ReadArkTribeFiles = false,
                ReadArkProfileFiles = true,
                ReadCryoObjects = false
            };
            using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger(), readerSettings);
            var records = reader.ReadGameRecords(TestContext.CancellationToken);
            var profiles = records.Values.Where(r => r.IsProfile()).ToList();

            Assert.IsNotEmpty(profiles);
        }

        [TestMethod]
        public void ReadFrom_GameModeCustomBytes()
        {
            var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP_GameModCustomBytes.ark");

            using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
            Assert.IsNotNull(reader);

            var records = reader.ReadGameModeCustomBytes(TestContext.CancellationToken);
            Assert.IsNotNull(records);
            Assert.IsNotNull(records.Profiles);
            Assert.IsNotEmpty(records.Profiles);
        }

    }
}

