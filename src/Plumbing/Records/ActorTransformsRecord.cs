using AsaSavegameToolkit.Plumbing.Primitives;

namespace AsaSavegameToolkit.Plumbing.Records;

/// <summary>
/// Represents actor transform data from the custom table.
/// Contains position and rotation information for actors in the game world.
/// </summary>
public class ActorTransformsRecord
{
    /// <summary>
    /// Dictionary mapping actor UUIDs to their transform data.
    /// </summary>
    public required Dictionary<Guid, ActorTransform> Transforms { get; init; }
    
    /// <summary>
    /// Reads actor transforms from the archive.
    /// </summary>
    /// <param name="archive">The archive to read from.</param>
    public static ActorTransformsRecord Read(Readers.AsaArchive archive)
    {
        var transforms = new Dictionary<Guid, ActorTransform>();
        var terminationUuid = Guid.Empty;
        
        // Read UUID
        var uuid = archive.ReadGuid();
        
        // Keep reading UUID + transform pairs until we hit the termination UUID (all zeros)
        while (uuid != terminationUuid)
        {
            var transform = ActorTransform.Read(archive);
            transforms.Add(uuid, transform);
            
            // Read next UUID
            uuid = archive.ReadGuid();
        }
        
        return new ActorTransformsRecord
        {
            Transforms = transforms
        };
    }
}

/// <summary>
/// World-space position and rotation for a single actor.
/// Each entry is 56 bytes: FVector (3 doubles) + FQuat (4 doubles).
/// </summary>
public readonly struct ActorTransform
{
    /// <summary>World-space position (X, Y, Z in Unreal units).</summary>
    public FVector Location { get; }

    /// <summary>World-space rotation as a quaternion.</summary>
    public FQuat Rotation { get; }

    public ActorTransform(FVector location, FQuat rotation)
    {
        Location = location;
        Rotation = rotation;
    }

    internal static ActorTransform Read(Readers.AsaArchive archive)
    {
        var x = archive.ReadDouble();
        var y = archive.ReadDouble();
        var z = archive.ReadDouble();

        var qx = archive.ReadDouble();
        var qy = archive.ReadDouble();
        var qz = archive.ReadDouble();
        var qw = archive.ReadDouble();

        return new ActorTransform(new FVector(x, y, z), new FQuat(qx, qy, qz, qw));
    }

    public override string ToString() => $"{Location} {Rotation}";
}