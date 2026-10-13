using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using AsaSavegameToolkit.Porcelain;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Numerics;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

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
            var parsedInventoryOwners = new ConcurrentDictionary<Guid, GameObjectRecord>();

            if (_settings.ReadGameObjects || _settings.ReadCryoObjects)
            {
                //cryo objects are stored inside gameobject so need to load if either is enabled to ensure we have all the data for parsing. 
                Parallel.ForEach(gameRows, new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxCores }, gameRow =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var (objectId, objectBytes) = gameRow;

                    try
                    {
                        var gameRecord = ParseGameRecord(objectId, objectBytes, saveHeader);
                        var inventoryComponentRef = gameRecord.Properties.Get<ObjectProperty>("MyInventoryComponent")?.Value?.ObjectId??Guid.Empty;

                        if (inventoryComponentRef!= Guid.Empty)
                        {
                            parsedInventoryOwners.AddOrUpdate(inventoryComponentRef, gameRecord, (_, _) =>
                            {
                                _logger.LogWarning("Replacing inventory component object with guid {Guid}. Returning the new object.", objectId);
                                return gameRecord;
                            });
                        }


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
            }

            // Release all raw byte arrays now that parsing is complete.
            // This allows the GC to collect the raw SQL bytes before the result dict is
            // assigned, keeping peak RSS lower on large saves.
            gameRows = null!;

            if (_settings.ReadCryoObjects)
            {



                using CryopodReader cryoReader = new CryopodReader(_logger, _settings);
                foreach (var cryopod in parsedGameRecords.Values.Where(r => r.HasCryoCreature()))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var cryoRecordSets = cryoReader.ReadCryopodData(cryopod, cancellationToken).ToList();

                    // Cryopod records allow for multiple CustomItemDatas entries, each one should probably be processed as a
                    // single dino. Still, warn if we see anything other that 1 set

                    if (cryoRecordSets.Count == 0)
                    {
                        _logger.LogWarning("DataBytes with name {CryopodName} and UUID {CryopodUuid} does not contain any data", cryopod.Names[0], cryopod.Uuid);
                        continue;
                    }

                    var creatureRecord = cryoRecordSets.SelectMany(g => g).FirstOrDefault(r => r.IsCreature());
                    if (creatureRecord == null)
                    {
                        _logger.LogWarning("DataBytes with name {CryopodName} and UUID {CryopodUuid} does not contain any creature data", cryopod.Names[0], cryopod.Uuid);
                        continue;
                    }


                    var containerProperty = creatureRecord.Properties.Get<ObjectProperty?>("CryoContainer");
                    if(containerProperty!=null)
                    {
                        var inventoryRecord = parsedGameRecords[containerProperty.Value.ObjectId];
                        if (inventoryRecord.Names.Count > 1)
                        {

                            if (parsedInventoryOwners.ContainsKey(inventoryRecord.Uuid))
                            {
                                var parentComponent = parsedInventoryOwners[inventoryRecord.Uuid];
                                var containerId = parentComponent.Uuid;                               

                                creatureRecord.Properties.Remove(containerProperty);
                                creatureRecord.Properties.Add(new ObjectProperty()
                                {
                                    Tag = containerProperty.Tag,
                                    Value = new Primitives.ObjectReference()
                                    {
                                        IsPath = false,
                                        ObjectId = containerId
                                    }

                                });


                                //re-base the TargetingTeam of cryo creature to the container it now exists in
                                var creatureTargetingTeamProperty = creatureRecord.Properties.FirstOrDefault(p => p.Tag.Name.ToString() == "TargetingTeam") as IntProperty;
                                if (creatureTargetingTeamProperty != null)
                                {
                                    var parentTargetingTeamValue = parentComponent.Properties.Get<int>("TargetingTeam");
                                    if (parentTargetingTeamValue != 0 && parentTargetingTeamValue != creatureTargetingTeamProperty.Value)
                                    {
                                        creatureTargetingTeamProperty.Value = parentTargetingTeamValue;                                        
                                    }
                                }
                            }


                        }
                    }


                    foreach (var cryoRecords in cryoRecordSets)
                    {
                        foreach (var cryoObject in cryoRecords)
                        {
                            parsedGameRecords.AddOrUpdate(cryoObject.Uuid, cryoObject, (_, _) =>
                            {
                                _logger.LogWarning("Replacing game object with guid {Guid} from cryopod parsing. This may indicate duplicate entries in the database or a collision. Returning the new object.", cryoObject.Uuid);
                                return cryoObject;
                            });
                        }
                    }
                }
            }


            if (_settings.ReadArkTribeFiles || _settings.ReadArkProfileFiles)
            {
                GameModeCustomBytesRecord? customBytes = null;
                try
                {
                    customBytes = ReadGameModeCustomBytes(cancellationToken);
                }catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read GameModeCustomBytes. This may impact parsing of tribe and profile data.");
                }

                if (_settings.ReadArkTribeFiles)
                {
                    var tribeObjects = ParseTribes(cancellationToken, customBytes);
                    if(tribeObjects.Count > 0)
                    {
                        Parallel.ForEach(tribeObjects, new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxCores }, tribeGameObject =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            parsedGameRecords.AddOrUpdate(tribeGameObject.Key, tribeGameObject.Value, (_, _) =>
                            {
                                _logger.LogWarning("Replacing game object with guid {Guid} from tribe files. This may indicate duplicate entries in the database or a collision. Returning the new object.", tribeGameObject.Key);
                                return tribeGameObject.Value;
                            });
                        }
                        );
                    }                                       
                }

                if (_settings.ReadArkProfileFiles)
                {
                    var profileObjects = ParseProfiles(cancellationToken, customBytes);
                    if (profileObjects.Count > 0)
                    {
                        Parallel.ForEach(profileObjects, new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxCores }, profileGameObject =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            parsedGameRecords.AddOrUpdate(profileGameObject.Key, profileGameObject.Value, (_, _) =>
                            {
                                _logger.LogWarning("Replacing game object with guid {Guid} from profile files. This may indicate duplicate entries in the database or a collision. Returning the new object.", profileGameObject.Key);
                                return profileGameObject.Value;
                            });
                        }
                        );
                    }
                }
            }                       

            StringPool.Shared.Reset();


            // Assign the ConcurrentDictionary directly — ConcurrentDictionary implements
            // IReadOnlyDictionary, so no .ToDictionary() copy is needed.
            _cachedGameRecords = parsedGameRecords;
        }
        return _cachedGameRecords;
    }

    private ConcurrentDictionary<Guid, GameObjectRecord> ParseProfiles(CancellationToken cancellationToken, GameModeCustomBytesRecord? customBytes)
    {
        ConcurrentDictionary<Guid, GameObjectRecord> profileRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();

        if (customBytes != null && customBytes.Profiles.Count > 0)
        {
            Parallel.ForEach(customBytes.Profiles, new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxCores }, profileContainer =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var subArchive = new AsaArchive(NullLogger.Instance, profileContainer.RawBlob, string.Empty);
                subArchive.IsArkFile = true;
                var profile = ArkProfileRecord.Read(subArchive, Guid.NewGuid());
                profileRecords.AddOrUpdate(profile.Uuid, profile, (_, _) =>
                {
                    _logger.LogWarning("Replacing game object with guid {Guid} from profile files. This may indicate duplicate entries in the database or a collision. Returning the new object.", profile.Uuid);
                    return profile;
                });
            }
            );
        }
        else
        {
            using ArkProfileReader profileReader = new ArkProfileReader(_logger, _settings);
            var profileObjects = profileReader.Read(_saveDirectory, cancellationToken);

            Parallel.ForEach(profileObjects, new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxCores }, profileGameObject =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                profileRecords.AddOrUpdate(profileGameObject.Uuid, profileGameObject, (_, _) =>
                {
                    _logger.LogWarning("Replacing game object with guid {Guid} from profile files. This may indicate duplicate entries in the database or a collision. Returning the new object.", profileGameObject.Uuid);
                    return profileGameObject;
                });
            }
            );
        }

        return profileRecords;
    }

    private ConcurrentDictionary<Guid, GameObjectRecord> ParseTribes(CancellationToken cancellationToken, GameModeCustomBytesRecord? customBytes)
    {
        ConcurrentDictionary<Guid, GameObjectRecord> tribeRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();
        //first attempt to read from GameModeCustomBytes
        if ( customBytes!=null && customBytes.Tribes.Count > 0)
        {
            Parallel.ForEach(customBytes.Tribes, new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxCores }, tribeContainer =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var subArchive = new AsaArchive(NullLogger.Instance, tribeContainer.RawBlob, string.Empty);
                subArchive.IsArkFile = true;
                var tribe = ArkTribeRecord.Read(subArchive, Guid.NewGuid());

                tribeRecords.AddOrUpdate(tribe.Uuid, tribe, (_, _) =>
                {
                    _logger.LogWarning("Replacing game object with guid {Guid} from tribe files. This may indicate duplicate entries in the database or a collision. Returning the new object.", tribe.Uuid);
                    return tribe;
                });
            }
            );
        }
        else
        {
            //attempt to parse from external .arktribe if not found in GameModeCustomBytes.
            using ArkTribeReader tribeReader = new ArkTribeReader(_logger, _settings);
            var tribeObjects = tribeReader.Read(_saveDirectory, cancellationToken);

            Parallel.ForEach(tribeObjects, new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxCores }, tribeGameObject =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                tribeRecords.AddOrUpdate(tribeGameObject.Uuid, tribeGameObject, (_, _) =>
                {
                    _logger.LogWarning("Replacing game object with guid {Guid} from tribe files. This may indicate duplicate entries in the database or a collision. Returning the new object.", tribeGameObject.Uuid);
                    return tribeGameObject;
                });
            }
            );
        }

        return tribeRecords;
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
        // Use just the GUID as the filename — the full save path is the same for every record
        // and concatenating it 100k+ times creates unnecessary unique string allocations.
        using var archive = new AsaArchive(_logger, objectData, _saveFile)
        {
            NameTable = saveHeader.NameTable,
            SaveVersion = saveHeader.SaveVersion
        };

        return GameObjectRecord.Read(archive, objectId);
    }


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

            using var archive = new AsaArchive(_logger, sqlBytes, $"{_saveFile}/custom/{key}");
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
