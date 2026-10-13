
using System.Collections;

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
    /// The underlying raw record for direct property access.
    /// </summary>
    public required GameObjectRecord Record { get; set; }

    /// <summary>
    /// Unique object ID of this creature.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The blueprint class name (e.g., "Yutyrannus_Character_BP_C").
    /// </summary>
    public required string ClassName { get; set; }

    /// <summary>
    /// The player-given tame name (e.g., "Fluffy"). Null if not tamed or unnamed.
    /// </summary>
    public string? TamedName { get; set; }

    /// <summary>
    /// The name of the tribe that owns this creature. Null if untamed.
    /// </summary>
    public string? TribeName { get; set; }

    /// <summary>
    /// The player who last tamed this creature (player identifier string).
    /// </summary>
    public string? TamerString { get; set; }

    /// <summary>
    /// True if this creature is tamed.
    /// </summary>
    public bool IsTamed { get; set; }

    /// <summary>
    /// True if this creature is stored in a cryopod.
    /// </summary>
    public bool IsInCryo { get; set; }

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
    /// Tamed-bonus levels added on top of wild level.
    /// </summary>
    public int ExtraLevel { get; set; }

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
    /// Imprint quality from 0.0 (no imprint) to 1.0 (100% imprint).
    /// </summary>
    public float ImprintQuality { get; set; }

    /// <summary>
    /// The player name who imprinted this creature.
    /// </summary>
    public string? ImprinterName { get; set; }

    /// <summary>
    /// Baby/juvenile age from 0.0 (newborn) to 1.0 (fully grown).
    /// Null if the creature is not a juvenile.
    /// </summary>
    public float? BabyAge { get; set; }

    /// <summary>
    /// True if this creature is still in the juvenile growth phase.
    /// </summary>
    public bool IsJuvenile { get; set; }

    /// <summary>
    /// Server time (seconds since epoch) when this creature was tamed. Null if not tamed.
    /// </summary>
    public double? TamedAtTime { get; set; }

    /// <summary>
    /// Part 1 of the unique dino identifier pair. Together with <see cref="DinoId2"/> 
    /// forms the globally-unique dino ID used by external tools.
    /// </summary>
    public uint? DinoId1 { get; set; }

    /// <summary>
    /// Part 2 of the unique dino identifier pair.
    /// </summary>
    public uint? DinoId2 { get; set; }

    /// <summary>
    /// Packed 64-bit dino identifier combining <see cref="DinoId1"/> and <see cref="DinoId2"/>.
    /// </summary>
    public long? DinoId => DinoId1.HasValue && DinoId2.HasValue ? ((long)DinoId1.Value << 32) | DinoId2.Value : null;

    /// <summary>
    /// Color region indices (up to 6 regions, indexed 0-5).
    /// Each value is a color ID (0 = default/no color).
    /// Corresponds to ArrayProperty(ByteProperty) "ColorSetIndices".
    /// </summary>
    public byte?[] ColorRegions { get; set; } = [];

    public byte?[] WildLevels { get; set; } = [];

    public byte?[] TamedLevelsApplied { get; set; } = [];

    public byte?[] MutationLevelsApplied { get; set; } = [];

    public float?[] StatsCurrent { get; set; } = [];

    public List<string> GeneTraits { get; set; } = [];

    public double? OriginalCreationGameTime { get; set; }

    public DateTime? OriginalCreationTimestamp { get; set; }

    public FVector? Location { get; private set; }

    public FQuat? Rotation { get; private set; }

    public Inventory? Inventory { get; internal set; }

    public override string ToString()
    {
        var name = TamedName ?? ClassName;
        var tribe = TribeName != null ? $" [{TribeName}]" : "";
        var level = TotalLevel.HasValue ? $" Lv{TotalLevel}" : "";
        return $"{name}{level}{tribe}";
    }

    /// <summary>
    /// Creates a new Creature instance from a record and transform.
    /// </summary>
    public static Creature Create(GameObjectRecord record, ActorTransform? transform)
    {
        var properties = record.Properties;

        var mutationsMale = properties.Get<int>("RandomMutationsMale");
        var mutationsFemale = properties.Get<int>("RandomMutationsFemale");
        var baseLevel = record.GetBaseLevel();
        var extraCharacterLevel = properties.Get<int>("ExtraCharacterLevel");
        var colorRegions = new byte?[12];
        if (properties.TryGet<IList>("ColorSetIndices", out var arrayElements))
        {
            foreach(var element in arrayElements.OfType<ByteProperty>())
            {
                colorRegions[element.Tag.ArrayIndex] = element.Value;
            }
        }


        var creature = new Creature
        {
            Id = record.Uuid,
            ClassName = record.GetClassName(),
            TamedName = properties.Get<string>("TamedName"),
            TribeName = properties.Get<string>("TribeName"),
            TamerString = properties.Get<string>("TamerString"),
            IsTamed = record.IsTamed(),
            IsFemale = properties.Get<bool>("bIsFemale"),
            BaseLevel = baseLevel,
            ExtraLevel = extraCharacterLevel,
            TotalLevel = baseLevel is int b ? b + extraCharacterLevel : null,
            MutationsMale = mutationsMale,
            MutationsFemale = mutationsFemale,
            TotalMutations = mutationsMale + mutationsFemale,
            ImprintQuality = properties.Get<float>("ImprintQuality"),
            ImprinterName = properties.Get<string>("ImprinterName"),
            BabyAge = properties.TryGet<float>("BabyAge", out var babyAge) ? babyAge : null,
            IsJuvenile = properties.Get<bool>("bBabyInitialized"),
            TamedAtTime = properties.TryGet<double>("TamedAtTime", out var tamedAtTime) ? tamedAtTime : null,
            OriginalCreationGameTime = properties.TryGet<double>("OriginalCreationTime", out var oct) ? oct : null,
            DinoId1 = properties.TryGet<uint>("DinoID1", out var dinoId1) ? dinoId1 : null,
            DinoId2 = properties.TryGet<uint>("DinoID2", out var dinoId2) ? dinoId2 : null,
            ColorRegions = colorRegions,
            Location = transform?.Location,
            Rotation = transform?.Rotation,
            Record = record
        };

        if (properties.Get<ArrayProperty>("DinoGeneTraits")?.Value is { } traitsArray)
        {
            foreach (var trait in traitsArray)
            {
                if (trait != null)
                {
                    creature.GeneTraits.Add(trait.ToString() ?? "");
                }
            }
        }

        var statusComponent = record.GetCharacterStatusComponent();
        if (statusComponent != null)
        {
            creature.IngestStatusRecord(statusComponent);
        }

        return creature;
    }

    internal void RefreshTimestamps(DateTime saveTimestamp, double gameTime)
    {
        if (OriginalCreationGameTime != null && OriginalCreationGameTime > 0)
        {
            OriginalCreationTimestamp = saveTimestamp.AddSeconds(OriginalCreationGameTime.Value - gameTime);
        }
    }

    private void IngestInventoryRecord(GameObjectRecord inventoryComponent)
    {
        throw new NotImplementedException();
    }

    private static T?[] GetStatValues<T>(GameObjectRecord statusComponent, string propertyName, int count) where T : struct
    {
        var levels = new T?[count];
        for (var i = 0; i < count; i++)
        {
            if (statusComponent.Properties.TryGet<T>(propertyName, i, out var level))
            {
                levels[i] = level;
            }
        }
        return levels;
    }

    internal void IngestStatusRecord(GameObjectRecord statusComponent)
    {
        WildLevels = GetStatValues<byte>(statusComponent, "NumberOfLevelUpPointsApplied", 12);
        TamedLevelsApplied = GetStatValues<byte>(statusComponent, "NumberOfLevelUpPointsAppliedTamed", 12);
        MutationLevelsApplied =GetStatValues<byte>(statusComponent, "NumberOfMutationsAppliedTamed", 12);
        StatsCurrent = GetStatValues<float>(statusComponent, "CurrentStatusValues", 12);
    }

    internal void IngestInventoryRecord(GameObjectRecord inventoryComponent, IDictionary<Guid, Item> inventoryItems)
    {
        Inventory = Inventory.Create(inventoryComponent, inventoryItems);
    }
}
