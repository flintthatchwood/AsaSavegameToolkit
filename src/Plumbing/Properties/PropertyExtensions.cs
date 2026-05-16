namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// Property extensions structure (v14+ only).
/// Serialized when property tag Flags has bit 0x04 set.
/// Contains metadata for property overrides and experimental features.
/// </summary>
public class PropertyExtensions
{
    /// <summary>
    /// Extension flags byte indicating which extensions are present.
    /// Bit 0x02: OverridableInformation present
    /// Bit 0x01: Reserved for future use
    /// </summary>
    public byte ExtensionFlags { get; set; }
    
    /// <summary>
    /// Override operation type (if ExtensionFlags & 0x02).
    /// Describes how this property overrides inherited/default values.
    /// </summary>
    public OverriddenPropertyOperation? OverrideOperation { get; set; }
    
    /// <summary>
    /// Whether this property has experimental overridable logic flag set (if ExtensionFlags & 0x02).
    /// </summary>
    public bool ExperimentalOverridableLogic { get; set; }
    
    /// <summary>
    /// Reads property extensions from the archive.
    /// </summary>
    internal static PropertyExtensions Read(Readers.AsaArchive archive)
    {
        var extensions = new PropertyExtensions
        {
            // Read extension flags
            ExtensionFlags = archive.ReadByte()
        };

        // Read OverridableInformation if present
        if ((extensions.ExtensionFlags & 0x02) != 0)
        {
            extensions.OverrideOperation = (OverriddenPropertyOperation)archive.ReadByte();
            extensions.ExperimentalOverridableLogic = archive.ReadByte() != 0;
        }
        
        return extensions;
    }
}

/// <summary>
/// Describes how a property overrides inherited or default values.
/// Matches EOverriddenPropertyOperation from UE source.
/// </summary>
public enum OverriddenPropertyOperation : byte
{
    None = 0,
    Modified = 1,
    Replace = 2,
    Add = 3,
    Remove = 4
}
