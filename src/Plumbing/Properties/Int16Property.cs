namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// 16-bit signed integer property.
/// </summary>
public class Int16Property : Property<short>
{
    public static Int16Property Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new Int16Property
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }
    
    public static short ReadValue(Readers.AsaArchive archive)
    {
        return archive.ReadInt16();
    }
}
