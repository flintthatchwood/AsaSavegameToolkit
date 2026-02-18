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
        
        // Skip to name table
        archive.Position = nameTableOffset;
        
        // Read name table
        var nameTable = ReadNameTable(archive);
        
        return new SaveHeaderRecord
        {
            SaveVersion = saveVersion,
            NameTable = nameTable
        };
    }
    
    /// <summary>
    /// Reads a SaveHeader for save versions 11-13 (legacy format).
    /// </summary>
    private static SaveHeaderRecord ReadPre14(Readers.AsaArchive archive, short saveVersion)
    {
        // Version 11-13 format (no 8-byte unknown field)
        
        // Read name table offset
        var nameTableOffset = archive.ReadInt32();
        
        // Skip to name table
        archive.Position = nameTableOffset;
        
        // Read name table
        var nameTable = ReadNameTable(archive);
        
        return new SaveHeaderRecord
        {
            SaveVersion = saveVersion,
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
