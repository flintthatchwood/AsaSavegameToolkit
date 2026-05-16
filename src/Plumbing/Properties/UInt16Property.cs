namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// 16-bit unsigned integer property.
/// </summary>
public class UInt16Property : Property<ushort>
{
    public static UInt16Property Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new UInt16Property
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }
    
    public static ushort ReadValue(Readers.AsaArchive archive)
    {
        return archive.ReadUInt16();
    }
}
