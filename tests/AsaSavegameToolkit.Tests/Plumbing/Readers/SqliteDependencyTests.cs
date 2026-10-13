using Microsoft.Data.Sqlite;

namespace AsaSavegameToolkit.Tests.Plumbing.Readers;

[TestClass]
public class SqliteDependencyTests
{
    public required TestContext TestContext { get; set; }

    [TestMethod]
    public void NativeSqliteVersion_IsAtLeast3502()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version()";
        var result = command.ExecuteScalar();

        Assert.IsInstanceOfType<string>(result);
        var version = Version.Parse((string)result);
        TestContext.WriteLine($"Loaded native SQLite version: {version}");
        Assert.IsTrue(version >= new Version(3, 50, 2),
            $"SQLite {version} is older than the 3.50.2 fix for CVE-2025-6965.");
    }
}
