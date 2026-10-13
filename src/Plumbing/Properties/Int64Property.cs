namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// 64-bit signed integer property.
/// </summary>
public class Int64Property : Property<long>
{
    public static Int64Property Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new Int64Property
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }
    
    public static long ReadValue(Readers.AsaArchive archive)
    {
        return archive.ReadInt64();
    }
}
