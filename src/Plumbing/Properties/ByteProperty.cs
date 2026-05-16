using AsaSavegameToolkit.Plumbing.Primitives;
using System;
using System.Drawing;
using System.Xml.Linq;

namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// 8-bit unsigned integer property, can be either numeric or enum.
/// Check tag.Type.Parameters to determine if it's an enum (ByteProperty(EnumType)) or simple byte.
/// </summary>
public class ByteProperty : Property<byte>
{
    public static Property Read(Readers.AsaArchive archive, PropertyTag tag)
    {
       

        if (tag.Type.Parameters.Count > 0)
        {
            // Enum: read as FName
            return new ByteEnumProperty
            {
                Tag = tag,
                Value = archive.ReadFName()
            };
        }
        else
        {
            return new ByteProperty
            {
                Tag = tag,
                Value = archive.ReadByte()
            };
        }
    }
        
    /// <summary>
    /// Reads just the value without a tag (for array elements, etc.).
    /// Assumes numeric byte since we don't have tag context.
    /// </summary>
    public static byte ReadValue(Readers.AsaArchive archive)
    {
        return archive.ReadByte();
    }
}

public class ByteEnumProperty : Property<FName>
{
    public static ByteEnumProperty Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new ByteEnumProperty
        {
            Tag = tag,
            Value = archive.ReadFName()
        };
    }
}
