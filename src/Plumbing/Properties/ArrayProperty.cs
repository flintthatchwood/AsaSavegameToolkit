using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Readers;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;

namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// Array property - ordered collection of elements of a single type.
/// Format: ArrayProperty(ElementType)
/// Access element type via tag.Type.Parameters[0].
/// </summary>
public class ArrayProperty : Property<IList>
{
    public override string ToString() => $"{Tag}: {Value.Count} elements";

    public static ArrayProperty Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        return new ArrayProperty
        {
            Tag = tag,
            Value = ReadValue(archive, tag)
        };
    }
    
    /// <summary>
    /// Reads array elements in v14+ format.
    /// Format: Int32 (count) + elements
    /// </summary>
    public static IList ReadValue(Readers.AsaArchive archive, PropertyTag tag)
    {
        if(archive.SaveVersion < 14 || archive.IsArkFile)
        {
            return ReadValuePre14(archive, tag);
        }

        // Get element type from tag
        var elementType = tag.Type.GetParameter(0);
        if (elementType == null)
            throw new InvalidOperationException($"ArrayProperty must have an ElementType parameter");
        
        // Read element count
        var count = archive.ReadInt32();

        // The minimum size for any other element is 1 byte, so we can do a quick sanity check to prevent OOM or excessive memory allocation
        if (count > archive.RemainingLength)
        {
            throw new AsaDataException($"Invalid array count read at offset {archive.Position - 4} of {archive.FileName}");
        }

        // Avoid boxing byte arrays - if element type is byte, read as byte array directly
        if (elementType.TypeName.FullName == "ByteProperty" )
        {
            if (elementType.ParameterCount == 0)
            {
                var byteArray = archive.ReadBytes(count);
                return byteArray;
            }

            //enum array
            var enumElements = new object[count];
            for (int i = 0; i < count; i++)
            {
                enumElements[i] = archive.ReadFName();
            }

            return enumElements;
        }

        var elements = new object[count];

        // Read each element based on element type
        for (int i = 0; i < count; i++)
        {
            elements[i] = ReadValue(archive, elementType);
        }
        
        return elements;
    }
    
    /// <summary>
    /// Reads array elements in v13 format.
    /// Format: Int32 (count) + elements
    /// Special case: StructProperty arrays have metadata header (PropertyTag + GUID) before elements.
    /// </summary>
    private static IList ReadValuePre14(Readers.AsaArchive archive, PropertyTag tag)
    {
        // Get element type from tag
        var elementType = tag.Type.GetParameter(0);
        if (elementType == null)
            throw new InvalidOperationException($"ArrayProperty must have an ElementType parameter");
        

        // StructProperty arrays have special format in v13
        if (elementType.TypeName.FullName == "StructProperty")
        {
            if (archive.IsArkFile)
                return ReadStructArrayFile(archive, tag);

            var structArray = ReadStructArrayPre14(archive, out var structType);
            // Fix up the tag to record the resolved struct type.
            // We must NOT mutate the interned elementType instance — create new interned
            // instances and replace tag.Type so all callers see the updated type tree.
            var fixedElementType = FPropertyTypeName.Create(elementType.TypeName, [structType]);
            tag.Type = FPropertyTypeName.Create(tag.Type.TypeName, [fixedElementType]);
            return structArray;
        }

        // Standard array reading for non-struct types
        var count = archive.ReadInt32();

        // The minimum size for any other element is 1 byte, so we can do a quick sanity check to prevent OOM or excessive memory allocation
        if (count > archive.RemainingLength)
        {
            throw new AsaDataException($"Invalid array count read at offset {archive.Position - 4} of {archive.FileName}");
        }

        // Avoid boxing byte arrays - if element type is byte, read as byte array directly
        if (elementType.TypeName.FullName == "ByteProperty")
        {
            var byteArray = archive.ReadBytes(count);
            return byteArray;
        }

        var elements = new object[count];

        // Read each element based on element type

        for (int i = 0; i < count; i++)
        {
            if (archive.IsArkFile || archive.IsCryopod)
            {
                switch (elementType.TypeName.Name)
                {
                    case "ObjectProperty":
                        var dataTypeSize = (tag.Size - 4) / count; // rough size of each element based on total size in tag (minus 4 bytes for count)

                        if (dataTypeSize > 8)
                        {
                            elements[i] = ReadValue(archive, elementType);
                        }
                        else
                        {
                            elements[i] = archive.ReadInt64();
                        }

                        break;
                    default:
                        elements[i] = ReadValue(archive, elementType);
                        break;
                }
            }
            else
            {
                elements[i] = ReadValue(archive, elementType);
            }



        }
        
        return elements;
    }
    
    /// <summary>
    /// Reads v13 StructProperty array with metadata header.
    /// Format: Count + PropertyTag (struct metadata) + GUID + Elements
    /// The PropertyTag contains struct type info that applies to all elements.
    /// </summary>
    private static IList ReadStructArrayPre14(Readers.AsaArchive archive, out FPropertyTypeName structType)
    {



        // Read element count first
        var count = archive.ReadInt32();

        // quick sanity check, the count can't exceed the remaining bytes in the archive
        if (count > archive.Length - archive.Position)
        {
            throw new AsaDataException($"Invalid array count read at offset {archive.Position - 4} of {archive.FileName}");
        }

        var elements = new object[count];
        
        // Read struct array metadata (acts like first element's property tag)
        var structTag = PropertyTag.Read(archive);
        if (structTag == null)
            throw new InvalidOperationException("Expected struct metadata for v13 array");
        
        // Read struct GUID (16 bytes, usually zeros)
        var guid = archive.ReadGuid();
        
        // Read each struct element using the shared metadata
        for (int i = 0; i < count; i++)
        {
            // Each element is a struct value with the type from structTag
            elements[i] = StructProperty.ReadValue(archive, structTag);
        }

        structType = structTag.Type.GetParameter(0) ?? throw new InvalidOperationException("StructProperty tag must have ElementType parameter");

        return elements;
    }

    private static IList ReadStructArrayFile(AsaArchive archive, PropertyTag tag)
    {

        if(archive.SaveVersion < 7)
        {
            return ReadStructArrayFilePre7(archive, tag);
        }


        var checkInt1 = archive.ReadInt32(); // unknown
        var structKey = archive.ReadString();
        var checkInt2 = archive.ReadInt32(); // unknown
        
        var structClassPath = archive.ReadString(); // structType / path
        _ = archive.ReadBytes(8); // struct id / padding
        _ = archive.ReadByte(); // separator

        FPropertyTypeName arrayTypeStruct = FPropertyTypeName.Create(tag.Type.Parameters[0].TypeName, new FName(0, 0, structKey));


        var count = archive.ReadInt32();

        var elements = new object[count];
        for(int i = 0; i < count; i++)
        {
            var structTag = new PropertyTag
            {
                Name = new FName(0,0, structKey),
                Type = arrayTypeStruct,
                ArrayIndex = i,
                Size = 1
            };
            var propValue = StructProperty.ReadValue(archive, structTag);
            elements[i] = propValue;

        }

        return elements;

    }

    private static IList ReadStructArrayFilePre7(AsaArchive archive, PropertyTag tag)
    {
        var count = archive.ReadInt32();
        var name = archive.ReadFName();
        var type = archive.ReadFName();

        _ = archive.ReadBytes(8);

        var innerKey = archive.ReadFName();

        _ = archive.ReadBytes(17);

        FPropertyTypeName arrayTypeStruct = FPropertyTypeName.Create(tag.Type.Parameters[0].TypeName, innerKey);

        var structTag = new PropertyTag
        {
            Name = name,
            Type = arrayTypeStruct,
            ArrayIndex = 0,
            Size = 1
        };

        var elements = new object[count];
        for (int i = 0; i < count; i++)
        {
            elements[i] = StructProperty.ReadValue(archive, structTag);
        }

        return elements;
    }
}
