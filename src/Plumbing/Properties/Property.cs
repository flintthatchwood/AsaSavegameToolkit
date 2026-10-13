using AsaSavegameToolkit.Plumbing.Readers;

namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// Base class for all properties in ARK save data.
/// Preserves complete metadata from the binary format.
/// </summary>
public abstract class Property
{
    /// <summary>
    /// Property metadata (name, type, size, array index, flags, etc.).
    /// This contains all the structural information from the binary format.
    /// </summary>
    public required PropertyTag Tag { get; set; }
    
    /// <summary>
    /// Returns the property value as a boxed object.
    /// Useful for generic exploration and display without knowing the concrete type.
    /// Note: boxes value types (int, float, etc.) - avoid in hot paths.
    /// </summary>
    public abstract object GetValue();



    /// <summary>
    /// Reads the next property from the archive.
    /// Returns null if the property name is "None" (end of property list).
    /// Automatically dispatches to the correct property type based on the tag.
    /// </summary>
    public static Property? Read(AsaArchive archive)
    {
        var start = archive.Position;

        // Read the property tag (metadata)
        var tag = PropertyTag.Read(archive);
        if (tag == null)
        {
            return null;  // "None" terminator
        }

        var expectedEnd = archive.Position + tag.Size;

        if (tag.Type.TypeName.FullName == "StructProperty" && archive.SaveVersion < 14 &! archive.IsArkFile)
        {
            // pre-14 structs include a guid in their header that's not really part of the struct value, but we don't
            // include it as part of the tag read
            expectedEnd += 16;
        }

        // Dispatch to type-specific reader
        var property = tag.Type.TypeName.FullName switch
        {
            "IntProperty" => IntProperty.Read(archive, tag),
            "Int8Property" => Int8Property.Read(archive, tag),
            "Int16Property" => Int16Property.Read(archive, tag),
            "Int64Property" => Int64Property.Read(archive, tag),
            "ByteProperty" => ByteProperty.Read(archive, tag),
            "UInt16Property" => UInt16Property.Read(archive, tag),
            "UInt32Property" => UInt32Property.Read(archive, tag),
            "UInt64Property" => UInt64Property.Read(archive, tag),
            "FloatProperty" => FloatProperty.Read(archive, tag),
            "DoubleProperty" => DoubleProperty.Read(archive, tag),
            "BoolProperty" => BoolProperty.Read(archive, tag),
            "StrProperty" => StrProperty.Read(archive, tag),
            "NameProperty" => NameProperty.Read(archive, tag),
            "ObjectProperty" => archive.IsCryopod && tag.Size <= 8
                ? CryopodObjectProperty.Read(archive, tag)
                : ObjectProperty.Read(archive, tag),
            "SoftObjectProperty" => SoftObjectProperty.Read(archive, tag),
            "MapProperty" => MapProperty.Read(archive, tag),
            "StructProperty" => StructProperty.Read(archive, tag),
            "SetProperty" => SetProperty.Read(archive, tag),
            "ArrayProperty" => ArrayProperty.Read(archive, tag),
            // More property types will be added here
            _ => throw new NotSupportedException($"Unknown property type: {tag.Type.TypeName.FullName}")
        };


        if (archive.Position < expectedEnd)
        {
            archive.Position = expectedEnd;
            throw new AsaDataException($"Property parsing read less bytes than expected at offset {start} in {archive.FileName}");
        }
        else if (archive.Position > expectedEnd)
        {
            if (!(archive.IsArkFile || archive.IsCryopod))
            {
                archive.Position = expectedEnd;
                throw new AsaDataException($"Property parsing read more bytes than expected at offset {start} in {archive.FileName}");

            }
        }

        return property;
    }
    
    /// <summary>
    /// Reads a property value without a tag (for collection elements, map values, etc.).
    /// Dispatches to the appropriate property type's ReadValue() method based on the type name.
    /// Used by ArrayProperty, SetProperty, and MapProperty when reading elements/values.
    /// </summary>
    public static object ReadValue(Readers.AsaArchive archive, Primitives.FPropertyTypeName propertyType)
    {
        return propertyType.TypeName.FullName switch
        {
            "IntProperty" => IntProperty.ReadValue(archive),
            "Int8Property" => Int8Property.ReadValue(archive),
            "Int16Property" => Int16Property.ReadValue(archive),
            "Int64Property" => Int64Property.ReadValue(archive),
            "ByteProperty" => ByteProperty.ReadValue(archive),
            "UInt16Property" => UInt16Property.ReadValue(archive),
            "UInt32Property" => UInt32Property.ReadValue(archive),
            "UInt64Property" => UInt64Property.ReadValue(archive),
            "FloatProperty" => FloatProperty.ReadValue(archive),
            "DoubleProperty" => DoubleProperty.ReadValue(archive),
            "BoolProperty" => BoolProperty.ReadValue(archive),
            "StrProperty" => StrProperty.ReadValue(archive),
            "NameProperty" => NameProperty.ReadValue(archive),
            "ObjectProperty" => ObjectProperty.ReadValue(archive),
            "SoftObjectProperty" => SoftObjectProperty.ReadValue(archive),
            "StructProperty" => StructProperty.ReadStructValue(archive, propertyType),
            // More property types will be added here
            _ => throw new NotSupportedException($"Unsupported collection element type: {propertyType.TypeName.FullName}")
        };
    }

    public static List<Property> ReadList(AsaArchive archive)
    {
        var properties = new List<Property>();

        while (true)
        {
            var property = Property.Read(archive);
            if (property == null)
                break; // "None" terminator

            properties.Add(property);
        }

        return properties;
    }
}
