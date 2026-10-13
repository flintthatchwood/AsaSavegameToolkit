using System.Diagnostics.CodeAnalysis;

using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Records;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace AsaSavegameToolkit.Tests.Helpers;

public class SaveTests
{
    /// <summary>
    /// For limiting managed-heap growth per test.
    /// </summary>
    private MemoryGuard? _memoryGuard;

    protected TestSettings TestSettings { get; } = TestSettings.Load();

    public required TestContext TestContext { get; set; }

    [TestInitialize]
    public void SetupMemoryGuard()
    {
        if (TestSettings.TestMemoryLimit.HasValue)
        {
            // Force a full GC before the test so the baseline is clean.
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            _memoryGuard = new MemoryGuard(TestContext, TestSettings.TestMemoryLimit.Value);
        }
    }

    [TestCleanup]
    public void TeardownMemoryGuard()
    {
        if (_memoryGuard != null)
        {
            var oneMB = 1024 * 1024;
            TestContext.WriteLine("Peak memory usage: {0} MB", _memoryGuard.PeakBytes / oneMB);
            _memoryGuard.Dispose();
            _memoryGuard = null;
        }
    }

    /// <summary>
    /// Returns a settings instance wired to the memory guard's cancellation token.
    /// Pass this to <c>AsaSaveGame.ReadFrom</c> so the guard can abort runaway tests.
    /// </summary>
    protected AsaReaderSettings GetReaderSettings(string saveFilePath)
    {
        // If the path was under the assets directory, make it relative so the debug output directory structure is nicer.
        // If it's outside (e.g. a temp file), just use the full path.

        var outputSuffix = Path.IsPathRooted(saveFilePath)
            ? Path.GetFileNameWithoutExtension(saveFilePath)  //  /some/long/path/save1.sav -> save1
            : Path.ChangeExtension(saveFilePath, null);       //  version_14/TheIsland_WP.sav -> version_14/TheIsland_WP

        return new AsaReaderSettings
        {
            DebugOutputDirectory = Path.Combine(TestSettings.OutputDirectory, outputSuffix)
        };
    }

    public AsaArchive GetArchive(string saveName, string tableName, string key, AsaReaderSettings? readerSettings = null)
    {
        string saveFilePath = Path.Combine(TestSettings.AssetsDirectory, saveName);

        Assert.IsTrue(File.Exists(saveFilePath), $"Save file not found: {saveFilePath}");

        var bytes = GetSqlBytes(saveFilePath, tableName, key);
        if(!string.IsNullOrEmpty(TestSettings.OutputDirectory))
        {
            string outputPath = Path.Combine(TestSettings.OutputDirectory, Path.ChangeExtension(saveName, null), tableName, $"{key}.bin");

            try
            {
                string directory = Path.GetDirectoryName(outputPath)!;
                
                if(!Directory.Exists(directory)){
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(outputPath, bytes);
            }
            catch(Exception ex)
            {
                TestContext.GetLogger().LogWarning(ex, "Failed to write SQL bytes to output directory");
            }
        }
        
        var archive = new AsaArchive(TestContext.GetLogger(), bytes, $"{saveFilePath}/{tableName}/{key}");

        if (key != "SaveHeader")
        {
            var saveHeader = GetSaveHeader(saveName, readerSettings);
            archive.SaveVersion = saveHeader.SaveVersion;
            archive.NameTable = saveHeader.NameTable;
        }

        return archive;
    }

    public SaveHeaderRecord GetSaveHeader(string saveName, AsaReaderSettings? readerSettings = null)
    {
        using var archive = GetArchive(saveName, "custom", "SaveHeader", readerSettings);
        var record = SaveHeaderRecord.Read(archive);
        return record;
    }

    public static byte[] GetSqlBytes(string path, string tableName, string key)
    {
        using var connection = new SqliteConnection($"Data Source={path};Filename={path};Mode=ReadOnly");
        using var command = new SqliteCommand($"SELECT value FROM {tableName} WHERE key = $key", connection);
        if (tableName == "game")
        {
            var keyBytes = AsaArchive.ConvertToBytes(Guid.Parse(key));
            command.Parameters.AddWithValue("$key", keyBytes);
        } else
        {
            command.Parameters.AddWithValue("$key", key);
        }

        connection.Open();
        using var reader = command.ExecuteReader();
        if (!reader.HasRows || !reader.Read())
        {
            throw new Exception("Unable to find key in database");
        }

        var valueBytes = GetSqlBytes(reader, 0);

        return valueBytes;
    }

    private static byte[] GetSqlBytes(SqliteDataReader reader, int fieldIndex)
    {
        const int ChunkSize = 2 * 1024;
        byte[] buffer = new byte[ChunkSize];
        long bytesRead;
        long fieldOffset = 0;

        using var stream = new MemoryStream();
        while ((bytesRead = reader.GetBytes(fieldIndex, fieldOffset, buffer, 0, buffer.Length)) > 0)
        {
            stream.Write(buffer, 0, (int)bytesRead);
            fieldOffset += bytesRead;
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Monitors managed heap growth on a background thread and cancels the test's
    /// <see cref="System.Threading.CancellationTokenSource"/> if the heap grows beyond <c>limitMb</c> MB
    /// above the baseline measured at construction.
    /// The cryopod parsing loop checks the token on each pod, so this reliably stops
    /// runaway parsing even during CPU-bound decompression (where Thread.Interrupt won't fire).
    /// </summary>
    private sealed class MemoryGuard : IDisposable
    {
        private readonly CancellationTokenSource _testCts;
        private readonly long _limitBytes;
        private readonly long _baselineBytes;
        private readonly CancellationTokenSource _guardCts = new();
        private readonly Task _task;

        [SuppressMessage("Usage", "MSTEST0054", Justification = "CancellationTokenSource access needed for memory based cancellation")]
        [SuppressMessage("Usage", "MSTEST0049", Justification = "We don't pass cancellationToken to Task.Run to avoid having it cancel itself")]
        public MemoryGuard(TestContext testContext, int limitMb)
        {
            _testCts = testContext.CancellationTokenSource;
            _limitBytes = (long)limitMb * 1024 * 1024;
            _baselineBytes = GC.GetTotalMemory(false);
            _task = Task.Run(MonitorAsync);
        }
        
        public long PeakBytes { get; private set; }

        private async Task MonitorAsync()
        {
            try
            {
                while (!_guardCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(250, _guardCts.Token).ConfigureAwait(false);

                    var current = GC.GetTotalMemory(false);
                    if (current > PeakBytes)
                    {
                        PeakBytes = current;
                    }

                    if (current - _baselineBytes > _limitBytes)
                    {
                        // Signal the test's CancellationToken. The cryopod parsing loop
                        // calls ct.ThrowIfCancellationRequested() on each pod, so the
                        // test thread will get an OperationCanceledException on the next
                        // iteration — no blocking wait required.
                        _testCts.Cancel();
                        return;
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        public void Dispose()
        {
            _guardCts.Cancel();
            _task.Wait(TimeSpan.FromSeconds(2));
            _guardCts.Dispose();
        }
    }
}