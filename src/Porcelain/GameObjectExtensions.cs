using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using AsaSavegameToolkit.Porcelain.ArkData;

namespace AsaSavegameToolkit.Porcelain;

/// <summary>
/// Extension methods for <see cref="GameObjectRecord"/> that provide typed property access
/// without needing to work directly with the raw property bag.
/// </summary>
public static class GameObjectExtensions
{
    /// <summary>
    /// Gets the short class name (e.g., "Yutyrannus_Character_BP_C").
    /// </summary>
    public static string GetClassName(this GameObjectRecord record)
    {
        return record.ClassName.FullName;
    }

    /// <summary>
    /// Checks if the class name contains the given substring (case-insensitive).
    /// Useful for filtering by blueprint prefix (e.g., "_Character_").
    /// </summary>
    public static bool ClassNameContains(this GameObjectRecord record, string substring)
    {
        return record.ClassName.FullName.Contains(substring, StringComparison.OrdinalIgnoreCase);
    }

    public readonly static string[] NonCreatureDisambiguatedClasses =
    [
        "Raft_BP_C",
        "MotorRaft_BP_C",
        "TekHoverSkiff_Character_BP_C",
        "CogRaft_BP_C",
        "DingyRaft_BP_C",
        "LongshipRaft_BP_C",
        "SRaft_BP_C",
        "CherufeNest_C"
    ];

    public static bool IsStructure(this GameObjectRecord gameObject)
    {
        var className = GetClassName(gameObject);
        return className != "Structure_LoadoutDummy_Hotbar_C"
                //&& !gameObject.IsInCryo
                && (
                    gameObject.Properties.HasAny("OwnerName")
                    || gameObject.Properties.HasAny("bHasResetDecayTime")
                    || NonCreatureDisambiguatedClasses.Contains(className)
                )
                && !className.StartsWith("DeathItemCache_");
    }

    public static bool IsTribe(this GameObjectRecord gameObject)
    {
        return gameObject.ClassName == "/Script/ShooterGame.PrimalTribeData";
    }

    public static bool IsProfile(this GameObjectRecord gameObject)
    {
        return gameObject.Properties.HasAny("MyData");
    }

    public static bool IsCreature(this GameObjectRecord gameObject)
    {
        return gameObject.Properties.HasAny("bServerInitializedDino")
            && !NonCreatureDisambiguatedClasses.Contains(gameObject.ClassName.FullName);
    }

    public static bool IsDroppedItem(this GameObjectRecord gameObject)
    {
        return gameObject.Properties.HasAny("MyItem") & !gameObject.ClassName.FullName.Contains("NoStasis");
    }

    public static bool IsInventoryItem(this GameObjectRecord gameObject)
    {
        return gameObject.Properties.HasAny("OwnerInventory");
    }

    public static bool IsItem(this GameObjectRecord gameObject)
    {
        return gameObject.Properties.HasAny("ItemID");
    }

    public static bool IsInventory(this GameObjectRecord gameObject)
    {
        return gameObject.Properties.HasAny("bInitializedMe");
    }

    public static bool IsPlayerComponent(this GameObjectRecord gameObject)
    {
        return gameObject.Properties.HasAny("LinkedPlayerDataID") || gameObject.Properties.HasAny("PlayerDataID");
    }

    public static bool IsStatusComponent(this GameObjectRecord gameObject)
    {
        return gameObject.Properties.HasAny("bServerFirstInitialized");
    }

    public static bool IsTamed(this GameObjectRecord gameObject)
    {
        if(!gameObject.IsCreature())
        {
            return false;
        }

        var teamId = gameObject.Properties.Get<int>("TargetingTeam");
        return TeamInfo.IsTamed(teamId);
    }

    public static bool IsWild(this GameObjectRecord gameObject)
    {
        if (!gameObject.IsCreature())
        {
            return false;
        }

        var teamId = gameObject.Properties.Get<int>("TargetingTeam");
        return !TeamInfo.IsTamed(teamId);
    }

    public static bool IsUnclaimedBaby(this GameObjectRecord gameObject)
    {
        if (!gameObject.IsCreature())
        {
            return false;
        }

        var teamId = gameObject.Properties.Get<int>("TargetingTeam");
        var teamType = TeamInfo.GetTeamType(teamId);
        return teamType == TeamType.Breeding;
    }

