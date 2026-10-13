namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// Name property (FName from name table).
/// </summary>
public class NameProperty : Property<Primitives.FName>
{
    /// <summary>
    /// Reads a complete NameProperty from the archive.
    /// The property tag has already been read.
    /// </summary>
    public static NameProperty Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new NameProperty
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }
    
    /// <summary>
    /// Reads just the FName value from the archive (for array elements, map values, etc.).
    /// Use this when reading property values without tags, such as ArrayProperty&lt;NameProperty&gt; elements.
    /// </summary>
    public static Primitives.FName ReadValue(Readers.AsaArchive archive)
    {
        return archive.ReadFName();
    }
}
