namespace AsaSavegameToolkit.Plumbing.Records;

/// <summary>
/// Flags indicating the type/category of a game object.
/// These bits determine how the object behaves and where it exists in the game world.
/// Analysis of 61,272 game objects shows only 5 combinations are used.
/// </summary>
[Flags]
public enum ObjectTypeFlags : byte
{
    /// <summary>
    /// Component objects - support objects attached to other entities.
    /// Examples: DinoCharacterStatusComponent, StructurePaintingComponent
    /// Not items, not spawning-related, not physical actors.
    /// </summary>
    None = 0x00,
    
    /// <summary>
    /// Bit 0: Item/inventory object flag.
    /// Indicates this is an item that exists in inventories.
    /// Examples: weapons, resources, consumables, armor, saddles.
    /// </summary>
    Item = 0x01,
    
    /// <summary>
    /// Bit 1: Spawning/NPC management flag.
    /// Indicates this object is involved in NPC spawning logic.
    /// Examples: NPCZoneManagerBlueprint (logic-only spawning controllers).
    /// </summary>
    Spawning = 0x02,
    
    /// <summary>
    /// Bit 3: Physical actor flag.
    /// Indicates this is a physical object placed in the game world.
    /// Examples: dinosaurs, structures, buildings.
    /// </summary>
    Actor = 0x08,
    
    /// <summary>
    /// Combined flags: Spawning + Actor (0x02 | 0x08 = 0x0A).
    /// Physical volumes used for spawn management.
    /// Examples: NPCZoneVolume, InstancedFoliageActor, tribute terminals.
    /// </summary>
    SpawnVolume = Spawning | Actor  // 0x0A
}
