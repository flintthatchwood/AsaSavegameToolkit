using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AsaSavegameToolkit.Plumbing.Readers;

public class AsaSaveReader : IDisposable
{
    private readonly string _saveFile;
    private readonly string _saveDirectory;
    private readonly ILogger _logger;
    private readonly AsaReaderSettings _settings;
    private readonly SqliteConnection _connection;
    private bool _disposed;

    // --- Read-through cache ---
    private SaveHeaderRecord? _cachedSaveHeader;
    private ActorTransformsRecord? _cachedActorTransforms;
    private GameModeCustomBytesRecord? _cachedGameModeCustomBytes;
    // IReadOnlyDictionary allows us to store a ConcurrentDictionary directly without
    // an extra .ToDictionary() copy, halving peak allocation during ReadGameRecords().
    private IReadOnlyDictionary<Guid, GameObjectRecord>? _cachedGameRecords;

    // Shared string pool for FString deduplication across parallel archives.
    // Lives exactly as long as this reader; released in Dispose().
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _stringPool = new();

    public AsaSaveReader(string saveFile, ILogger? logger = default, AsaReaderSettings? settings = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveFile);

        if (!File.Exists(saveFile))
        {
            throw new FileNotFoundException("Save file not found", saveFile);
        }

        _saveFile = saveFile;
        _saveDirectory = Path.GetDirectoryName(saveFile) ?? throw new Exception($"Unable to get directory name for path {saveFile}");

        _logger = logger ?? NullLogger.Instance;
        _settings = settings ?? AsaReaderSettings.None;
        string sqlConnectionString = $"Data Source={_saveFile};Filename={_saveFile};Mode=ReadOnly";

