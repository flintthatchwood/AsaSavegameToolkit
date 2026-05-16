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
        if (archive.IsArkFile)
        {
            return ReadFile(archive);
        }
        
        if(archive.IsCryopod)
        {
            return ReadCryopod(archive);
        }

        var referenceType = archive.ReadInt16();

        if (referenceType == 0)
        {
            // GUID reference (most common)
            return new ObjectReference
            {
                IsPath = false,
                ObjectId = archive.ReadGuid()
            };
        }


        var pos = archive.Position;
        try
        {
            // Path reference (FName)

            var pathName = archive.ReadFName();
            return new ObjectReference
            {
                IsPath = true,
                Path = pathName
            };
        }
        catch
        {
            archive.Position = pos;
            var p1 = archive.ReadInt64();
                
            return new ObjectReference()
            {
                IsPath = false,
                ObjectId = Guid.Empty
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
                Path = new FName(0,0, archive.ReadString())
            };
        }
        else
        {
            archive.Position -= 4; // Rewind the int32 we just read, as the GUID is stored in the same 4 bytes + 12 more bytes
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
            var valStart = archive.Position;
            var allow = archive.AllowDynamicNameTable;
            archive.AllowDynamicNameTable = false;
            try
            {

                var pathValue = archive.ReadFName();

                // Path reference (FName)
                return new ObjectReference
                {
                    IsPath = true,
                    Path = pathValue
                };
            }
            catch
            {
                archive.Position = valStart;
                archive.AllowDynamicNameTable = allow;                
                var pathValue = archive.ReadString();

                // Path reference (FName)
                return new ObjectReference
                {
                    IsPath = true,
                    Path = new FName(0,0,pathValue)
                };

            }
            archive.AllowDynamicNameTable = allow;
        }
        else
        {
            var endOfData = archive.ReadInt32();

            return new ObjectReference
            {
                IsPath = false,
                ObjectId = Guid.Empty
            };

        }
    }

    public override string ToString() => Value;
}
