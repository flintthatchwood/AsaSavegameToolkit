using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

namespace AsaSavegameToolkit.Porcelain;

public class PlayerStructure : Structure
{
    public long TribeId { get; set; }
    public long OwningPlayerId { get; set; }
    public long PlacerId { get; set; }
    public string PlacedTime { get; set; } = string.Empty;

    internal double CreationGameTime { get; set; }
    internal double LastAllyInRangeGameTime { get; set; }

    public DateTime? CreationTimestamp { get; set; }
    public DateTime? LastAllyInRangeTimestamp { get; set; }

    public bool HasFuel { get; set; }
    public bool LastToggleActivated { get; set; }
    public bool IsPinLocked { get; set; }
    public bool IsLocked { get; set; }
    public bool IsWatered { get; set; }
    public bool IsPowered { get; set; }
    public bool IsSeeded { get; set; }
    public bool IsFertilized { get; set; }

    public static new PlayerStructure Create(GameObjectRecord record, ActorTransform? transform)
    {
        var properties = record.Properties;
        var structure = new PlayerStructure
        {
            ClassName = record.GetClassName()
        };
        structure.PopulateFromRecord(record, transform);

        structure.TribeId = properties.Get<int>("TargetingTeam");
        structure.OwningPlayerId = properties.Get<int>("OwningPlayerID");
        structure.PlacerId = properties.Get<int>("OriginalPlacerID");
        structure.PlacedTime = properties.Get<string>("OriginalPlacedTimeStamp") ?? "";
        structure.CreationGameTime = properties.Get<double>("OriginalCreationTime");
        structure.LastAllyInRangeGameTime = properties.Get<double>("LastAllyInRangeTimeSerialized");
        structure.HasFuel = properties.Get<bool>("bHasFuel");
        structure.LastToggleActivated = properties.Get<bool>("bLastToggleActivated");
        structure.IsPinLocked = properties.Get<bool>("bIsPinLocked");
        structure.IsLocked = properties.Get<bool>("bIsLocked");
        structure.IsWatered = properties.Get<bool>("bIsWatered");
        structure.IsPowered = properties.Get<bool>("bIsPinPowered");
        structure.IsSeeded = properties.Get<bool>("bIsSeeded");
        structure.IsFertilized = properties.Get<bool>("bIsFertilized");
        return structure;
    }

    internal override void RefreshTimestamps(DateTime saveTimestamp, double gameTime)
    {
        base.RefreshTimestamps(saveTimestamp, gameTime);

        if (CreationGameTime > 0)
        {
            CreationTimestamp = saveTimestamp.AddSeconds(CreationGameTime - gameTime);
        }

        if (LastAllyInRangeGameTime > 0)
        {
            LastAllyInRangeTimestamp = saveTimestamp.AddSeconds(LastAllyInRangeGameTime - gameTime);
        }
    }
}
