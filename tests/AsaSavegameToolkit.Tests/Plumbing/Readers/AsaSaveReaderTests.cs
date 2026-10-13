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
}
