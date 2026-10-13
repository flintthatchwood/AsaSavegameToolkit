namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// String property (UTF-16 or ASCII depending on encoding).
/// </summary>
public class StrProperty : Property<string>
{
    /// <summary>
    /// Reads a complete StrProperty from the archive.
    /// The property tag has already been read.
    /// </summary>
    public static StrProperty Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new StrProperty
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }
    
    /// <summary>
    /// Reads just the string value from the archive (for array elements, map values, etc.).
    /// Use this when reading property values without tags, such as ArrayProperty&lt;StrProperty&gt; elements.
    /// </summary>
    public static string ReadValue(Readers.AsaArchive archive)
    {
        return archive.ReadString();
    }
}
