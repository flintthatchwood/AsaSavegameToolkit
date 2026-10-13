namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// Boolean property.
/// BoolProperty is special: the value is stored in the PropertyTag's Flags field (bit 0x10),
/// not in a separate data section like other properties.
/// </summary>
public class BoolProperty : Property<bool>
{
    public static BoolProperty Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        // BoolProperty has no data section - the value is in the tag's Flags
        // Bit 0x10 in Flags indicates true/false
        bool value = (tag.Flags & 0x10) != 0;
        
        return new BoolProperty
        {
            Tag = tag,
            Value = value
        };
    }
    
    /// <summary>
    /// Reads just the value without a tag (for array elements, etc.).
    /// For untagged bool values, read as a single byte (0 or 1).
    /// </summary>
    public static bool ReadValue(Readers.AsaArchive archive)
    {
        return archive.ReadByte() != 0;
    }
}
