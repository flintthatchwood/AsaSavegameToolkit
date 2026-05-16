using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;

using FracturedJson;

using Newtonsoft.Json;

namespace AsaSavegameToolkit.Tests;

[TestClass]
public class DebugAssistTests : SaveTests
{
    [TestMethod]
    [Ignore]
    public void TestSpecificRecord()
    {
        var saveFile = "version_14/LostColony_WP.ark";
        var recordId = "3f264ce3-4c7c-1ff0-2df6-93850506220e";

        var outputDirectory = Path.Combine(TestSettings.OutputDirectory, Path.ChangeExtension(saveFile, null));
        Directory.CreateDirectory(outputDirectory);

        var readerSettings = new AsaReaderSettings { MaxCores = 1, DebugOutputDirectory = outputDirectory };

        using var reader = new AsaSaveReader(Path.Combine(TestSettings.AssetsDirectory, saveFile), TestContext.GetLogger(), readerSettings);
        var record = reader.ReadGameRecord(Guid.Parse(recordId), TestContext.CancellationToken);

        using var cryoReader = new CryopodReader(TestContext.GetLogger(), readerSettings);
        var cryoRecords = cryoReader.ReadCryopodData(record, TestContext.CancellationToken);

        var path = Path.Combine(outputDirectory, $"{recordId}.json");
        File.WriteAllText(path, new Formatter().Reformat(JsonConvert.SerializeObject(record)));
    }

    [TestMethod]
    //[Ignore]
    public void TestSpecificPorcelainRead()
    {
        var saveFile = "version_14/LostColony_WP.ark";
        var path = Path.Combine(TestSettings.AssetsDirectory, saveFile);
        var logger = TestContext.GetLogger();
        var saveGame = AsaSaveGame.ReadFrom(path, logger, cancellationToken: TestContext.CancellationToken);
    }

    [TestMethod]
    [Ignore]
    public void IndexAllPropertiesUsed()
    {
        var allSaves = Directory.GetFiles(TestSettings.AssetsDirectory, "*.ark", SearchOption.AllDirectories);
        var properties = new List<(string saveName, string objectId, string propertyName, string propertyType)>();

        void addProperties(IEnumerable<Property> props, string saveName, string objectId, string parentPath = "")
        {
            foreach (var prop in props)
            {
                var fullPath = string.IsNullOrEmpty(parentPath) ? prop.Tag.Name.ToString() : $"{parentPath}.{prop.Tag.Name}";
                properties.Add((saveName, objectId, fullPath, prop.Tag.Type.ToString()));
                if (prop is ArrayProperty arrayProp)
                {
                    addProperties(arrayProp.Value.OfType<Property>(), saveName, objectId, $"{fullPath}[]");
                }
                else if (prop is SetProperty setProp)
                {
                    addProperties(setProp.Value.OfType<Property>(), saveName, objectId, $"{fullPath}[]");
                }
                else if (prop is StructProperty structProp && structProp.Value is IEnumerable<Property> structElements)
                {
                    addProperties(structElements, saveName, objectId, fullPath);
                }
            }
        }

        foreach(var save in allSaves)
        {
            var relativePath = Path.GetRelativePath(TestSettings.AssetsDirectory, save);
            using var reader = new AsaSaveReader(save, TestContext.GetLogger(), new AsaReaderSettings { MaxCores = 1 });
            var records = reader.ReadGameRecords(TestContext.CancellationToken);
            foreach (var record in records.Values)
            {
                addProperties(record.Properties, relativePath, record.Uuid.ToString());
            }
        }

        var path = Path.Combine(TestSettings.OutputDirectory, "AllPropertiesUsed.json");
        Directory.CreateDirectory(TestSettings.OutputDirectory);

        var output = properties
            .GroupBy(x => x.propertyType)
            .Select(g => new
            {
                PropertyType = g.Key,
                Count = g.Count(),
                V13Count = g.Count(x => x.saveName.Contains("13")),
                V14Count = g.Count(x => x.saveName.Contains("14")),
                Occurrences = g.GroupBy(x => x.propertyName)
                    .Select(x => new { x.Key, Count = x.Count(), Instance13 = x.FirstOrDefault(x => x.saveName.Contains("13")), Instance14 = x.FirstOrDefault(x => x.saveName.Contains("14")) })
                    .ToDictionary(x => x.Key, x => new { x.Count, Instance13 = $"{x.Instance13.saveName}:{x.Instance13.objectId}", Instance14 = $"{x.Instance14.saveName}:{x.Instance14.objectId}" })
            })
            .OrderBy(x => x.Count)
            .ThenBy(x => x.PropertyType);

        File.WriteAllText(path, new Formatter().Reformat(JsonConvert.SerializeObject(output)));
    }

    [TestMethod]
    [Ignore]
    public void OutputFullSave()
    {
        var outputBins = true;
        var outputSaveHeaderJson = true;
        var outputGameObjectJson = true;

        var relativeSavePath = "version_14/LostColony_WP.ark";
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, relativeSavePath);
        var outputDirectory = Path.Combine(TestSettings.OutputDirectory, Path.ChangeExtension(relativeSavePath, null));
        var readerSettings = new AsaReaderSettings { DebugOutputDirectory = outputBins ? outputDirectory : null, MaxCores = 1 };
        if (outputBins)
        {
            readerSettings.DebugOutputDirectory = outputDirectory;
        }

        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger(), readerSettings);

        if (outputSaveHeaderJson)
        {
            var saveHeader = reader.ReadSaveHeader(TestContext.CancellationToken);
            var path = Path.Combine(outputDirectory, "custom", "SaveHeader.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, new Formatter().Reformat(JsonConvert.SerializeObject(saveHeader)));
        }

        if (outputGameObjectJson || outputBins)
        {
            using var cryoReader = new CryopodReader(TestContext.GetLogger(), readerSettings);
            var records = reader.ReadGameRecords(TestContext.CancellationToken);

            foreach (var record in records.Values)
            {
                TestContext.CancellationToken.ThrowIfCancellationRequested();
                var id = record.Uuid.ToString();

                if (outputGameObjectJson)
                {
                    var path = Path.Combine(outputDirectory, $"game/{id[0]}/{id[1]}/{id[2]}/{id}.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, new Formatter().Reformat(JsonConvert.SerializeObject(record)));
                }

                var cryoRecordSets = cryoReader.ReadCryopodData(record, TestContext.CancellationToken);
                if (outputGameObjectJson && cryoRecordSets.Count > 0)
                {
                    var path = Path.Combine(outputDirectory, $"game/{id[0]}/{id[1]}/{id[2]}/{id}.cryo.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, new Formatter().Reformat(JsonConvert.SerializeObject(cryoRecordSets)));
                }
            }
        }
    }
}
