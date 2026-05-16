namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// Map property - key/value pairs.
/// Format: MapProperty(KeyType, ValueType)
/// Access key/value types via tag.Type.Parameters[0] and [1].
/// </summary>
public class MapProperty : Property<List<MapEntry>>
{
    public override string ToString() => $"{Tag}: {Value.Count} elements";

    public static MapProperty Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new MapProperty
        {
            Tag = tag,
            Value = ReadValue(archive, tag)
        };
    }

    /// <summary>
    /// Reads the map entries.
    /// Format: Int32 (skip, always 0) + Int32 (count) + entries
    /// </summary>
    public static List<MapEntry> ReadValue(Readers.AsaArchive archive, PropertyTag tag)
    {
        // Skip/unknown int (always 0)
        var skip = archive.ReadInt32();
        
        // Entry count
        var count = archive.ReadInt32();
        
        // The minimum size for an entry is 2 bytes (Map<byte, byte>) 
        if (count * 2 > archive.RemainingLength)
        {
            throw new AsaDataException($"Invalid map entry count read at offset {archive.Position - 4} of {archive.FileName}");
        }

        var entries = new List<MapEntry>(count);
        
        // Get key and value types from tag
        var keyType = tag.Type.GetParameter(0);
        var valueType = tag.Type.GetParameter(1);
        
        if (keyType == null || valueType == null)
            throw new InvalidOperationException($"MapProperty must have both KeyType and ValueType parameters");
        
        for (int i = 0; i < count; i++)
        {
            var entry = new MapEntry
            {
                Key = ReadValue(archive, keyType),
                Value = ReadValue(archive, valueType)
            };

            entries.Add(entry);
        }
        
        return entries;
    }
}

/// <summary>
/// Represents a single entry in a MapProperty.
/// In v13: Value is a list of properties (until "None" terminator).
/// In v14: Value is a single typed value.
/// </summary>
public class MapEntry
{
    /// <summary>
    /// The key (type determined by MapProperty's KeyType parameter).
    /// Stored as string representation for simplicity.
    /// </summary>
    public required object Key { get; init; }

    /// <summary>
    /// The value properties for this map entry (v13 format).
    /// In v13, each map value is a list of properties.
    /// In v14, this will have a single property entry.
    /// </summary>
    public required object Value { get; init; }
}
