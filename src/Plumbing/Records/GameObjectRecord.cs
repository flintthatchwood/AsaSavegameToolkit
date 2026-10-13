using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;

namespace AsaSavegameToolkit.Plumbing.Records;

/// <summary>
/// Represents a game object from the save file.
/// Game objects include dinos, players, structures, items, and all other entities in the game.
/// </summary>
public class GameObjectRecord
{
    /// <summary>
    /// Separator used in <see cref="FullName"/>. Verified absent from all name entries in the save format.
    /// </summary>
    public const char FullNameSeparator = '.';

    public GameObjectRecord(
        Guid uuid,
        FName className,
        IList<string> names,
        List<Property> properties,
        int dataFileIndex,
        ObjectTypeFlags objectType,
        List<Guid>? extraGuids)
    {
        Uuid         = uuid;
        ClassName    = className;
        Names        = names;
        Properties   = properties;
        DataFileIndex = dataFileIndex;
        ObjectType   = objectType;
        ExtraGuids = extraGuids ?? [];
        Components = [];

        Name       = names.FirstOrDefault() ?? string.Empty;
        ParentName = names.Count >= 2 ? names[1] : null;
        FullName   = string.Join(FullNameSeparator, names.AsEnumerable().Reverse());
    }

    /// <summary>
    /// Unique identifier for this game object.
    /// </summary>
    public Guid Uuid { get; }

    /// <summary>
    /// The class/type of this object (e.g., "DinoCharacter_BP", "PlayerPawnTest").
    /// </summary>
    public FName ClassName { get; }

    /// <summary>
    /// Instance name chain for this object.
    /// <list type="bullet">
    /// <item><term>Count == 1 (root object)</term>
    ///   <description><c>Names[0]</c> is the unique instance name — acts as a primary key.</description></item>
    /// <item><term>Count == 2 (child object)</term>
    ///   <description><c>Names[0]</c> is the own name (unique among siblings);
    ///   <c>Names[1]</c> is the parent's <see cref="Name"/> (foreign key), unless it starts with
    ///   <c>"/"</c> in which case it is an asset path.</description></item>
    /// </list>
    /// </summary>
    public IList<string> Names { get; }

    /// <summary>
    /// The own instance name (<c>Names[0]</c>).
    /// Unique among root objects; unique among siblings for child objects.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The raw second name entry (<c>Names[1]</c>), or <c>null</c> for root objects.
    /// When it does not start with <c>"/"</c>, it is the parent object's <see cref="Name"/> (a foreign key).
    /// When it starts with <c>"/"</c>, it is an asset path reference rather than a parent instance name.
    /// </summary>
    public string? ParentName { get; }

    /// <summary>
    /// Globally unique identifier string for this object, formed by joining
    /// <see cref="Names"/> in reverse order with <see cref="FullNameSeparator"/>.
    /// e.g. <c>"DinoCharacter_Rex_C1.DinoCharacter_Rex_C1_42"</c> for a child,
    /// or <c>"DinoCharacter_Rex_C1_42"</c> for a root.
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// Properties containing the object's data.
    /// This is where all the actual game state is stored.
    /// </summary>
    public List<Property> Properties { get; }

    /// <summary>
    /// Index into a separate data file (usually 0 for main save).
    /// </summary>
    public int DataFileIndex { get; }

    /// <summary>
    /// Object type/category flags indicating how the object behaves.
    /// Bit 0 (0x01): Item/inventory object
    /// Bit 1 (0x02): Spawning/NPC management
    /// Bit 3 (0x08): Physical actor
    /// Only 5 combinations appear: None (0x00), Item (0x01), Spawning (0x02), Actor (0x08), SpawnVolume (0x0A)
    /// </summary>
    public ObjectTypeFlags ObjectType { get; }

    /// <summary>
    /// Additional GUIDs that may be associated with this object.
    /// Often just contains the object's own GUID.
    /// </summary>
    public List<Guid> ExtraGuids { get; }

    public Dictionary<string, GameObjectRecord> Components { get; }

