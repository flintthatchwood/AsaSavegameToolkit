namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// 32-bit unsigned integer property.
/// </summary>
public class UInt32Property : Property<uint>
{
    public static UInt32Property Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new UInt32Property
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }
    
    public static uint ReadValue(Readers.AsaArchive archive)
    {
        return archive.ReadUInt32();
    }
}
