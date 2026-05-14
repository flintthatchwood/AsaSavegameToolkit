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
    public double GameTime { get; set; }

    public required IDictionary<Guid, Player> Players { get; set; }
    public required IDictionary<Guid, Tribe> Tribes { get; set; }
    public required IDictionary<Guid, Creature> WildCreatures { get; set; }
    public required IDictionary<Guid, Creature> TamedCreatures { get; set; }
    /// <summary>Creatures currently stored in cryopods. These are not live world objects.</summary>
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

        // First pass: place all top-level objects (those with only 1 name) into the recordsByName dictionary so they
        // can be referenced as parents by their components in the second pass.
        Dictionary<string, GameObjectRecord> recordsByName = gameObjects.Values
            .Where(x => x.Names.Count == 1)
            .ToDictionary(x => x.Names[0]);

        // Second pass: for each game object with multiple names, find its parent object in recordsByName using the
        // last name in the Names list, then crawl down the component stack using the preceding names until we find the
        // correct parent to attach this component to.
        foreach (var gameObject in gameObjects.Values.Where(x => x.Names.Count > 1))
        {
            var parentName = gameObject.Names[^1];
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

        // Third Pass: now that we've nested all the components, we can categorize the top level game objects by type
        foreach (var gameObject in recordsByName.Values)
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
        recordsByName = null!;

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
            var cryoRecordSets = cryoReader.ReadCryopodData(cryopod, cancellationToken).ToList();

            // Cryopod records allow for multiple CustomItemDatas entries, each one should probably be processed as a
            // single dino. Still, warn if we see anything other that 1 set

            if (cryoRecordSets.Count == 0)
            {
                logger.LogWarning("Cryopod with name {CryopodName} and UUID {CryopodUuid} does not contain any creature data", cryopod.Names[0], cryopod.Uuid);
                continue;
            }

            if (cryoRecordSets.Count > 1)
            {
                logger.LogWarning("Cryopod with name {CryopodName} and UUID {CryopodUuid} contains more than on set of creature data", cryopod.Names[0], cryopod.Uuid);
            }

            // Nest status components under their parent dino so Creature.Create() can read stat levels.
            // Normally, a creature's equipped saddle still appears in its inventory with an IsEquipped property set.
            // In the cryopod, there is no inventory component and the saddle is in its own record. To make them more
            // like normal creatures, we'll wrap the saddle in an inventory component and attach that to the creature.

            foreach (var cryoRecords in cryoRecordSets)
            {
                var dinoRecords = cryoRecords.Where(r => r.IsCreature()).ToArray();
                if (dinoRecords.Length > 1)
                {
                    logger.LogWarning("Cryopod parsing returned more than one dino object");
                }

                var dinoRecord = dinoRecords.FirstOrDefault();
                if (dinoRecord == null)
                {
                    logger.LogWarning("Cryopod parsing returned no dino records");
                    continue;
                }
                
                var cryoCreature = Creature.Create(dinoRecord, null);

                if (cryoRecords.Length > 1)
                {
                    // process the DinoCharacterStatusComponent record
                    cryoCreature.IngestStatusRecord(cryoRecords[1]);
                }

                if (cryoRecords.Length > 2)
                {
                    // process the costume record
                    //cryoCreature.IngestCostumeRecord(cryoRecords[2]);
                }

                if (cryoRecords.Length > 3)
                {
                    // process the saddle record
                    var saddleObject = Item.FromCryoSaddle(cryoRecords[3]);
                    cryoCreature.Inventory = new Inventory
                    {
                        Id = Guid.NewGuid(),
                        ClassName = "CryoSaddleInventory",
                        Items = 
                        {
                            [saddleObject.Id] = saddleObject
                        }
                    };
                }

                cryoCreature.IsInCryo = true;
                cryopoddedCreatures[cryoCreature.Id] = cryoCreature;
            }
        }

        return new AsaSaveGame
        {
            SaveVersion = header.SaveVersion,
            GameTime = header.GameTime,
            Players = players,
            Tribes = tribes,
            TamedCreatures = tamedCreatures,
            WildCreatures = wildCreatures,
            CryopoddedCreatures = cryopoddedCreatures,
            Structures = structures,
            DroppedItems = droppedItems,
        };
    }
}
