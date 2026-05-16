using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualBasic;
using System.Collections.Concurrent;
using System.Data;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace AsaSavegameToolkit.Porcelain;

public class AsaSaveGame
{
    public int SaveVersion { get; set; }
    public string SaveFilename { get; set; } = string.Empty;
    public DateTime SaveTimestamp { get; set; }= DateTime.Now;
    private double GameTime { get; set; } = 0;

    public required IDictionary<Guid, Player> Players { get; set; }
    public required IDictionary<Guid, Tribe> Tribes { get; set; }
    public required IDictionary<Guid, CreatureWild> WildCreatures { get; set; }
    public required IDictionary<Guid, CreatureTamed> TamedCreatures { get; set; }
    public required IDictionary<Guid, Structure> Structures { get; set; }
    public required IDictionary<Guid, DroppedItem> DroppedItems { get; set; }

    private static Dictionary<string,MapDefinition> MapDefinitions { get; set; } = new Dictionary<string,MapDefinition>();

    private static void ReadMapDefinitions()
    {
        MapDefinitions = new Dictionary<string,MapDefinition>();
        
        string mapDefinitionFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "maps.json");
        if (!File.Exists(mapDefinitionFilename)) return;// no maps configured

        try
        {
            string fileContent = File.ReadAllText(mapDefinitionFilename);
            JsonObject? mapConfig = (JsonObject?)JsonObject.Parse(fileContent);
            if (mapConfig != null)
            {
                var mapList = mapConfig["maps"]?.AsArray();
                if(mapList==null) return; //no maps found

                foreach (var mapData in mapList) 
                {
                    var mapDefinition = new MapDefinition();
                    mapDefinition.Name = mapData["MapName"].GetValue<string>().ToLower();
                    mapDefinition.DisplayName = mapData["DisplayName"].GetValue<string>(); 
                    mapDefinition.ImageFile = mapData["ImageFile"].GetValue<string>();
                    mapDefinition.ScaleX = mapData["ScaleX"].GetValue<double>();
                    mapDefinition.ScaleY = mapData["ScaleY"].GetValue<double>();
                    mapDefinition.OffsetX = mapData["OffsetX"].GetValue<double>();
                    mapDefinition.OffsetY = mapData["OffsetY"].GetValue<double>();

                    MapDefinitions.Add(mapDefinition.Name, mapDefinition);
                }
            }

        }
        catch(Exception ex)
        {

        }
    }

    public static AsaSaveGame ReadFrom(string path, ILogger? logger = null, AsaReaderSettings? settings = null, CancellationToken cancellationToken = default)
    {
        logger ??= NullLogger.Instance;
        var saveFileTimestamp = File.GetLastWriteTimeUtc(path);

        using var reader = new AsaSaveReader(path, logger, settings ?? AsaReaderSettings.None);

        var header = reader.ReadSaveHeader(cancellationToken);

        ReadMapDefinitions();

        MapDefinition? mapDefinition = null;
        if (MapDefinitions.ContainsKey(header.MapName.ToLower()))
        {
            mapDefinition = MapDefinitions[header.MapName.ToLower()];
        }

        var gameObjects = reader.ReadGameRecords(cancellationToken);
        var transforms = reader.ReadActorTransforms(cancellationToken).Transforms;

        var creatureRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();
        var statusRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();
        var inventoryRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();

        var droppedItemRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();     
        var itemRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();

        var tribeRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();
        var profileRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();
        var playerComponentRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();
        var structureRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();
        var deathCacheRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();
        var ignoredRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();
        var unknownRecords = new ConcurrentDictionary<Guid, GameObjectRecord>();


        // Third Pass: now that we've nested all the components, we can categorize the top level game objects by type
        Parallel.ForEach(gameObjects.Values, gameObject =>
        //foreach (var gameObject in gameObjects.Values)
        {
            if (gameObject.IsCreature())
                creatureRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsTribe())
                tribeRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsProfile())
                profileRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsPlayerComponent())
                playerComponentRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsStatusComponent())
                statusRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsStructure())
                structureRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsInventory())
                inventoryRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsItem())
                itemRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsDeathItemCache())
                deathCacheRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.IsDroppedItem() || (gameObject.ObjectType == ObjectTypeFlags.Item && transforms.ContainsKey(gameObject.Uuid)))
                droppedItemRecords[gameObject.Uuid] = gameObject;
            else if (gameObject.ClassNameContains("NPCZone") || gameObject.ClassNameContains("NPCCount"))
                ignoredRecords[gameObject.Uuid] = gameObject;
            else
                unknownRecords[gameObject.Uuid] = gameObject;
        });


        // The full record dictionaries are no longer needed now that every object has been
        // placed into a typed bucket. Release them so the GC can collect the raw
        // GameObjectRecord graph before we begin allocating the Porcelain objects.
        gameObjects = null!;

        var structures = structureRecords.ToDictionary(
            r => r.Key,
            r => 
            {
                var structure = Structure.Create(r.Value, transforms.TryGetValue(r.Key, out var t) ? t : null);

                var inventoryComponentRef = (ObjectReference?)r.Value.Properties.Get<ObjectProperty>("MyInventoryComponent")?.Value;
                if (inventoryComponentRef != null && inventoryRecords.ContainsKey(inventoryComponentRef.ObjectId))
                {
                    var inventoryRecord = inventoryRecords[inventoryComponentRef.ObjectId];

                    Inventory inventory = new Inventory();
                    List<Item> items = new List<Item>();

                    //InventoryItems
                    var inventoryItems = inventoryRecord.Properties.Get<ArrayProperty>("InventoryItems")?.Value;
                    if (inventoryItems != null && inventoryItems.Count > 0)
                    {
                        foreach (ObjectReference itemReference in inventoryItems)
                        {
                            if (itemReference.ObjectId == Guid.Empty || !itemRecords.ContainsKey(itemReference.ObjectId))
                            {
                                continue; //skip empty slots
                            }
                            var itemRecord = itemRecords[itemReference.ObjectId];
                            var item = Item.Create(itemRecord);
                            items.Add(item);
                        }
                    }

                    if (items.Count > 0)
                    {
                        inventory.Items = items;
                        structure.IngestInventory(inventory);
                    }
                }


                /* Dedicated Storage Inventory */
                ObjectReference? dedicatedStorageClassReference = r.Value.Properties.Get<ObjectProperty>("SelectedResourceClass")?.Value;
                if (dedicatedStorageClassReference != null)
                {
                    Inventory inventory = new Inventory();
                    string itemClass = dedicatedStorageClassReference.Value;
                    int itemQuantity = (int)r.Value.Properties.Get<long>("ResourceCount");
                    Item item = new Item() { ClassName = itemClass, Id = Guid.NewGuid(), Quantity= itemQuantity};
                    inventory.Items.Add(item);
                    structure.IngestInventory(inventory);
                }

                structure.UpdateGPSLocation(mapDefinition);
                return structure;
            });

        var players = profileRecords.ToDictionary(
            r => r.Key,
            r =>
            {
                ulong playerDataId = 0;

                var myData = r.Value.Properties.Get<StructProperty>("MyData");
                if (myData != null)
                {
                    List<Property> properties = (List<Property>)myData.Value;
                    playerDataId = properties.Get<ulong>("PlayerDataID");

                }
                List<GameObjectRecord> playerComponents = playerComponentRecords.Values.Where(v => v.Properties.Get<ulong>("LinkedPlayerDataID") == playerDataId).ToList();
                
                var characterRecord = playerComponents.FirstOrDefault(c => !c.IsStatusComponent());
                GameObjectRecord? statusRecord = null;

                ActorTransform? actorLocation = null;
                if (characterRecord != null)
                {
                    actorLocation = transforms.TryGetValue(characterRecord.Uuid, out var t) ? t : null;

                    var statusRefProperty = (ObjectReference)characterRecord.Properties.Get<ObjectProperty>("MyCharacterStatusComponent")?.Value;

                    var statusRef = statusRefProperty?.ObjectId;
                    if (statusRef != null && statusRecords.ContainsKey(statusRef.Value))
                        statusRecord = statusRecords[statusRef.Value];
                }

                var player = Player.Create(r.Value, actorLocation);
                
                if (characterRecord != null)
                {
                    player.IngestCharacterRecord(characterRecord);

                    var inventoryComponentRef = (ObjectReference?)characterRecord.Properties.Get<ObjectProperty>("MyInventoryComponent")?.Value;
                    if (inventoryComponentRef != null && inventoryRecords.ContainsKey(inventoryComponentRef.ObjectId))
                    {
                        var inventoryRecord = inventoryRecords[inventoryComponentRef.ObjectId];

                        Inventory inventory = new Inventory();
                        List<Item> items = new List<Item>();

                        //InventoryItems
                        var inventoryItems = inventoryRecord.Properties.Get<ArrayProperty>("InventoryItems")?.Value;
                        if (inventoryItems != null && inventoryItems.Count > 0)
                        {
                            foreach (ObjectReference itemReference in inventoryItems)
                            {
                                if (itemReference.ObjectId == Guid.Empty || !itemRecords.ContainsKey(itemReference.ObjectId))
                                {
                                    continue; //skip empty slots or those we can't find a matching item
                                }
                                var itemRecord = itemRecords[itemReference.ObjectId];
                                var item = Item.Create(itemRecord);

                                var existingItem = items.FirstOrDefault(i => i.ClassName == item.ClassName);
                                if (existingItem != null)
                                {
                                    item.Quantity+=existingItem.Quantity;
                                    items.Remove(existingItem);
                                }

                                items.Add(item);
                                
                            }
                        }

                        if (items.Count > 0)
                        {
                            inventory.Items = items;
                            player.IngestInventory(inventory);
                        }                            
                    }
                }
                if (statusRecord != null)
                    player.IngestStatusRecord(statusRecord);

                player.RefreshTimestamps(saveFileTimestamp, header.GameTime);
                player.UpdateGPSLocation(mapDefinition);

                return player;
            });


        var tribes = tribeRecords.ToDictionary(
        r => r.Key,
        r =>
        {
            return Tribe.Create(r.Value);
        });


        var creatures = creatureRecords.ToDictionary(
            r => r.Key,
            r => {

                var locationKey = r.Key;
                if (r.Value.Properties.HasAny("CryoContainer"))
                {
                    locationKey = (r.Value.Properties.Get<ObjectProperty>("CryoContainer").Value as ObjectReference).ObjectId;

                }

                var creature = Creature.Create(r.Value, transforms.TryGetValue(locationKey, out var t) ? t : null);
                
                var statusComponentRef = (ObjectReference?)r.Value.Properties.Get<ObjectProperty>("MyCharacterStatusComponent")?.Value;
                if (statusComponentRef != null)
                {
                    if (statusRecords.ContainsKey(statusComponentRef.ObjectId))
                    {
                        var statusComponent = statusRecords[statusComponentRef.ObjectId];
                        creature.IngestStatusRecord(statusComponent);
                    }
                }

                var inventoryComponentRef = (ObjectReference?)r.Value.Properties.Get<ObjectProperty>("MyInventoryComponent")?.Value;
                if (inventoryComponentRef != null && inventoryRecords.ContainsKey(inventoryComponentRef.ObjectId))
                {
                    var inventoryRecord = inventoryRecords[inventoryComponentRef.ObjectId];
                    Inventory inventory = new Inventory();
                    List<Item> items = new List<Item>();


                    //InventoryItems
                    var inventoryItems = inventoryRecord.Properties.Get<ArrayProperty>("InventoryItems")?.Value;
                    if (inventoryItems != null && inventoryItems.Count > 0)
                    {
                        foreach (ObjectReference itemReference in inventoryItems)
                        {
                            if (itemReference.ObjectId == Guid.Empty || !itemRecords.ContainsKey(itemReference.ObjectId))
                            {
                                continue; //skip empty slots
                            }
                            var itemRecord = itemRecords[itemReference.ObjectId];
                            var item = Item.Create(itemRecord);
                            items.Add(item);
                        }
                    }

                    //EquippedItems
                    var equippedSlots = inventoryRecord.Properties.Get<ArrayProperty>("EquippedItems")?.Value;
                    if (equippedSlots != null && equippedSlots.Count > 0)
                    {
                        foreach (ObjectReference itemReference in equippedSlots)
                        {
                            if (itemReference.ObjectId == Guid.Empty || !itemRecords.ContainsKey(itemReference.ObjectId))
                            {
                                continue; //skip empty slots
                            }
                            var itemRecord = itemRecords[itemReference.ObjectId];
                            var item = Item.Create(itemRecord);
                            items.Add(item);
                        }
                    }


                    //ItemSlots
                    var itemSlots = inventoryRecord.Properties.Get<ArrayProperty>("ItemSlots")?.Value;
                    if (itemSlots != null && itemSlots.Count > 0)
                    {
                        foreach (ObjectReference itemReference in itemSlots)
                        {
                            if (itemReference.ObjectId == Guid.Empty || !itemRecords.ContainsKey(itemReference.ObjectId))
                            {
                                continue; //skip empty slots
                            }
                            var itemRecord = itemRecords[itemReference.ObjectId];
                            var item = Item.Create(itemRecord);
                            items.Add(item);
                        }
                    }

                    if (items.Count > 0)
                    {
                        inventory.Items = items;
                        creature.IngestInventory(inventory);
                    }
                }

                creature.RefreshTimestamps(saveFileTimestamp, header.GameTime);
                creature.UpdateGPSLocation(mapDefinition);
                return creature;
            });

   
        
        var droppedItems = droppedItemRecords.ToDictionary(
            r => r.Key,
            r => 
            {
                ActorTransform? itemLocation = transforms.TryGetValue(r.Key, out var t) ? t : null;
                GameObjectRecord itemRecord = r.Value;
                
                var droppedItem = DroppedItem.Create(itemRecord, itemLocation);

                var myObjectRef = (ObjectReference?)r.Value.Properties.Get<ObjectProperty>("MyItem")?.Value;
                if (myObjectRef != null && itemRecords.ContainsKey(myObjectRef.ObjectId))
                {
                    var referencedObject = itemRecords[myObjectRef.ObjectId];
                    var referencedItem = Item.Create(referencedObject);
                    droppedItem.IngestItem(referencedItem);
                    droppedItem.UpdateGPSLocation(mapDefinition);
                }

                return droppedItem;

            });

        var tamedCreatures = creatures.Values.OfType<CreatureTamed>().ToDictionary(x => x.Id);
        var wildCreatures = creatures.Values.OfType<CreatureWild>().ToDictionary(x => x.Id);


        return new AsaSaveGame
        {
            SaveVersion = header.SaveVersion,
            SaveTimestamp = saveFileTimestamp,
            SaveFilename = path,
            GameTime = header.GameTime,
            Players = players,
            Tribes = tribes,
            TamedCreatures = tamedCreatures,
            WildCreatures = wildCreatures,
            Structures = structures,
            DroppedItems = droppedItems,
        }; 
    }
}
