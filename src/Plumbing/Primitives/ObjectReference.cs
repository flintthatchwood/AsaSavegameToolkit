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
    /// For a non-path reference inside a cryopod payload: the referenced object's index within the payload,
    /// or -1 for none. Cryopods reference objects this way instead of by GUID.
    /// </summary>
    public int ObjectIndex { get; init; } = -1;

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
        if (archive.IsArkFile)
        {
            return ReadFile(archive);
        }

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

    private static ObjectReference ReadFile(AsaArchive archive)
    {
        var referenceType = archive.ReadInt32();

        if (referenceType == 1)
        {
            // Path reference (FName)
            return new ObjectReference
            {
                IsPath = true,
                Path = new FName(0, 0, archive.ReadString())
            };
        }
        else
        {
            var objectIndex = archive.ReadInt32();
            return new ObjectReference
            {
                IsPath = false,
                ObjectIndex = objectIndex,
                ObjectId = Guid.Empty
            };
        }
    }

    private static ObjectReference ReadCryopod(AsaArchive archive)
    {
        var referenceType = archive.ReadInt32();

        if (referenceType == 1)
        {
            // Path reference. Cryopod paths are always inline strings, even when the payload has a name table
            // (e.g. item classes in a cryopodded Gacha's ResourceProduction).
            return new ObjectReference
            {
                IsPath = true,
                Path = new FName(int.MinValue, 0, archive.ReadString())
            };
        }

        // Otherwise 0 then the object's index in the payload (-1 for none), or a lone -1 for no reference. Same
        // layout as CryopodObjectProperty; there are no GUID references in cryopods (e.g. PlacedIceBridges and
        // BoardedCharacters arrays use 8 bytes per element).
        return referenceType switch
        {
            0 => new ObjectReference { IsPath = false, ObjectIndex = archive.ReadInt32() },
            -1 => new ObjectReference { IsPath = false },
            _ => throw new AsaDataException($"Unknown cryopod object reference type {referenceType} at {archive.Position - 4} in {archive.FileName}"),
        };
    }

    public override string ToString() => Value;
}
