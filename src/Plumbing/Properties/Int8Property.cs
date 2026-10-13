namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// 8-bit signed integer property.
/// </summary>
public class Int8Property : Property<sbyte>
{
    public static Int8Property Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new Int8Property
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }
    
    public static sbyte ReadValue(Readers.AsaArchive archive)
    {
        return (sbyte)archive.ReadByte();
    }
}
