using System;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Threading;
using System.Xml;

using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AsaSavegameToolkit.Plumbing.Readers;

/// <summary>
/// Extracts embedded creature game objects from cryopod custom item data.
/// Cryopods store binary blobs in <c>CustomItemDatas[0].CustomDataBytes.ByteArrays[n].Bytes</c>:
/// <list type="bullet">
///   <item><c>[0]</c> — dino + status component (zlib-compressed, then wildcard-compressed)</item>
///   <item><c>[1]</c> — saddle item properties (uncompressed, v13+)</item>
///   <item><c>[2]</c> — costume item properties (uncompressed, v14+ only)</item>
/// </list>
/// See <c>docs/RecordTypes/Cryopod.md</c> for the full format specification.
/// <para>
/// When <see cref="AsaReaderSettings.DebugOutputDirectory"/> is set, two extra files are
/// written beside the outer game-record file for each cryopod that contains a creature:
/// <list type="bullet">
///   <item><c>game/{xx}/{yy}/{guid}.cryo.bin</c> — raw compressed blob from the database</item>
///   <item><c>game/{xx}/{yy}/{guid}.cryo.expanded.bin</c> — fully decompressed wildcard payload</item>
/// </list>
/// </para>
/// </summary>
public sealed class CryopodReader : IDisposable
{
    private readonly ILogger _logger;
    private readonly AsaReaderSettings _settings;

    // Known name constants used by cryo payloads (matches Java implementation)
    private static readonly ReadOnlyDictionary<int, string> NameConstants = new(
        new Dictionary<int, string>
        {
            { 0, "TribeName" },
            { 1, "StrProperty" },
            { 2, "bServerInitializedDino" },
            { 3, "BoolProperty" },
            { 5, "FloatProperty" },
            { 6, "ColorSetIndices" },
            { 7, "ByteProperty" },
            { 8, "None" },
            { 9, "ColorSetNames" },
            { 10, "NameProperty" },
            { 11, "TamingTeamID" },
            { 12, "ObjectProperty" },
            { 13, "RequiredTameAffinity" },
            { 14, "TamingTeamID" },
            { 15, "IntProperty" },
            { 19, "StructProperty" },
            { 23, "DinoID1" },
            { 24, "UInt32Property" },
            { 25, "DinoID2" },
            { 31, "UploadedFromServerName" },
            { 32, "TamedOnServerName" },
            { 34, "MyCharacterStatusComponent" },
            { 36, "TargetingTeam" },
            { 38, "bReplicateGlobalStatusValues" },
            { 39, "bAllowLevelUps" },
            { 40, "bServerFirstInitialized" },
            { 41, "ExperiencePoints" },
            { 42, "CurrentStatusValues" },
            { 44, "ArrayProperty" },
            { 55, "bIsFemale" },
        });

    // Some cryo payloads (notably v0x0406 from save version 13) encode property type names incorrectly
    // (e.g., property name "TribeName" is repeated where a type like "StrProperty" should be).
    // To avoid failing the entire parse, we provide a small override map for common cryo properties
    // and a fallback reader for simple primitive property types.
    private static readonly ReadOnlyDictionary<string, string> KnownCryoPropertyTypeOverrides = new(
        new Dictionary<string, string>
        {
            // All of these are strings in observed payloads
            { "TribeName", "StrProperty" },
            { "UploadedFromServerName", "StrProperty" },
            { "TamedOnServerName", "StrProperty" },
            // Known booleans
            { "bServerInitializedDino", "BoolProperty" },
            { "bAllowLevelUps", "BoolProperty" },
            { "bReplicateGlobalStatusValues", "BoolProperty" },
            // Known ints
            { "TargetingTeam", "IntProperty" },
            { "TamingTeamID", "IntProperty" },
            // Known floats / doubles
            { "RequiredTameAffinity", "FloatProperty" },
            { "ExperiencePoints", "FloatProperty" },
        });

    public CryopodReader(ILogger? logger = default, AsaReaderSettings? settings = default)
    {
        _logger = logger ?? NullLogger.Instance;
        _settings = settings ?? AsaReaderSettings.None;
    }

