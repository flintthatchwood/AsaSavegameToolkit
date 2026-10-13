using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Plumbing.Readers;

[TestClass]
public class AsaSaveReaderTests : SaveTests
{
    [TestMethod]
    public void CanRead_AllTestSaves()
    {
        var exceptions = new List<Exception>();
        foreach (var file in Directory.EnumerateFiles(TestSettings.AssetsDirectory, "*.ark", SearchOption.AllDirectories))
        {
            try
            {
                var reader = new AsaSaveReader(file, TestContext.GetLogger());
                var records = reader.ReadGameRecords(TestContext.CancellationToken);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing file {file}: {ex.Message}");
                exceptions.Add(new Exception($"Error parsing file {file}", ex));
            }
        }

        if (exceptions.Count != 0)
        {
            throw new AggregateException(exceptions);
        }
    }

    [TestMethod]
    public void ReadGameRecords_KeepsOnlyRecordsMatchingTheFilter()
    {
        var path = Path.Combine(TestSettings.AssetsDirectory, "version_14", "TheIsland_WP.ark");
        var wanted = Guid.Parse("be2fb31d-487c-5bd6-e684-9999d4d751a5");

        using var all = new AsaSaveReader(path, TestContext.GetLogger());
        Assert.IsGreaterThan(1, all.ReadGameRecords(TestContext.CancellationToken).Count);

        var settings = new AsaReaderSettings { RecordFilter = r => r.Uuid == wanted };
        using var filtered = new AsaSaveReader(path, TestContext.GetLogger(), settings);
        var records = filtered.ReadGameRecords(TestContext.CancellationToken);

        Assert.HasCount(1, records);
        Assert.IsTrue(records.ContainsKey(wanted));
    }
}
