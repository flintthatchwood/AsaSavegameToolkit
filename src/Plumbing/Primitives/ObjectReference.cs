using AsaSavegameToolkit.Plumbing.Readers;

namespace AsaSavegameToolkit.Plumbing.Primitives;

/// <summary>
/// Reference to a game object (used by ObjectProperty and SoftObjectProperty).
/// Can be either a GUID (most common) or an FName path.
/// </summary>
public class ObjectReference
{
    /// <summary>
    /// True if this is a path reference (FName), false if it's a GUID reference.
    /// </summary>
    public bool IsPath { get; init; }

    /// <summary>
    /// The path name (only set if IsPath is true).
    /// </summary>
    public FName Path { get; init; } = FName.None;

    /// <summary>
    /// The object GUID (only set if IsPath is false).
    /// </summary>
    public Guid ObjectId { get; init; } = Guid.Empty;
    
    /// <summary>
    /// String representation of the reference (either the path name or GUID).
    /// </summary>
    public string Value => IsPath ? Path.FullName : ObjectId.ToString();
    
    /// <summary>
    /// Reads an ObjectReference from the archive.
    /// Format for v13+ (with name table):
    /// - Int16 (1 = path/FName, 0 = GUID)
    /// - If 1: FName (via name table)
    /// - If 0: Guid (16 bytes)
    /// </summary>
    public static ObjectReference Read(Readers.AsaArchive archive)
    {
        if(archive.IsCryopod)
        {
            return ReadCryopod(archive);
        }

        var referenceType = archive.ReadInt16();
        
        if (referenceType == 1)
        {
            // Path reference (FName)
            return new ObjectReference
            {
                IsPath = true,
                Path = archive.ReadFName()
            };
        }
        else
        {
            // GUID reference (most common)
            return new ObjectReference
            {
                IsPath = false,
                ObjectId = archive.ReadGuid()
            };
        }
    }

    private static ObjectReference ReadCryopod(AsaArchive archive)
    {
        var referenceType = archive.ReadInt32();

        if (referenceType == 1)
        {
            // Path reference (FName)
            return new ObjectReference
            {
                IsPath = true,
                Path = archive.ReadFName()
            };
        }
        else
        {
            // GUID reference (most common)
            return new ObjectReference
            {
                IsPath = false,
                ObjectId = archive.ReadGuid()
            };
        }
    }

    public override string ToString() => Value;
}
