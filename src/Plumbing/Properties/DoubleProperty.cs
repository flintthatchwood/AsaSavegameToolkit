namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// 64-bit floating point property.
/// </summary>
public class DoubleProperty : Property<double>
{
    /// <summary>
    /// Reads a complete DoubleProperty from the archive.
    /// The property tag has already been read.
    /// </summary>
    public static DoubleProperty Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new DoubleProperty
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }
    
    /// <summary>
    /// Reads just the double value from the archive (for array elements, map values, etc.).
    /// Use this when reading property values without tags, such as ArrayProperty&lt;DoubleProperty&gt; elements.
    /// </summary>
    public static double ReadValue(Readers.AsaArchive archive)
    {
        return archive.ReadDouble();
    }
}