    public static bool IsWeapon(this GameObjectRecord gameObject)
    {
        return gameObject.Properties.HasAny("AssociatedPrimalItem") || gameObject.Properties.HasAny("MyPawn");
    }

    public static bool IsDeathItemCache(this GameObjectRecord gameObject)
    {
        return gameObject.ClassName.FullName.StartsWith("DeathItemCache_");
    }

    public static bool IsFemale(this GameObjectRecord gameObject)
    {
        return gameObject.Properties.Get<bool>("bIsFemale");
    }

    public static GameObjectRecord? GetCharacterStatusComponent(this GameObjectRecord gameObject)
    {
        if (gameObject.Properties.TryGet<ObjectReference>("MyCharacterStatusComponent", out var component))
        {
            return gameObject.Components.Values.FirstOrDefault(x => x.Uuid == component.ObjectId);
        }
        else
        {
            return gameObject.Components.Where(x => x.Key.StartsWith("DinoCharacterStatus_")).Select(x => x.Value).FirstOrDefault();
        }
    }

    public static GameObjectRecord? GetInventoryComponent(this GameObjectRecord gameObject)
    {
        if (gameObject.Properties.TryGet<ObjectReference>("MyInventoryComponent", out var component))
        {
            return gameObject.Components.Values.FirstOrDefault(x => x.Uuid == component.ObjectId);
        }

        return null;
    }
    
    public static int GetBaseLevel(this GameObjectRecord gameObject)
    {
        GameObjectRecord? statusComponent = gameObject.GetCharacterStatusComponent();

        if (statusComponent == null)
        {
            return 1;
        }

        return statusComponent.Properties.TryGet<int>("BaseCharacterLevel", out var value) ? value : 1;
    }

    public static int GetFullLevel(this GameObjectRecord gameObject)
    {
        GameObjectRecord? statusComponent = gameObject.GetCharacterStatusComponent();
      
        if (statusComponent == null)
        {
            return 1;
        }

        /*
        if(!statusComponent.Properties.TryGet<int>("BaseCharacterLevel", out var baseLevel))
        {
            baseLevel = 0;
        }
        */

        int wildLevels = GetStatValues<byte>(statusComponent, "NumberOfLevelUpPointsApplied", 12).Sum(x=>x.GetValueOrDefault(0));
        int tamedLevels = GetStatValues<byte>(statusComponent, "NumberOfLevelUpPointsAppliedTamed", 12).Sum(x=>x.GetValueOrDefault(0));
        int tamedMutations = GetStatValues<byte>(statusComponent, "NumberOfMutationsAppliedTamed", 12).Sum(x => x.GetValueOrDefault(0));
        
        var totalLevel = wildLevels + tamedLevels + tamedMutations + 1;

        return totalLevel; 


    }

    public static T?[] GetStatValues<T>(this GameObjectRecord statusComponent, string propertyName, int count) where T : struct
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

    public static string? GetNameForCreature(this GameObjectRecord gameObject, ArkDataProvider arkData, string valueIfNotFound = "")
    {
        return arkData.GetCreatureForClass(gameObject.ClassName.FullName)?.Name ?? valueIfNotFound;
    }

    public static string? GetNameForStructure(this GameObjectRecord gameObject, ArkDataProvider arkData, string valueIfNotFound = "")
    {
        return arkData.GetStructureForClass(gameObject.ClassName.FullName)?.Name ?? valueIfNotFound;
    }

    public static string? GetNameForItem(this GameObjectRecord gameObject, ArkDataProvider arkData, string valueIfNotFound = "")
    {
        return arkData.GetItemForClass(gameObject.ClassName.FullName)?.Name ?? valueIfNotFound;
    }

    public static long GetDinoId(this GameObjectRecord gameObject)
    {
        return CreateDinoId(gameObject.Properties.Get<int>("DinoID1", 0), gameObject.Properties.Get<int>("DinoID2", 0));
    }

    public static long CreateDinoId(int id1, int id2)
    {
        return long.Parse($"{id1}{id2}");

        //return ((long)id1 << 32) | (id2 & 0xFFFFFFFFL);
    }   
}
