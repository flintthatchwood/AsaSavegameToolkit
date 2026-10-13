using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

namespace AsaSavegameToolkit.Porcelain;

public class Structure
{
    public Guid Id { get; set; }
    public required string ClassName { get; set; }
    public string? StructureName { get; set; }
    public string? TribeName { get; set; }
    public bool IsPlayerBuilt { get; set; }
    public bool IsActivated { get; set; }
    internal double LastActivatedGameTime { get; set; }
    internal double LastDeactivatedGameTime { get; set; }
    public DateTime? LastActivatedTimestamp { get; set; }
    public DateTime? LastDeactivatedTimestamp { get; set; }
    public Inventory? Inventory { get; set; }
    public FVector? Location { get; set; }
    public FQuat? Rotation { get; set; }
    public GameObjectRecord? Record { get; set; }

    internal static Structure Create(GameObjectRecord r, ActorTransform? transform)
    {
        var targetingTeam = r.Properties.Get<int>("TargetingTeam");
        if (TeamInfo.IsTamed(targetingTeam))
        {
            return PlayerStructure.Create(r, transform);
        }

        var structure = new Structure { ClassName = r.GetClassName() };
        structure.PopulateFromRecord(r, transform);
        return structure;
    }

    protected void PopulateFromRecord(GameObjectRecord record, ActorTransform? transform)
    {
        var properties = record.Properties;

        Record = record;
        Id = record.Uuid;
        ClassName = record.GetClassName();
        StructureName = properties.Get<string>("BoxName") ?? properties.Get<string>("StructureName");
        TribeName = properties.Get<string>("OwnerName") ?? properties.Get<string>("TribeName");
        IsPlayerBuilt = properties.Get<bool>("bHasResetDecayTime") ||
                        GameObjectExtensions.NonCreatureDisambiguatedClasses.Contains(record.GetShortClassName());
        IsActivated = properties.Get<bool>("bContainerActivated");
        LastActivatedGameTime = properties.Get<double>("LastActivatedTime");
        LastDeactivatedGameTime = properties.Get<double>("LastDeactivatedTime");
        Location = transform?.Location;
        Rotation = transform?.Rotation;
    }

    public virtual void IngestInventory(Inventory inventory)
    {
        Inventory = inventory;
    }

    internal virtual void RefreshTimestamps(DateTime saveTimestamp, double gameTime)
    {
        if (LastActivatedGameTime > 0)
        {
            LastActivatedTimestamp = saveTimestamp.AddSeconds(LastActivatedGameTime - gameTime);
        }

        if (LastDeactivatedGameTime > 0)
        {
            LastDeactivatedTimestamp = saveTimestamp.AddSeconds(LastDeactivatedGameTime - gameTime);
        }
    }

    public override string ToString()
    {
        var name = StructureName ?? ClassName;
        var tribe = TribeName != null ? $" [{TribeName}]" : "";
        return $"{name}{tribe}";
    }
}
