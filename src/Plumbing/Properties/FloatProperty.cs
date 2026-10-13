namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// 32-bit floating point property.
/// </summary>
public class FloatProperty : Property<float>
{
    /// <summary>
    /// Reads a complete FloatProperty from the archive.
    /// The property tag has already been read.
    /// </summary>
    public static FloatProperty Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new FloatProperty
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }
    
    /// <summary>
    /// Reads just the float value from the archive (for array elements, map values, etc.).
    /// Use this when reading property values without tags, such as ArrayProperty&lt;FloatProperty&gt; elements.
    /// </summary>
    public static float ReadValue(Readers.AsaArchive archive)
    {
        return archive.ReadFloat();
    }
}