    /// <summary>
    /// Reads a game object from the archive (v14+ format).
    /// Automatically delegates to ReadPre14() for older save versions.
    /// </summary>
    /// <param name="archive">The archive to read from.</param>
    /// <param name="uuid">The object's GUID (read from the custom table).</param>
    public static GameObjectRecord Read(Readers.AsaArchive archive, Guid uuid)
    {
        // Delegate to legacy reader for older versions
        if (archive.SaveVersion < 14)
        {
            return ReadPre14(archive, uuid);
        }
        
        // Read class name
        var className = archive.ReadFName();
        
        // Unknown Int32 (always 0)
        archive.ReadInt32();
        
        // Read names
        var names = ReadNames(archive);
        
        // Read data file index
        var dataFileIndex = archive.ReadInt32();

        var pos = archive.Position;

        // Read object type flags
        var objectType = (ObjectTypeFlags)archive.ReadByte();

        List<Property> properties = [];
        List<Guid>? extraGuids = null;

        // some object just stop here without getting into properties
        if (archive.Position != archive.Length)
        {
            // Skip padding byte (v14 format has 1 padding byte after ObjectTypeFlags)
            archive.ReadByte();
        
            // Read properties until "None" terminator
            properties = ReadProperties(archive);

            // Read extra GUIDs
            extraGuids = ReadExtraGuids(archive);
        }

        return new GameObjectRecord(uuid, className, names, properties, dataFileIndex, objectType, extraGuids);
    }

    /// <summary>
    /// Reads a game object for save versions 11-13 (legacy format).
    /// </summary>
    private static GameObjectRecord ReadPre14(Readers.AsaArchive archive, Guid uuid)
    {
        // Read class name
        var className = archive.ReadFName();
        
        // Unknown Int32 (always 0)
        archive.ReadInt32();
        
        // Read names
        var names = ReadNames(archive);
        
        // Read data file index
        var dataFileIndex = archive.ReadInt32();
        
        // Read object type flags
        var objectType = (ObjectTypeFlags)archive.ReadByte();

        List<Property> properties = [];
        List<Guid>? extraGuids = null;

        // some object just stop here without getting into properties
        if (archive.Position != archive.Length)
        {
            // Read properties until "None" terminator
            properties = ReadProperties(archive);

            // Read extra GUIDs
            extraGuids = ReadExtraGuids(archive);
        }
        
        return new GameObjectRecord(uuid, className, names, properties, dataFileIndex, objectType, extraGuids);
    }

    /// <summary>
    /// Reads the list of names for this object.
    /// Format: Int32 (count) + FString[]
    /// Note: These are FStrings, not FNames!
    /// </summary>
    private static List<string> ReadNames(Readers.AsaArchive archive)
    {
        var count = archive.ReadInt32();

        // The minimum size for a FPropertyTypeName is 4 bytes (charCount Int32 == 0) 
        if (count * 4 > archive.RemainingLength)
        {
            throw new AsaDataException($"Invalid string count read at offset {archive.Position - 4} of {archive.FileName}");
        }

        var names = new List<string>(count);        
        for (int i = 0; i < count; i++)
        {
            names.Add(archive.ReadString());
        }
        
        return names;
    }
    
    /// <summary>
    /// Reads properties until "None" terminator.
    /// </summary>
    private static List<Property> ReadProperties(Readers.AsaArchive archive)
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
    /// Reads the extra GUIDs at the end of the object.
    /// Format: Int32 (count) + Guid[]
    /// </summary>
    private static List<Guid>? ReadExtraGuids(Readers.AsaArchive archive)
    {
        var count = archive.ReadInt32();
        if (count == 0)
            return null;

        // The minimum size for a guid is 16 bytes (charCount Int32 == 0) 
        if (count * 16 > archive.RemainingLength)
        {
            throw new AsaDataException($"Invalid guid count read at offset {archive.Position - 4} of {archive.FileName}");
        }
        
        var guids = new List<Guid>(count);
        for (int i = 0; i < count; i++)
        {
            guids.Add(archive.ReadGuid());
        }
        
        return guids;
    }
    
    public override string ToString()
    {
        var name = Names.Count > 0 ? Names[0] : ClassName.FullName;
        return $"GameObject({name}, {Properties.Count} properties)";
    }

}
