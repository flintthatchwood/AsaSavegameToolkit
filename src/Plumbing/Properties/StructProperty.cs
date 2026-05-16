using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Records;
using System.Drawing;
using System.Reflection.Metadata.Ecma335;

namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// Represents a StructProperty - a compound value containing either:
/// - A list of properties (generic struct)
/// - A typed value (Quat, Vector, etc.)
/// </summary>
public class StructProperty : Property<object>
{
    /// <summary>
    /// Optional GUID for the struct type (v13 only, usually all zeros).
    /// In v14, GUID may be in the type parameters.
    /// </summary>
    public Guid? StructGuid { get; init; }

    public override string ToString() => Tag.ToString();

    /// <summary>
    /// Reads struct value with a tag.
    /// Generic structs: Read property list until "None" terminator.
    /// Typed structs (Quat, Vector, etc.): Read fixed-size binary data.
    /// </summary>
    public static object ReadValue(Readers.AsaArchive archive, PropertyTag tag)
    {
        return ReadStructValue(archive, tag.Type);
    }
    
    /// <summary>
    /// Reads a Quat (quaternion) struct: 4 doubles (X, Y, Z, W).
    /// Total size: 32 bytes.
    /// </summary>
    private static FQuat ReadQuat(Readers.AsaArchive archive)
    {
        var x = archive.ReadDouble();
        var y = archive.ReadDouble();
        var z = archive.ReadDouble();
        var w = archive.ReadDouble();
        return new FQuat(x, y, z, w);
    }
    
    /// <summary>
    /// Reads a Vector struct: 3 doubles (X, Y, Z).
    /// Total size: 24 bytes.
    /// </summary>
    private static FVector ReadVector(Readers.AsaArchive archive)
    {
        var x = archive.ReadDouble();
        var y = archive.ReadDouble();
        var z = archive.ReadDouble();
        return new FVector(x, y, z);
    }
    
    /// <summary>
    /// Reads a Rotator struct: 3 doubles (Pitch, Yaw, Roll).
    /// Total size: 24 bytes.
    /// </summary>
    private static FRotator ReadRotator(Readers.AsaArchive archive)
    {
        var pitch = archive.ReadDouble();
        var yaw = archive.ReadDouble();
        var roll = archive.ReadDouble();
        return new FRotator(pitch, yaw, roll);
    }
    
    /// <summary>
    /// Reads a LinearColor struct: 4 floats (R, G, B, A).
    /// Total size: 16 bytes.
    /// </summary>
    private static FLinearColor ReadLinearColor(Readers.AsaArchive archive)
    {
        var r = archive.ReadFloat();
        var g = archive.ReadFloat();
        var b = archive.ReadFloat();
        var a = archive.ReadFloat();
        return new FLinearColor(r, g, b, a);
    }
    
    /// <summary>
    /// Reads a Color struct: 4 bytes (B, G, R, A).
    /// Note: Unreal stores colors in BGRA order!
    /// Total size: 4 bytes.
    /// </summary>
    private static FColor ReadColor(Readers.AsaArchive archive)
    {
        var b = archive.ReadByte();
        var g = archive.ReadByte();
        var r = archive.ReadByte();
        var a = archive.ReadByte();
        return new FColor(b, g, r, a);
    }
    
    /// <summary>
    /// Reads a Vector2D struct: 2 doubles (X, Y).
    /// Total size: 16 bytes.
    /// </summary>
    private static FVector2D ReadVector2D(Readers.AsaArchive archive)
    {
        var x = archive.ReadDouble();
        var y = archive.ReadDouble();
        return new FVector2D(x, y);
    }

    /// <summary>
    /// Reads a UniqueNetIdRepl struct: 1 byte unknown + FString type + 1 byte length + length bytes ID.
    /// Used to identify players across different online subsystems (NULL, Steam, etc.).
    /// </summary>
    private static FUniqueNetIdRepl ReadUniqueNetIdRepl(Readers.AsaArchive archive)
    {
        archive.ReadByte(); // unknown
        var type = archive.ReadString();
        var length = archive.ReadByte();
        var id = archive.ReadBytes(length);
        return new FUniqueNetIdRepl(type, id);
    }
    
    public static StructProperty Read(Readers.AsaArchive archive, PropertyTag tag)
    {
        Guid? guid = null;
        if (archive.IsArkFile)
        {
            var structValue = ReadValue(archive, tag);
            return new StructProperty
            {
                Tag = tag,
                StructGuid = guid,
                Value = structValue
            };
        }


        guid = ReadStructGuid(archive, tag);
        var value = ReadValue(archive, tag);

        return new StructProperty
        {
            Tag = tag,
            StructGuid = guid,
            Value = value
        };
    }


    /// <summary>
    /// Reads the optional struct GUID (v13 only).
    /// In v13 non-array structs: 16 bytes (usually all zeros).
    /// In v14: No GUID in data.
    /// </summary>
    private static Guid? ReadStructGuid(Readers.AsaArchive archive, PropertyTag tag)
    {
        if (archive.SaveVersion < 14)
        {
            // v13: Always read 16-byte GUID
            return archive.ReadGuid();
        }
        
        // v14: Check if GUID is in the type parameters
        // StructProperty(StructName, Guid) would have 2 parameters
        if (tag.Type.Parameters.Count > 1)
        {
            // Parse GUID from FName string representation
            var guidName = tag.Type.GetParameter(1)?.TypeName.FullName;
            if (!string.IsNullOrEmpty(guidName) && Guid.TryParse(guidName, out var guid))
            {
                return guid;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Reads a generic struct as a list of properties until "None" terminator.
    /// This is the fallback for unknown struct types.
    /// </summary>
    private static List<Property> ReadGenericStruct(Readers.AsaArchive archive)
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
    
    /// <summary>
    /// Reads struct value without a tag (for array elements, map values, etc.).
    /// Assumes generic struct since we need the tag to know the type.
    /// </summary>
    public static object ReadStructValue(Readers.AsaArchive archive, FPropertyTypeName structType)
    {
        var innerType = structType.GetParameter(0);
        // We allow the inner type to be null to support v13 Map<..,StructPropert> where the inner type isn't stored in the data.
        // We just assume that it will be a generic property bag struct and not a known struct 

        return innerType?.TypeName.FullName switch
        {
            // Standard Unreal Engine structs with known binary formats - read fixed-size data
            "Quat" => ReadQuat(archive),
            "Vector" => ReadVector(archive),
            "Rotator" => ReadRotator(archive),
            "LinearColor" => ReadLinearColor(archive),
            "Color" => ReadColor(archive),
            "Vector2D" => ReadVector2D(archive),
            "UniqueNetIdRepl" => ReadUniqueNetIdRepl(archive),
            "IntPoint" => ReadIntPoint(archive),

            // Custom ark structs with their own readers - read using custom logic
            "CustomItemData" => CustomItemDataRecord.Read(archive),

            // Unknown struct types - read as generic property bags
            _ => ReadGenericStruct(archive) // Fallback to generic
        };
    }

    private static FIntPoint ReadIntPoint(AsaArchive archive)
    {
        return new FIntPoint(archive.ReadInt32(), archive.ReadInt32());

    }
}
