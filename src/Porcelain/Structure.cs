using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using System.Security.Principal;

namespace AsaSavegameToolkit.Porcelain;

public class Structure
{
    public Guid Id { get; set; }
    public required string ClassName { get; set; }
    public string? StructureName { get; set; }
    public bool IsActivated { get; set; } = false;
    internal double LastActivatedGameTime { get; set; } = 0;
    internal double LastDeactivatedGameTime { get; set; } = 0;

    public DateTime? LastActivatedTimestamp { get; set; }
    public DateTime? LastDeactivatedTimestamp { get; set;  }

    public Inventory? Inventory { get; set; }
    public FVector? Location { get;  set; }
    public FQuat? Rotation { get;  set; }

    public FVector2D? GPSLocation { get; private set; }

    internal static Structure Create(GameObjectRecord r, ActorTransform? transform)
    {
        var className = r.GetClassName();

        var properties = r.Properties;

        var targetingTeam = properties.Get<int>("TargetingTeam");
        if (TeamInfo.IsTamed(targetingTeam))
        {
            return PlayerStructure.Create(r,transform);
        }
                
        var structureId = properties.Get<uint>("StructureID");
        var containerActivated = properties.Get<bool>("bContainerActivated");
        var lastActivatedTime = properties.Get<double>("LastActivatedTime");
        var lastDeactivatedTime = properties.Get<double>("LastDeactivatedTime");
        var displayName = properties.Get<string>("BoxName") ?? "";

        return new Structure
        {
            Id = r.Uuid,
            ClassName = className,
            StructureName = displayName,
            IsActivated = containerActivated,
            LastActivatedGameTime = lastActivatedTime,
            LastDeactivatedGameTime = lastDeactivatedTime,
            Location = transform?.Location,
            Rotation = transform?.Rotation
        };
    }

    public virtual void IngestInventory(Inventory inventory)
    {
        Inventory = inventory;
    }

    public override string ToString()
    {
        var name = StructureName ?? ClassName;
        return $"{name}";
    }

    internal virtual void RefreshTimestamps(DateTime saveTimestamp, double gameTime)
    {
        if(LastActivatedGameTime > 0)
        {
            LastActivatedTimestamp = saveTimestamp.AddSeconds(LastActivatedGameTime - gameTime);
        }

        if(LastDeactivatedGameTime > 0)
        {
            LastDeactivatedTimestamp = saveTimestamp.AddSeconds(LastDeactivatedGameTime - gameTime);
        }
    }

    internal void UpdateGPSLocation(MapDefinition? map)
    {
        if (map == null) return;
        if (Location == null) return; //no location to calcaulte gps co-ords

        GPSLocation = new FVector2D(map.GetLongitude(Location.Value.X), map.GetLatitude(Location.Value.Y));
    }
}