        _connection = new SqliteConnection(sqlConnectionString);
    }

    private readonly object _saveHeaderLock = new();
    public SaveHeaderRecord ReadSaveHeader(CancellationToken cancellationToken = default)
    {
         if (_cachedSaveHeader == null)
        {
            lock (_saveHeaderLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _cachedSaveHeader ??= ProcessCustomTableRow("SaveHeader", SaveHeaderRecord.Read);
            }
        }

        return _cachedSaveHeader;
    }

    private readonly object _actorTransformsLock = new();
    public ActorTransformsRecord ReadActorTransforms(CancellationToken cancellationToken = default)
    {
        if (_cachedActorTransforms == null)
        {
            lock(_actorTransformsLock)
            {
                if (_cachedActorTransforms == null)
                {
                    var saveHeader = ReadSaveHeader(cancellationToken);
                    _cachedActorTransforms = ProcessCustomTableRow("ActorTransforms", ActorTransformsRecord.Read, saveHeader);
                }
            }
        }

        return _cachedActorTransforms;
    }

    private readonly object _gameModeCustomBytesLock = new();
    public GameModeCustomBytesRecord ReadGameModeCustomBytes(CancellationToken cancellationToken = default)
    {
        if (_cachedGameModeCustomBytes == null)
        {
            lock(_gameModeCustomBytesLock)
            {
                if (_cachedGameModeCustomBytes == null)
                {
                    var saveHeader = ReadSaveHeader(cancellationToken);
                    _cachedGameModeCustomBytes = ProcessCustomTableRow("GameModeCustomBytes", GameModeCustomBytesRecord.Read, saveHeader);
                }
            }
        }

        return _cachedGameModeCustomBytes;
    }

    private readonly object _gameRecordsLock = new();
    public IReadOnlyDictionary<Guid, GameObjectRecord> ReadGameRecords(CancellationToken cancellationToken = default)
    {
        if (_cachedGameRecords != null)
        {
            return _cachedGameRecords;
        }

        lock(_gameRecordsLock)
        {
            if (_cachedGameRecords != null)
            {
                return _cachedGameRecords;
            }

            var saveHeader = ReadSaveHeader(cancellationToken);

            using var commandData = new SqliteCommand("SELECT key, value FROM game", _connection);

            _connection.Open();
            using var reader = commandData.ExecuteReader();

            var gameRows = new Dictionary<Guid, byte[]>();

            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keyBytes = GetSqlBytes(reader, 0);
                var valueBytes = GetSqlBytes(reader, 1);
                Guid guid = keyBytes.ToArkGuid();
                gameRows.Add(guid, valueBytes);

                var id = guid.ToString();
                DumpDebugBytes($"game/{id[0]}/{id[1]}/{id[2]}/{id}.bin", valueBytes);
            }

            if (gameRows.Count == 0)
            {
                throw new InvalidOperationException("No game records found in the save file.");
            }

            var parsedGameRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();

            Parallel.ForEach(gameRows, new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxCores }, gameRow =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (objectId, objectBytes) = gameRow;

                try
                {
                    var gameRecord = ParseGameRecord(objectId, objectBytes, saveHeader);
                    parsedGameRecords.AddOrUpdate(objectId, gameRecord, (_, _) =>
                    {
                        _logger.LogWarning("Replacing game object with guid {Guid}. This may indicate duplicate entries in the database or a collision. Returning the new object.", objectId);
                        return gameRecord;
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse game object {ObjectId}", objectId);
                    throw new AsaDataException($"Failed to parse game object {objectId} in {_saveFile}", ex);
                }
            });

            // Release all raw byte arrays now that parsing is complete.
            // This allows the GC to collect the raw SQL bytes before the result dict is
            // assigned, keeping peak RSS lower on large saves.
            gameRows = null!;

            // Assign the ConcurrentDictionary directly — ConcurrentDictionary implements
            // IReadOnlyDictionary, so no .ToDictionary() copy is needed.
            _cachedGameRecords = parsedGameRecords;
        }
        return _cachedGameRecords;
    }

    public GameObjectRecord ReadGameRecord(Guid objectId, CancellationToken cancellationToken = default)
    {
        var saveHeader = ReadSaveHeader(cancellationToken);

        var keyBytes = objectId.ToArkByteArray();

        using var commandData = new SqliteCommand("SELECT value FROM game WHERE key = $key", _connection);
        var parameter = commandData.Parameters.AddWithValue("$key", keyBytes);
        parameter.SqliteType = SqliteType.Blob;

        _connection.Open();
        using var reader = commandData.ExecuteReader();

        if (!reader.Read())
        {
            throw new InvalidOperationException($"Unable to find game object with guid {objectId} in the database.");
        }

        var valueBytes = GetSqlBytes(reader, 0);

        var id = objectId.ToString();
        DumpDebugBytes($"game/{id[0]}/{id[1]}/{id[2]}/{id}.bin", valueBytes);

        cancellationToken.ThrowIfCancellationRequested();
        return ParseGameRecord(objectId, valueBytes, saveHeader);
    }

    /// <summary>
    /// Writes <paramref name="data"/> to <c>{DebugOutputDirectory}/{relativePath}</c> when
    /// <see cref="AsaReaderSettings.DebugOutputDirectory"/> is configured. No-op otherwise.
    /// </summary>
    private void DumpDebugBytes(string relativePath, byte[] data)
    {
        var dir = _settings.DebugOutputDirectory;
        if (dir == null) return;

        var fullPath = Path.Combine(dir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, data);
    }

    private GameObjectRecord ParseGameRecord(Guid objectId, byte[] objectData, SaveHeaderRecord saveHeader)
    {
        var stream = new MemoryStream(objectData);
        // Use just the GUID as the filename — the full save path is the same for every record
        // and concatenating it 100k+ times creates unnecessary unique string allocations.
        using var archive = new AsaArchive(_logger, stream, _saveFile)
        {
            NameTable = saveHeader.NameTable,
            SaveVersion = saveHeader.SaveVersion,
            StringPool = _stringPool,
        };

        return GameObjectRecord.Read(archive, objectId);
    }

    //private void AddTribeFiles(AsaSaveGame saveGame)
    //{
    //    var debugSettings = GetDerivedReaderSettings("tribes");
    //    var tribeFiles = Directory.EnumerateFiles(_saveDirectory, "*.arktribe");

    //    var tribeBag = new ConcurrentBag<TribeFile>();
    //    var exceptions = new ConcurrentBag<Exception>();

    //    Parallel.ForEach(tribeFiles, new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxCores }, filePath =>
    //    {
    //        try
    //        {
    //            using var stream = new MemoryStream(File.ReadAllBytes(filePath));
    //            using var archive = new AsaArchive(_logger, stream, filePath)
    //            {
    //                NameTable = NameTable,
    //                SaveVersion = SaveVersion
    //            };

    //            var parsed = TribeFile.Read(archive, filePath, usePropertiesOffset: true);

    //            tribeBag.Add(parsed);

    //            if (debugSettings.HasOutput)
    //            {
    //                var outputPath = Path.Join(debugSettings.OutputDirectory, Path.GetFileName(filePath));
    //                File.Copy(filePath, outputPath, overwrite: true);
    //                DebugOutput.WriteSerializedJson(Path.ChangeExtension(outputPath, ".json"), parsed);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Failed to read tribe file {FilePath}", filePath);
    //            exceptions.Add(ex);
    //        }
    //    });

    //    saveGame.TribeFiles.AddRange(tribeBag);

    //    if (!exceptions.IsEmpty)
    //    {
    //        throw new AggregateException("Failed to read one or more tribe files", exceptions);
    //    }
    //}

    //private void AddProfileFiles(AsaSaveGame saveGame)
    //{
    //    var debugSettings = GetDerivedReaderSettings("profiles");
    //    var profileFiles = Directory.EnumerateFiles(_saveDirectory, "*.arkprofile");

    //    var parsedFiles = new ConcurrentBag<ProfileFile>();
    //    var exceptions = new ConcurrentBag<Exception>();

    //    Parallel.ForEach(profileFiles, new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxCores }, filePath =>
    //    {
    //        try
    //        {
    //            using var stream = new MemoryStream(File.ReadAllBytes(filePath));
    //            using var archive = new AsaArchive(_logger, stream, filePath)
    //            {
    //                NameTable = NameTable,
    //                SaveVersion = SaveVersion
    //            };

    //            var parsed = ProfileFile.Read(archive, filePath);

    //            parsedFiles.Add(parsed);

    //            if (debugSettings.HasOutput)
    //            {
    //                var outputPath = Path.Join(debugSettings.OutputDirectory, Path.GetFileName(filePath));
    //                File.Copy(filePath, outputPath, overwrite: true);
    //                DebugOutput.WriteSerializedJson(Path.ChangeExtension(outputPath, ".json"), parsed);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Failed to read profile file {FilePath}", filePath);
    //            exceptions.Add(ex);
    //        }
    //    });

    //    saveGame.ProfileFiles.AddRange(parsedFiles);

    //    if (!exceptions.IsEmpty)
    //    {
    //        throw new AggregateException("Failed to read one or more profile files", exceptions);
    //    }
    //}

    private T ProcessCustomTableRow<T>(string key, Func<AsaArchive, T> processSqlBytes, SaveHeaderRecord? saveHeader = null)
    {
        try
        {
            using var command = new SqliteCommand($"SELECT value FROM custom WHERE key = '{key}'", _connection);

            _connection.Open();
            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                throw new AsaDataException($"Unable to read custom table for key {key}. No rows found.");
            }

            var sqlBytes = GetSqlBytes(reader, 0);
            DumpDebugBytes(Path.Combine("custom", key + ".bin"), sqlBytes);

            using var stream = new MemoryStream(sqlBytes);
            using var archive = new AsaArchive(_logger, stream, $"{_saveFile}/custom/{key}")
            {
                StringPool = _stringPool,
            };
            if(saveHeader != null)
            {
                archive.NameTable = saveHeader.NameTable;
                archive.SaveVersion = saveHeader.SaveVersion;
            }

            T result = processSqlBytes(archive)
                ?? throw new AsaDataException($"Processing byte for {key} in {_saveFile} returned null");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read {RecordKey} in {SaveFile}", key, _saveFile);
            throw new AsaDataException($"Failed to read {key} in {_saveFile}", ex);
        }
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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection.Dispose();
            }

            _cachedGameRecords = null;
            _cachedSaveHeader = null;
            _cachedGameModeCustomBytes = null;
            _cachedActorTransforms = null;
            _stringPool.Clear();

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
