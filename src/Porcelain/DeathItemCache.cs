using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

using Microsoft.Extensions.Logging;

namespace AsaSavegameToolkit.Porcelain;

public class DeathItemCache
{
    public Guid Id { get; set; }
    public required string ClassName { get; set; }
    public string? OwnerName { get; set; }
    public int? TargetingTeam { get; set; }
    public int? OwningPlayerId { get; set; }
    public FVector? Location { get; set; }
    public FQuat? Rotation { get; set; }
    public Inventory? Inventory { get; set; }
    public GameObjectRecord? Record { get; set; }
    public DateTime? CreationTimestamp { get; private set; }
    public DateTime? ExpirationTimestamp { get; private set; }

    internal double? CreationGameTime { get; set; }
    internal double? DestructionGameTime { get; set; }

    internal static DeathItemCache Create(GameObjectRecord record, ActorTransform? transform,
        IDictionary<Guid, Item> inventoryItems, ILogger logger)
    {
        var properties = record.Properties;
        var cache = new DeathItemCache
        {
            Id = record.Uuid,
            ClassName = record.GetClassName(),
            Record = record,
            OwnerName = properties.Get<string>("OwnerName"),
            TargetingTeam = properties.TryGet<int>("TargetingTeam", out var team) ? team : null,
            OwningPlayerId = properties.TryGet<int>("OwningPlayerID", out var player) ? player : null,
            Location = transform?.Location,
            Rotation = transform?.Rotation,
            CreationGameTime = properties.TryGet<double>("OriginalCreationTime", out var creation) ? creation : null,
            DestructionGameTime = properties.TryGet<double>("NetDestructionTime", out var destruction) ? destruction : null
        };

        var inventoryRecord = record.GetInventoryComponent();
        if (inventoryRecord != null)
        {
            cache.Inventory = Inventory.Create(inventoryRecord, inventoryItems, logger);
        }
        else if (properties.TryGet<ObjectReference>("MyInventoryComponent", out var reference)
            && (reference.IsPath || reference.ObjectId != Guid.Empty))
        {
            logger.LogWarning("Death cache {CacheId} references an inventory component that could not be resolved.", cache.Id);
        }

        return cache;
    }

    internal void RefreshTimestamps(DateTime saveTimestamp, double gameTime)
    {
        CreationTimestamp = CreationGameTime is > 0
            ? saveTimestamp.AddSeconds(CreationGameTime.Value - gameTime)
            : null;
        ExpirationTimestamp = DestructionGameTime is > 0
            ? saveTimestamp.AddSeconds(DestructionGameTime.Value - gameTime)
            : null;
    }
}
