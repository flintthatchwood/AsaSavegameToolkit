namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// Set property - unique elements of a single type.
/// Format: SetProperty(ElementType)
/// Access element type via tag.Type.Parameters[0].
/// </summary>
public class SetProperty : Property<List<object>>
{
    /// <summary>
    /// Number of elements that were removed from the set (NumKeysToRemove).
    /// This field exists for compatibility with Unreal's TSet serialization format,
    /// but in ARK save files it appears to always be 0. Removed elements are not serialized.
    /// </summary>
    public int NumKeysToRemove { get; init; }

    public override string ToString() => $"{Tag}: {Value.Count} elements";


    public static SetProperty Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        // Read removed element count
        var numKeysToRemove = archive.ReadInt32();
        
        return new SetProperty
        {
            Tag = tag,
            NumKeysToRemove = numKeysToRemove,
            Value = ReadValue(archive, tag)
        };
    }
    
    /// <summary>
    /// Reads the set elements.
    /// Format: Int32 (count) + elements
    /// </summary>
    public static List<object> ReadValue(Readers.AsaArchive archive, PropertyTag tag)
    {
        // Read element count
        var count = archive.ReadInt32();
        
        // The minimum size for an entry is 1 bytes (Set<byte>) 
        if (count > archive.RemainingLength)
        {
            throw new AsaDataException($"Invalid set entry count read at offset {archive.Position - 4} of {archive.FileName}");
        }
        
        var elements = new List<object>(count);
        
        // Get element type from tag
        var elementType = tag.Type.GetParameter(0);
        if (elementType == null)
            throw new InvalidOperationException($"SetProperty must have an ElementType parameter");
        
        // Read each element based on element type
        for (int i = 0; i < count; i++)
        {
            var element = Property.ReadValue(archive, elementType);
            elements.Add(element);
        }
        
        return elements;
    }
}