    public void Dispose() { } // no unmanaged resources; IDisposable enables the `using` pattern

    /// <summary>
    /// Extracts all embedded cryo creatures from the provided cryopod object.
    /// When <see cref="AsaReaderSettings.DebugOutputDirectory"/> is set, the raw compressed blob
    /// is written to disk before decompression.
    /// </summary>
    public List<GameObjectRecord[]> ReadCryopodData(GameObjectRecord cryoPod, CancellationToken cancellationToken = default)
    {
        var customItemDatas = cryoPod.Properties.Get<object[]>("CustomItemDatas")?.OfType<CustomItemDataRecord>().ToArray();
        if (customItemDatas == null || customItemDatas.Length == 0)
        {
            return [];
        }

        var results = new List<GameObjectRecord[]>();

        foreach (var customItemData in customItemDatas)
        {
            // CustomDataBytes layout:
            //   [0] = dino + status component (zlib → wildcard compressed)
            //   [1] = saddle item properties (uncompressed)
            //   [2] = costume item properties (uncompressed)
            for (var index = 0; index < customItemData.CustomDataBytes.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var blob = customItemData.CustomDataBytes[index];
                if (blob.Length == 0)
                {
                    continue; // skip empty blobs
                }

                // Dump raw compressed blob beside the outer game record

                GameObjectRecord[] cryoObjects;
                try
                {
                    cryoObjects = ParseCryoBlob(cryoPod, blob, index, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse cryo blob for pod {PodName} ({PodUuid})", cryoPod.Name, cryoPod.Uuid);
                    throw new AsaDataException($"Failed to parse cryo blob for pod {cryoPod.Name} ({cryoPod.Uuid})", ex);
                }

                results.Add(cryoObjects);
            }
        }

        return results;
    }

    /// <summary>
    /// Parses the cryo blob into a list of <see cref="GameObjectRecord"/>.
    /// </summary>
    private GameObjectRecord[] ParseCryoBlob(GameObjectRecord cryoPod, byte[] bytes, int index, CancellationToken cancellationToken)
    {
        var version = BitConverter.ToInt32(bytes, 0);
        if (version == 0x01BEDEAD) // observed in v14 cryos; version is actually at offset 4, with a preceding magic value
        {
            version = BitConverter.ToInt32(bytes, 4);
        }

        var objectType = version & 0xFF00;  // 0406 -> 0400
        var dataStore = objectType == 0x0400;  // 0400 == dataStore v6 (dino). 00 == individual v7+ (saddle/costume)
        version &= 0x00FF; // mask to 8 bits.  0406 -> 06

        if (version < 7)
        {
            return ParseCryoBlobPre7(cryoPod, bytes, index, version, dataStore, cancellationToken);
        }

        var cryopodId = cryoPod.Uuid.ToString();
        var binName = $"game/{cryopodId[0]}/{cryopodId[1]}/{cryopodId[2]}/{cryopodId}[{index}].cryo.bin";

        byte[] payloadBytes;
        int? namesOffset = null;

        if (dataStore)
        {
            DumpDebugBytes(Path.ChangeExtension(binName, ".compressed.bin"), bytes);
            (payloadBytes, namesOffset) = DecompressBytes(bytes);
        }
        else
        {
            // For uncompressed blobs (saddle/costume), skip decompression and treat the blob after the header as the final payload directly.
            payloadBytes = bytes;
        }

        DumpDebugBytes(binName, payloadBytes);

        using var memoryStream = new MemoryStream(payloadBytes);
        using var archive = new AsaArchive(_logger, memoryStream, $"{cryoPod.Uuid}[{index}]")
        {
            SaveVersion = 14,
            AllowDynamicNameTable = dataStore,
            IsCryopod = true
        };

        if (namesOffset != null)
        {
            // Build name table from payload (offset table + known constants)
            archive.NameTable = ReadNameTable(archive, namesOffset.Value);
        }

        // Now parse object metadata manually (BinaryReader) to avoid alignment issues
        var results = new List<GameObjectRecord>();

        var metaObjects = new List<CryoObjectMeta>();
        if (dataStore)
        {
            // data stores have 2 unknown ints to skip
            archive.Position = 8;

            // only dataStore blobs contain multiple objects
            var objectCount = archive.ReadInt32();

            for (var i = 0; i < objectCount; i++)
            {
                metaObjects.Add(ReadCryoObjectMeta(archive));
            }
        }
        else
        {
            // individual blobs have their version header to skip (magic + version) + 2 unknown ints

            // For individual blobs, we only have one object with no metadata.
            // We add a placeholder meta with the default property offset after the header.
            metaObjects.Add(new CryoObjectMeta { PropertiesOffset = 16 });
        }

        // Now read properties for each object via stored offsets using AsaArchive for property decoding
        foreach (var meta in metaObjects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Construct FName for class (blueprint path string)
            var classFName = new FName(-1, 0, meta.Blueprint ?? "Unknown");

            // Prepare properties
            var props = new List<Property>();

            if (meta.PropertiesOffset <= 0 || meta.PropertiesOffset >= archive.Length)
            {
                _logger.LogWarning("Skipping properties for {Uuid} due to invalid offset {Offset} (archiveLen={Len})", meta.Uuid, meta.PropertiesOffset, archive.Length);
                continue;
            }

            var posBackup = archive.Position;
            archive.Position = meta.PropertiesOffset;

            // There should be a zero byte to skip.
            if (archive.Position < archive.Length)
            {
                var maybeZero = archive.ReadByte();
                if (maybeZero != 0)
                {
                    // if it wasn't zero, step back so it can be read as part of the first property tag
                    archive.Position -= 1;
                }
            }

            while (archive.Position < archive.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var prePos = archive.Position;
                var property = Property.Read(archive);

                // Guard against an infinite loop: if the parser returned without
                // advancing the position (e.g. tag.Size == 0 for an unrecognized
                // property type), we can't make progress — stop reading this object.
                if (property == null || archive.Position <= prePos)
                {
                    break; // end-of-properties sentinel ("None")
                }

                props.Add(property);
            }

            // After properties, an extra int is present (unknown); optional extra GUID
            if (archive.Position + sizeof(int) <= archive.Length)
            {
                _ = archive.ReadInt32();
            }

            archive.Position = posBackup;

            var gameObject = new GameObjectRecord(
                meta.Uuid,
                classFName,
                meta.Names,
                props,
                meta.DataFileIndex,
                ObjectTypeFlags.None,
                extraGuids: []
            );

            // If orientation was present, inject as synthetic property so downstream can use transforms if desired
            if (meta.Rotation.HasValue)
            {
                gameObject.Properties.Add(CreateSyntheticStructProperty("CryoStoredRotation", meta.Rotation.Value));
            }

            results.Add(gameObject);
        }

        return [.. results];
    }

    private GameObjectRecord[] ParseCryoBlobPre7(GameObjectRecord cryoPod, byte[] bytes, int index, int version, bool dataStore, CancellationToken cancellationToken)
    {
        var cryopodId = cryoPod.Uuid.ToString();
        var binName = $"game/{cryopodId[0]}/{cryopodId[1]}/{cryopodId[2]}/{cryopodId}[{index}].cryo.bin";

        byte[] payloadBytes;
        int? namesOffset = null;

        if (dataStore)
        {
            DumpDebugBytes(Path.ChangeExtension(binName, ".compressed.bin"), bytes);
            (payloadBytes, namesOffset) = DecompressBytes(bytes);
        }
        else
        {
            // For uncompressed blobs (saddle/costume), skip decompression and treat the blob after the header as the final payload directly.
            payloadBytes = bytes;
        }

        DumpDebugBytes(binName, payloadBytes);

        using var memoryStream = new MemoryStream(payloadBytes);
        using var archive = new AsaArchive(_logger, memoryStream, $"{cryoPod.Uuid}[{index}]")
        {
            SaveVersion = 13,
            AllowDynamicNameTable = dataStore,
            IsCryopod = true
        };

        if (namesOffset != null)
        {
            // Build name table from payload (offset table + known constants)
            archive.NameTable = ReadNameTable(archive, namesOffset.Value);
        }

        // Now parse object metadata manually (BinaryReader) to avoid alignment issues
        var results = new List<GameObjectRecord>();

        List<CryoObjectMeta> metaObjects = [];
        if (dataStore)
        {
            archive.Position = 0;

            // only data stores contains multiple objects
            var objectCount = archive.ReadInt32();

            for (var i = 0; i < objectCount; i++)
            {
                metaObjects.Add(ReadCryoObjectMeta(archive));
            }
        }
        else
        {
            // For individual blobs, we only have one object with no metadata.
            // We add a placeholder meta with the default property offset after the int32 version header.
            metaObjects.Add(new CryoObjectMeta { PropertiesOffset = 4 });
        }

        // Now read properties for each object via stored offsets using AsaArchive for property decoding
        foreach (var meta in metaObjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Construct FName for class (blueprint path string)
            var classFName = new FName(-1, 0, meta.Blueprint ?? "Unknown");

            // Prepare properties
            var props = new List<Property>();

            if (meta.PropertiesOffset <= 0 || meta.PropertiesOffset >= archive.Length)
            {
                _logger.LogWarning("Skipping properties for {Uuid} due to invalid offset {Offset} (archiveLen={Len})", meta.Uuid, meta.PropertiesOffset, archive.Length);
                continue;
            }

            var posBackup = archive.Position;
            archive.Position = meta.PropertiesOffset;

            while (archive.Position < archive.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var prePos = archive.Position;
                var property = Property.Read(archive);

                // Guard against an infinite loop: if the parser returned without
                // advancing the position (e.g. tag.Size == 0 for an unrecognized
                // property type), we can't make progress — stop reading this object.
                if (property == null || archive.Position <= prePos)
                {
                    break; // end-of-properties sentinel ("None")

                }

                props.Add(property);
            }

            // After properties, an extra int is present (unknown); optional extra GUID
            if (archive.Position + sizeof(int) <= archive.Length)
            {
                _ = archive.ReadInt32();
            }

            archive.Position = posBackup;

            var gameObject = new GameObjectRecord(
                meta.Uuid,
                classFName,
                meta.Names,
                props,
                meta.DataFileIndex,
                ObjectTypeFlags.None,
                extraGuids: []
            );

            // If orientation was present, inject as synthetic property so downstream can use transforms if desired
            if (meta.Rotation.HasValue)
            {
                gameObject.Properties.Add(CreateSyntheticStructProperty("CryoStoredRotation", meta.Rotation.Value));
            }

            results.Add(gameObject);
        }

        return [.. results];
    }


    /// <summary>
    /// Decompresses a cryo blob (zlib + wildcard) and returns the final payload bytes along with header metadata.
    /// </summary>
    private (byte[] expandedBytes, int namesOffset) DecompressBytes(byte[] cryoBytes)
    {
        using var mem = new MemoryStream(cryoBytes);
        using var reader = new BinaryReader(mem);

        reader.ReadInt32(); // e.g. 0x0406
        var inflatedSize = reader.ReadInt32();
        var namesOffset = reader.ReadInt32();
        var payload = reader.ReadBytes((int)(mem.Length - mem.Position));
        byte[] expanded;

        if (payload.Length == inflatedSize)
        {
            expanded = payload; // no compression; use as-is
        }
        else
        {
            // Stage 1: zlib (use ZLibStream; DeflateStream cannot handle the zlib header)
            using var zlibStream = new ZLibStream(new MemoryStream(payload), CompressionMode.Decompress);
            using var ms = new MemoryStream();
            zlibStream.CopyTo(ms);
            var decompressed = ms.ToArray();

            if (inflatedSize > 0 && decompressed.Length != inflatedSize)
            {
                _logger.LogDebug("Cryo inflated size mismatch: header {InflatedSize} vs actual {Actual}", inflatedSize, decompressed.Length);
            }

            // Stage 2: wildcard inflater
            expanded = WildcardInflater.Inflate(decompressed);
        }

        return (expanded, namesOffset);
    }


    private void DumpDebugBytes(string relativePath, Span<byte> data)
    {
        var dir = _settings.DebugOutputDirectory;
        if (dir == null) return;

        var fullPath = Path.Combine(dir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, data.ToArray());
    }

    private static CryoObjectMeta ReadCryoObjectMeta(AsaArchive archive)
    {
        var startPos = archive.Position;
        var uuid = archive.ReadGuid();
        var blueprint = archive.ReadString();
        archive.ReadInt32(); // unknown0 expect 0

        var names = archive.ReadStringArray();

        archive.ReadInt32(); // fromDataFile (bool as int32) -- see Java readBoolean()
        archive.ReadInt32(); // dataFileIndex
        FRotator? rotation = null;
        if (archive.ReadInt32() != 0) // hasRotation (bool as int32)
        {
            // ArkRotator stores pitch/yaw/roll as three doubles (not floats)
            var pitch = archive.ReadDouble();
            var yaw = archive.ReadDouble();
            var roll = archive.ReadDouble();
            rotation = new FRotator(yaw, pitch, roll);
        }

        // Following fields differ from main save objects; in observed payloads, we see immediately an int (properties offset) and trailing zero.
        var propertiesOffset = archive.ReadInt32();

        archive.ReadInt32(); // expect 0

        // basic sanity: positions should advance
        return archive.Position > startPos
            ? new CryoObjectMeta(uuid, blueprint, names, /*dataFileIndex*/ 0, propertiesOffset, rotation)
            : throw new InvalidDataException($"Cryo meta parsing did not advance stream (start {startPos}, end {archive.Position})");
    }

    private static Dictionary<int, string> ReadNameTable(AsaArchive archive, int namesOffset)
    {
        var table = new Dictionary<int, string>();

        archive.Position = namesOffset;

        var nameCount = archive.ReadInt32();
        // The minimum size for a string entry is 4 bytes (empty string)
        if (nameCount < 0 || nameCount * 4 > archive.RemainingLength)
        {
            throw new AsaDataException($"Invalid name count read at offset {archive.Position - 4} of {archive.FileName}");
        }

        for (int i = 0; i < nameCount; i++)
        {
            var name = archive.ReadString();
            table[i | 0x10000000] = name;
        }

        foreach (var kvp in NameConstants)
        {
            // include both raw and high-bit variants to be safe
            var rawKey = kvp.Key;
            var hiKey = kvp.Key | 0x10000000;
            table.TryAdd(rawKey, kvp.Value);
            table.TryAdd(hiKey, kvp.Value);
        }

        return table;
    }

    private static StructProperty CreateSyntheticStructProperty(string name, FRotator rotation)
    {
        var tag = new PropertyTag
        {
            Name = new FName(-1, 0, name),
            Type = FPropertyTypeName.Create(new FName(-1, 0, "StructProperty"), new FName(-1, 0, "Rotator")),
            Size = 24,
            ArrayIndex = 0,
            Flags = 0
        };
        return new StructProperty
        {
            Tag = tag,
            Value = rotation
        };
    }

    private class CryoObjectMeta
    {
        public CryoObjectMeta() { }

        public CryoObjectMeta(Guid uuid, string? blueprint, string[] names, int dataFileIndex, int propertiesOffset, FRotator? rotation)
        {
            Uuid = uuid;
            Blueprint = blueprint;
            Names = names;
            DataFileIndex = dataFileIndex;
            PropertiesOffset = propertiesOffset;
            Rotation = rotation;
        }

        public Guid Uuid { get; set; }
        public string? Blueprint { get; set; }
        public string[] Names { get; set; } = [];
        public int DataFileIndex { get; set; }
        public int PropertiesOffset { get; set; }
        public FRotator? Rotation { get; set; }
    }
}
