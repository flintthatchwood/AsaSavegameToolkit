namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// 32-bit signed integer property.
/// </summary>
public class IntProperty : Property<int>
{
    /// <summary>
    /// Reads a complete IntProperty from the archive.
    /// The property tag has already been read.
    /// </summary>
    public static IntProperty Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new IntProperty
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }
    
    /// <summary>
    /// Reads just the int value from the archive (for array elements, map values, etc.).
    /// Use this when reading property values without tags, such as ArrayProperty&lt;IntProperty&gt; elements.
    /// </summary>
    public static int ReadValue(Readers.AsaArchive archive)
    {
        return archive.ReadInt32();
    }
}
