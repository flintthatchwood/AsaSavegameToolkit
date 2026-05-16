namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// 64-bit unsigned integer property.
/// </summary>
public class UInt64Property : Property<ulong>
{
    public static UInt64Property Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new UInt64Property
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }
    
    public static ulong ReadValue(Readers.AsaArchive archive)
    {
        return archive.ReadUInt64();
    }
}
