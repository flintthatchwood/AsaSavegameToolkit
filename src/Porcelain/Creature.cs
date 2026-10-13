
using System.Collections;
using System.ComponentModel;
using System.Numerics;
using System.Transactions;
using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

namespace AsaSavegameToolkit.Porcelain;

/// <summary>
/// Represents a creature (dinosaur or other animal) in an ARK save file.
/// Wraps a <see cref="GameObjectRecord"/> and exposes typed accessors for common creature properties.
/// </summary>
public class Creature
{
    /// <summary>
    /// Unique object ID of this creature.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The blueprint class name (e.g., "Yutyrannus_Character_BP_C").
    /// </summary>
    public required string ClassName { get; set; }

    /// <summary>
    /// Unique dino identifier. 
    /// forms the globally-unique dino ID used in-game.
    /// </summary>
    public long? DinoId { get; set; }

    public long TribeId { get; set; } = 0;
    /// <summary>
    /// True if this creature is female.
    /// </summary>
    public bool IsFemale { get; set; }

    /// <summary>
    /// The base character level (wild levels allocated). Null if not set.
    /// Note: tamed bonus levels are separate - see <see cref="ExtraLevel"/>.
    /// </summary>
    public int? BaseLevel { get; set; }

    /// <summary>
    /// Total displayed level (BaseLevel + ExtraLevel).
    /// </summary>
    public int? TotalLevel { get; set; }

    /// <summary>
    /// Number of mutations from the male lineage.
    /// </summary>
    public int MutationsMale { get; set; }

    /// <summary>
    /// Number of mutations from the female lineage.
    /// </summary>
    public int MutationsFemale { get; set; }

    /// <summary>
    /// Total mutation count (male + female lineage).
    /// </summary>
    public int TotalMutations { get; set; }

    /// <summary>
    /// Baby/juvenile age from 0.0 (newborn) to 1.0 (fully grown).
    /// Null if the creature is not a juvenile.
    /// </summary>
    public float? BabyAge { get; set; }

    /// <summary>
    /// True if this creature is still in the juvenile growth phase.
    /// </summary>
    public bool IsJuvenile { get; set; }

    public string[]? Traits { get; set; } = [];


    /// <summary>
    /// Color region indices (up to 6 regions, indexed 0-5).
    /// Each value is a color ID (0 = default/no color).
    /// Corresponds to ArrayProperty(ByteProperty) "ColorSetIndices".
    /// </summary>
    public byte[] ColorRegions { get; set; } = new byte[6] { 0, 0, 0, 0, 0, 0 };

    public byte[] WildStats { get; set; } = new byte[12] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    public float?[] StatsCurrent { get; set; } = [];
    public string[]? ProductionResources { get; set; }

    public FVector? Location { get;  set; }
    public FVector2D? GPSLocation { get; set; }

    public FQuat? Rotation { get; set; }
    public Inventory? Inventory { get; internal set; }

    public bool IsCryo { get; set; } = false;
    public bool IsNeutered { get; set; } = false;
    public float Scale { get; set; } = 1;
    internal double OriginalCreationGameTime { get; set; }
    public DateTime? OriginalCreationTimestamp { get; set; } = null;

    public override string ToString()
    {
        var name = ClassName;
        var tribe = "";
        var level = TotalLevel.HasValue ? $" Lv{TotalLevel}" : "";
        return $"{name}{level}{tribe}";
    }

    /// <summary>
    /// Creates a new Creature instance from a record and transform.
    /// </summary>
    public static Creature Create(GameObjectRecord actor, ActorTransform? transform)
    {
        if (!actor.IsCreature())
            throw new AsaSavegameToolkit.Plumbing.AsaDataException($"Gameobject {actor.Uuid} is cannot be parsed as a Creature.");
      
        var properties = actor.Properties;

        if(properties==null || properties.Count == 0)
        {
            return new CreatureWild
            {
                Id = actor.Uuid,
                ClassName = actor.GetClassName(),
                Location = transform?.Location,
                Rotation = transform?.Rotation
            };
        }

        int targetingTeam = properties.Get<int>("TargetingTeam");
        if(TeamInfo.IsTamed(targetingTeam))
            return CreatureTamed.Create(actor,transform);

        return CreatureWild.Create(actor,transform);
    }

    internal void IngestInventory(Inventory inventory)
    {
        Inventory = inventory;
    }

    internal virtual void IngestStatusRecord(GameObjectRecord statusComponent)
    {
     
    }

    internal virtual void RefreshTimestamps(DateTime saveDate, double gameTime)
    {
        if (OriginalCreationGameTime > 0)
        {
            OriginalCreationTimestamp = saveDate.AddSeconds(OriginalCreationGameTime - gameTime);
        }
    }

    internal void UpdateGPSLocation(MapDefinition? map)
    {
        if(map== null) return;
        if (Location == null) return; //no location to calcaulte gps co-ords

        GPSLocation = new FVector2D(map.GetLongitude(Location.Value.X), map.GetLatitude(Location.Value.Y));
    }
}
