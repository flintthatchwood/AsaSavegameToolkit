namespace AsaSavegameToolkit.Plumbing.Records;

/// <summary>
/// Represents the SaveHeader record from the custom table.
/// Contains metadata needed to parse the rest of the save file.
/// </summary>
public class SaveHeaderRecord
{
    /// <summary>
    /// Save file format version (determines parsing logic).
    /// </summary>
    public required short SaveVersion { get; init; }

    public required double GameTime { get; init; }

    /// <summary>
    /// The name of the map/level for this save file.
    /// </summary>
    public string MapName { get; init; } = "Unknown";
    
    /// <summary>
    /// Name table - maps integer IDs to string names.
    /// Used throughout the save file to reduce duplication.
    /// </summary>
    public required Dictionary<int, string> NameTable { get; init; }
    
    /// <summary>
    /// Reads a SaveHeader from the archive for save version 14+ (current format).
    /// Automatically delegates to ReadPre14() for older save versions.
    /// Full implementation to come - this is minimal for tests.
    /// </summary>
    public static SaveHeaderRecord Read(Readers.AsaArchive archive)
    {
        // Read save version first
        var saveVersion = archive.ReadInt16();
        
        // Early delegation for older versions
        if (saveVersion < 14)
        {
            return ReadPre14(archive, saveVersion);
        }
        
        // Version 14+ format
        archive.ReadInt64(); // Unknown1 + Unknown2 (8 bytes in v14+)
        
        // Read name table offset
        var nameTableOffset = archive.ReadInt32();

        // Read game time
        var gameTime = archive.ReadDouble();

        // Read map name
        _ = archive.ReadBytes(8);
        var mapName = archive.ReadString() ?? "Unknown";

        // Skip to name table
        archive.Position = nameTableOffset;
        
        // Read name table
        var nameTable = ReadNameTable(archive);
        
        return new SaveHeaderRecord
        {
            SaveVersion = saveVersion,
            GameTime = gameTime,
            MapName = mapName,
            NameTable = nameTable
        };
    }
    
    /// <summary>
    /// Reads a SaveHeader for save versions 12-13, delegating older layouts to ReadPre12().
    /// </summary>
    private static SaveHeaderRecord ReadPre14(Readers.AsaArchive archive, short saveVersion)
    {
        if (saveVersion < 12)
        {
            return ReadPre12(archive, saveVersion);
        }
        
        // Read name table offset
        var nameTableOffset = archive.ReadInt32();

        // Read game time
        var gameTime = archive.ReadDouble();

        _ = archive.ReadInt32();

        var dataCount = archive.ReadInt32();
        var mapName = archive.ReadString() ?? "Unknown";

        // Skip to name table
        archive.Position = nameTableOffset;
        
        // Read name table
        var nameTable = ReadNameTable(archive);
        
        return new SaveHeaderRecord
        {
            SaveVersion = saveVersion,
            GameTime = gameTime,
            MapName = mapName,
            NameTable = nameTable
        };
    }

    private static SaveHeaderRecord ReadPre12(Readers.AsaArchive archive, short saveVersion)
    {
        var nameTableOffset = archive.ReadInt32();
        var gameTime = archive.ReadDouble();
        _ = archive.ReadInt32();
        var mapName = archive.ReadString() ?? "Unknown";

        archive.Position = nameTableOffset;
        var nameTable = ReadNameTable(archive);

        return new SaveHeaderRecord
        {
            SaveVersion = saveVersion,
            GameTime = gameTime,
            MapName = mapName,
            NameTable = nameTable
        };
    }
    
    /// <summary>
    /// Reads the name table (same format across all versions).
    /// </summary>
    private static Dictionary<int, string> ReadNameTable(Readers.AsaArchive archive)
    {
        var nameTable = new Dictionary<int, string>();
        int nameCount = archive.ReadInt32();
        // The minimum size for a name entry is 8 bytes (index + empty string)
        if (nameCount < 0 || nameCount * 8 > archive.RemainingLength)
        {
            throw new AsaDataException($"Invalid name count read at offset {archive.Position - 4} of {archive.FileName}");
        }
        
        for (int i = 0; i < nameCount; i++)
        {
            int nameIndex = archive.ReadInt32();
            string nameString = archive.ReadString();
            nameTable[nameIndex] = nameString;
        }
        
        return nameTable;
    }
}
