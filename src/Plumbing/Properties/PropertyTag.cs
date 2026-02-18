using AsaSavegameToolkit.Plumbing.Primitives;

namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// Represents property metadata in ARK save data.
/// This is the fundamental unit of serialization - all game objects are
/// composed of lists of properties terminated by a "None" property name.
/// The actual property value is read separately by type-specific readers.
/// </summary>
public class PropertyTag
{
    /// <summary>
    /// Property name (e.g., "TargetingTeam", "Health").
    /// </summary>
    public FName Name { get; set; }
    
    /// <summary>
    /// Complete type information including generic parameters.
    /// In v14+: Read directly from archive as FPropertyTypeName tree.
    /// In v13-: Constructed from property-specific type metadata.
    /// Access root type name via Type.TypeName (e.g., "IntProperty", "StructProperty").
    /// Examples:
    /// - IntProperty
    /// - ByteProperty(EPrimalEquipmentType)
    /// - ArrayProperty(IntProperty)
    /// - MapProperty(NameProperty, FloatProperty)
    /// - StructProperty(Vector, guid)
    /// </summary>
    public required FPropertyTypeName Type { get; set; }
    
    /// <summary>
    /// Size of the property value data in bytes (Int32 in all versions).
    /// </summary>
    public long Size { get; set; }
    
    /// <summary>
    /// Array index for sparse arrays (e.g., ColorRegions[2], [4], [5]).
    /// In v13: Always present (Int32).
    /// In v14+: Only present if Flags & 0x01.
    /// Default is 0 for non-array properties.
    /// </summary>
    public int ArrayIndex { get; set; }
    
    /// <summary>
    /// Property flags byte (v14+ only).
    /// Bit 0x01: HasArrayIndex
    /// Bit 0x02: HasPropertyGuid
    /// Bit 0x04: HasPropertyExtensions
    /// Bit 0x08: HasBinaryOrNativeSerialize
    /// Bit 0x10: BoolValue (for BoolProperty)
    /// Bit 0x20: SkippedSerialize
    /// </summary>
    public byte Flags { get; set; }
    
    /// <summary>
    /// Optional property GUID (v14+, if Flags & 0x02).
    /// Used for blueprint property identity across renames.
    /// </summary>
    public Guid? PropertyGuid { get; set; }
    
    /// <summary>
    /// Optional property extensions (v14+, if Flags & 0x04).
    /// Contains override information and experimental flags.
    /// </summary>
    public PropertyExtensions? Extensions { get; set; }

    public string GetDisplayName()
    {
        var propertyName = Name.ToString();

        return ArrayIndex > 0 ? $"{propertyName}[{ArrayIndex}]" : propertyName;
    }

    public override string ToString()
    {
        return $"{Type} {GetDisplayName()}";
    }

    /// <summary>
    /// Reads a property tag from the archive for save version 14+ (current format).
    /// Returns null if the property name is "None" (end of property list).
    /// Automatically delegates to ReadPre14() for older save versions.
    /// </summary>
    public static PropertyTag? Read(Readers.AsaArchive archive)
    {
        // Early delegation for older versions
        if (archive.SaveVersion < 14)
        {
            return ReadPre14(archive);
        }
        
        // Read property name
        var name = archive.ReadFName();
        
        // "None" signals end of property list
        if (name.FullName == "None")
        {
            return null;
        }
        
        // Read property type name (full FPropertyTypeName tree in v14+)
        var type = archive.ReadPropertyTypeName();
        
        // Read property size (Int32 in all versions)
        var size = archive.ReadInt32();
        
        // Read flags byte
        var flags = archive.ReadByte();
        
        // Conditional fields based on flags
        int arrayIndex = 0;
        if ((flags & 0x01) != 0) // HasArrayIndex
        {
            arrayIndex = archive.ReadInt32();
        }
        
        Guid? propertyGuid = null;
        if ((flags & 0x02) != 0) // HasPropertyGuid
        {
            propertyGuid = archive.ReadGuid();
        }
        
        PropertyExtensions? extensions = null;
        if ((flags & 0x04) != 0) // HasPropertyExtensions
        {
            extensions = PropertyExtensions.Read(archive);
        }
        
        // Note: We don't read the value here - that's done by type-specific readers
        // The value starts at current position and is 'size' bytes long
        
        return new PropertyTag
        {
            Name = name,
            Type = type,
            Size = size,
            ArrayIndex = arrayIndex,
            Flags = flags,
            PropertyGuid = propertyGuid,
            Extensions = extensions
        };
    }
    
    /// <summary>
    /// Reads a property tag for save versions 11-13 (legacy format).
    /// Returns null if the property name is "None" (end of property list).
    /// In v13, type-specific metadata is read here and converted to FPropertyTypeName format.
    /// </summary>
    private static PropertyTag? ReadPre14(Readers.AsaArchive archive)
    {
        // Read property name
        var name = archive.ReadFName();
        
        // "None" signals end of property list
        if (name == FName.None)
        {
            return null;
        }
        
        // Read property type (just FName in v13)
        var rootType = archive.ReadFName();
        
        // Read property size (Int32 in v13)
        var size = archive.ReadInt32();
        
        // Read array index (always present in v13)
        var arrayIndex = archive.ReadInt32();

        // Read type-specific metadata and construct FPropertyTypeName
        // Different property types have different metadata after ArrayIndex
        byte flags;
        FPropertyTypeName type;
        
        switch (rootType.FullName)
        {
            case "BoolProperty":
                // BoolProperty is special: has BoolVal (Int16) instead of Flags byte
                // Value is embedded in tag (Size=0), not after it
                var boolVal = archive.ReadInt16();
                flags = boolVal != 0 ? (byte)0x10 : (byte)0;
                type = FPropertyTypeName.Create(rootType);
                break;
                
            case "ByteProperty":
                // ByteProperty can be an enum: reads EnumName + Flags
                var enumName = archive.ReadFName();
                flags = archive.ReadByte();
                
                if (enumName == FName.None || string.IsNullOrEmpty(enumName.FullName))
                {
                    // Simple byte, no enum
                    type = FPropertyTypeName.Create(rootType);
                }
                else
                {
                    // Enum byte: ByteProperty(EnumName)
                    type = FPropertyTypeName.Create(rootType, enumName);
                }
                break;

            case "ArrayProperty" or "SetProperty" or "StructProperty":
                // All read single InnerType/StructName parameter: FName + Flags
                // ArrayProperty(InnerType), SetProperty(InnerType), StructProperty(StructName)
                var innerType = archive.ReadFName();
                flags = archive.ReadByte();
                type = FPropertyTypeName.Create(rootType, innerType);
                break;

            case "MapProperty":
                // MapProperty(KeyType, ValueType)
                var keyType = archive.ReadFName();
                var valueType = archive.ReadFName();
                flags = archive.ReadByte();
                type = FPropertyTypeName.Create(rootType, keyType, valueType);
                break;
                
            default:
                // Simple property type: IntProperty, FloatProperty, StrProperty, etc.
                flags = archive.ReadByte();
                type = FPropertyTypeName.Create(rootType);
                break;
        }

        return new PropertyTag
        {
            Name = name,
            Type = type,
            Size = size,
            ArrayIndex = arrayIndex,
            Flags = flags
        };
    }
}