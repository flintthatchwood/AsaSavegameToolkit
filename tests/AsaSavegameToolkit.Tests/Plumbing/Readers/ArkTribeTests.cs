using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace AsaSavegameToolkit.Tests.Plumbing.Readers
{
    [TestClass]
    public class ArkTribeTests: SaveTests
    {
        [TestMethod]
        public void ReadFrom_ArkTribe()
        {
            var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP.ark");

            var readerSettings = new AsaReaderSettings
            {
                ReadArkTribeFiles = true,
                ReadArkProfileFiles = false,
                ReadCryoObjects = false
            };
            using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
            var records = reader.ReadGameRecords(TestContext.CancellationToken);
            var tribes = records.Values.Where(r=>r.IsTribe()).ToList();
            
            Assert.IsGreaterThan(0, tribes.Count);  

        }

        [TestMethod]
        public void ReadFrom_GameModeCustomBytes()
        {
            var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP_GameModCustomBytes.ark");

            var readerSettings = new AsaReaderSettings
            {
                ReadArkTribeFiles = true,
                ReadArkProfileFiles = false,
                ReadCryoObjects = false
            };
            using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
            Assert.IsNotNull(reader);


            var records = reader.ReadGameModeCustomBytes(TestContext.CancellationToken);
            Assert.IsNotNull(records);
            Assert.IsNotNull(records.Tribes);
            Assert.IsGreaterThan(0, records.Tribes.Count);

        }


    }
}
