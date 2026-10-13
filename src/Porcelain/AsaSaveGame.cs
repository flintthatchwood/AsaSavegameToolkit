using System.Threading;

using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AsaSavegameToolkit.Porcelain;

public class AsaSaveGame
{
    public int SaveVersion { get; set; }
    public TimeSpan GameTime { get; set; }

    public required IDictionary<Guid, Player> Players { get; set; }
    public required IDictionary<Guid, Tribe> Tribes { get; set; }
    public required IDictionary<Guid, Creature> WildCreatures { get; set; }
    public required IDictionary<Guid, Creature> TamedCreatures { get; set; }
    /// <summary>
    /// Creatures currently stored in cryopods, keyed by the cryopod's id (two pods can hold copies of the same
    /// creature). If a pod ever holds a second creature, that one is keyed by its own id. These are not live world
    /// objects.
    /// </summary>
    public required IDictionary<Guid, Creature> CryopoddedCreatures { get; set; }
    public required IDictionary<Guid, Structure> Structures { get; set; }
    public required IDictionary<Guid, Item> DroppedItems { get; set; }

    public static AsaSaveGame ReadFrom(string path, ILogger? logger = null, AsaReaderSettings? settings = null, CancellationToken cancellationToken = default)
    {
        logger ??= NullLogger.Instance;
        using var reader = new AsaSaveReader(path, logger, settings ?? AsaReaderSettings.None);
        using var cryoReader = new CryopodReader(logger, settings);

        var header = reader.ReadSaveHeader(cancellationToken);
        var gameObjects = reader.ReadGameRecords(cancellationToken);
        var transforms = reader.ReadActorTransforms(cancellationToken).Transforms;
        var customBytes = reader.ReadGameModeCustomBytes(cancellationToken);

        var creatureRecords = new Dictionary<Guid, GameObjectRecord>();
        var droppedItemRecords = new Dictionary<Guid, GameObjectRecord>();
        var inventoryItemRecords = new Dictionary<Guid, GameObjectRecord>();
        var playerRecords = new Dictionary<Guid, GameObjectRecord>();
        var structureRecords = new Dictionary<Guid, GameObjectRecord>();
        var deathCacheRecords = new Dictionary<Guid, GameObjectRecord>();
        var ignoredRecords = new Dictionary<Guid, GameObjectRecord>();
        var unknownRecords = new Dictionary<Guid, GameObjectRecord>();

        var topLevelRecords = NestComponents(gameObjects.Values, logger);

        // Third Pass: now that we've nested all the components, we can categorize the top level game objects by type
        foreach (var gameObject in topLevelRecords)
        {
            if (gameObject.IsCreature())
                creatureRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsPlayer())
                playerRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsStructure())
                structureRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsInventoryItem())
                inventoryItemRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsDeathItemCache())
                deathCacheRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsDroppedItem() || (gameObject.ObjectType == ObjectTypeFlags.Item && transforms.ContainsKey(gameObject.Uuid)))
                droppedItemRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.ClassNameContains("NPCZone") || gameObject.ClassNameContains("NPCCount"))
                ignoredRecords[gameObject.Uuid] = gameObject;
            else
                unknownRecords[gameObject.Uuid] = gameObject;
        }

        // The full record dictionaries are no longer needed now that every object has been
        // placed into a typed bucket. Release them so the GC can collect the raw
        // GameObjectRecord graph before we begin allocating the Porcelain objects.
        gameObjects = null!;
        topLevelRecords = null!;

        var tribes = new Dictionary<Guid, Tribe>();

        var structures = structureRecords.ToDictionary(
            r => r.Key,
            r => Structure.Create(r.Value, transforms.TryGetValue(r.Key, out var t) ? t : null));

        var players = playerRecords.ToDictionary(
            r => r.Key, 
            r => Player.Create(r.Value, transforms.TryGetValue(r.Key, out var t) ? t : null));

        var creatures = creatureRecords.ToDictionary(
            r => r.Key,
            r => Creature.Create(r.Value, transforms.TryGetValue(r.Key, out var t) ? t : null));

        var droppedItems = droppedItemRecords.ToDictionary(
            r => r.Key,
            r => Item.Create(r.Value, transforms.TryGetValue(r.Key, out var t) ? t : null));

        var inventoryItems = inventoryItemRecords.ToDictionary(
            r => r.Key,
            r => Item.Create(r.Value));

        var tamedCreatures = creatures.Values.Where(c => c.IsTamed).ToDictionary(x => x.Id);

        var wildCreatures = creatures.Values.Where(c => !c.IsTamed).ToDictionary(x => x.Id);

        // Extract creatures from cryopods. Each cryopod item holds compressed byte arrays that represent the
        // creature, status component and saddles that would be spawned on deployment. These are not world objects, so
        // they get their own list rather than appearing in TamedCreatures.
        // Filter: class must have a CustomItemData with CustomDataName=="Dino" and a non-empty dino blob at CustomDataBytes[0].
        var cryopoddedCreatures = new Dictionary<Guid, Creature>();
        foreach (var cryopod in inventoryItemRecords.Values.Where(r => r.HasCryoCreature()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cryoRecordSets = cryoReader.ReadCryopodData(cryopod, cancellationToken);

            var podCreatures = CreaturesFromCryopod(cryopod, cryoRecordSets, logger);
            if (podCreatures.Count == 0)
            {
                continue;
            }

            cryopoddedCreatures[cryopod.Uuid] = podCreatures[0];
            foreach (var extra in podCreatures.Skip(1))
            {
                cryopoddedCreatures[extra.Id] = extra;
            }
        }

        return new AsaSaveGame
        {
            SaveVersion = header.SaveVersion,
            GameTime = TimeSpan.FromSeconds(header.GameTime - 100000000),
            Players = players,
            Tribes = tribes,
            TamedCreatures = tamedCreatures,
            WildCreatures = wildCreatures,
            CryopoddedCreatures = cryopoddedCreatures,
            Structures = structures,
            DroppedItems = droppedItems,
        };
    }

    /// <summary>
    /// Builds the creatures in one cryopod from its record sets (<see cref="CryopodReader.ReadCryopodData"/>).
    /// A creature's set holds the creature, its status component and anything it carries (e.g. a Carcha holding a
    /// player, with that player's own status component). Each set after it is one item the creature wears (saddle,
    /// costume, hat), which goes in the creature's inventory like a live creature's equipped items.
    /// </summary>
    internal static List<Creature> CreaturesFromCryopod(GameObjectRecord cryopod, IReadOnlyList<GameObjectRecord[]> cryoRecordSets, ILogger logger)
    {
        var creatures = new List<Creature>();
        Creature? creature = null;

        // A picked-up car is stored the same way, with the car where the creature would be. It isn't a creature.
        if (cryoRecordSets.Count > 0
            && !cryoRecordSets[0].Any(r => r.IsCreature())
            && cryoRecordSets[0].Any(r => GameObjectExtensions.NonCreatureDisambiguatedClasses.Contains(r.GetShortClassName())))
        {
            return creatures;
        }

        foreach (var cryoRecords in cryoRecordSets)
        {
            var dinoRecord = cryoRecords.FirstOrDefault(r => r.IsCreature());
            if (dinoRecord != null)
            {
                creature = Creature.Create(dinoRecord, null);

                // The creature's own status component names the creature as its owner. Its position in the set varies
                // (a carried player comes before it).
                var statusRecord = cryoRecords.FirstOrDefault(r => r != dinoRecord && r.Names.Count > 1 && r.Names[1] == dinoRecord.Names[0]);
                if (statusRecord != null)
                {
                    creature.IngestStatusRecord(statusRecord);
                }

                creature.IsInCryo = true;
                creatures.Add(creature);
                continue;
            }

            if (creature == null)
            {
                logger.LogWarning("Cryopod with name {CryopodName} and UUID {CryopodUuid} has item data before any creature data", cryopod.Names[0], cryopod.Uuid);
                continue;
            }

            creature.Inventory ??= new Inventory
            {
                Id = Guid.NewGuid(),
                ClassName = "CryoSaddleInventory",
            };
            foreach (var itemRecord in cryoRecords)
            {
                var item = Item.FromCryoSaddle(itemRecord);
                creature.Inventory.Items[item.Id] = item;
            }
        }

        if (creatures.Count == 0)
        {
            logger.LogWarning("Cryopod with name {CryopodName} and UUID {CryopodUuid} does not contain any creature data", cryopod.Names[0], cryopod.Uuid);
        }
        else if (creatures.Count > 1)
        {
            logger.LogWarning("Cryopod with name {CryopodName} and UUID {CryopodUuid} contains more than one creature", cryopod.Names[0], cryopod.Uuid);
        }

        return creatures;
    }

    /// <summary>
    /// Attaches each component (an object with more than one name) to its parent and returns the top-level objects.
    /// Top-level names aren't always unique (e.g. two LadderBP structures or InstancedFoliageActors can share one),
    /// so every top-level object is kept and a component whose parent name is shared is left unattached.
    /// </summary>
    internal static List<GameObjectRecord> NestComponents(IEnumerable<GameObjectRecord> gameObjects, ILogger logger)
    {
        var topLevel = new List<GameObjectRecord>();
        var components = new List<GameObjectRecord>();
        var recordsByName = new Dictionary<string, GameObjectRecord>();
        var sharedNames = new HashSet<string>();

        // First pass: place all top-level objects (those with only 1 name) into recordsByName so they can be
        // referenced as parents by their components in the second pass.
        foreach (var gameObject in gameObjects)
        {
            if (gameObject.Names.Count > 1)
            {
                components.Add(gameObject);
            }
            else if (gameObject.Names.Count == 1)
            {
                topLevel.Add(gameObject);
                if (!recordsByName.TryAdd(gameObject.Names[0], gameObject))
                {
                    sharedNames.Add(gameObject.Names[0]);
                }
            }
        }

        // Second pass: for each game object with multiple names, find its parent object in recordsByName using the
        // last name in the Names list, then crawl down the component stack using the preceding names until we find the
        // correct parent to attach this component to.
        foreach (var gameObject in components)
        {
            var parentName = gameObject.Names[^1];
            if (sharedNames.Contains(parentName))
            {
                logger.LogWarning(
                    "More than one game object is named {ParentName}, so game object {ChildName} can't be attached to its parent",
                    parentName,
                    gameObject.Names[0]);
                continue;
            }

            if (!recordsByName.TryGetValue(parentName, out var parent))
            {
                // only warn about missing parents if the parent name isn't rooted.
                // We don't expect to find `/Game/PrimalEarth/Sound/PlayerVoice/Female_Legacy/DA_Female_Legacy` in the records.
                if (!parentName.StartsWith('/'))
                {
                    logger.LogWarning(
                        "Could not find parent game object with name {ParentName} for game object with name {ChildName}",
                        gameObject.Names[1],
                        gameObject.Names[0]);
                }
                continue;
            }

            // The component names are in deepest-first order, so crawl from the back of the list forward down the component stack
            // This will probably never be be invoked as there are no known objects with more than 2 names
            for (var i = gameObject.Names.Count - 2; i > 0; i--)
            {
                parent = parent.Components[gameObject.Names[i]];
            }

            parent.Components[gameObject.Names[0]] = gameObject;
        }

        return topLevel;
    }
}
