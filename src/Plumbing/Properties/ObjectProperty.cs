namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// Reference to another object in the save file.
/// The value is typically a GUID that can be used to look up the object.
/// </summary>
public class ObjectProperty : Property<Primitives.ObjectReference>
{
    /// <summary>
    /// Reads a complete ObjectProperty from the archive.
    /// The property tag has already been read.
    /// </summary>
    public static ObjectProperty Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new ObjectProperty
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }
    
    /// <summary>
    /// Reads just the ObjectReference value from the archive (for array elements, map values, etc.).
    /// Use this when reading property values without tags, such as ArrayProperty&lt;ObjectProperty&gt; elements.
    /// </summary>
    public static Primitives.ObjectReference ReadValue(Readers.AsaArchive archive)
    {
        return Primitives.ObjectReference.Read(archive);
    }
}
