using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using Microsoft.VisualBasic;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;

namespace AsaSavegameToolkit.Porcelain;

/// <summary>
/// Represents a player character in an ARK save file.
/// Wraps a <see cref="GameObjectRecord"/> and exposes typed accessors for common player properties.
/// </summary>
/// <remarks>
/// Full player data (engrams, stats, ascensions) lives in .arkprofile files.
/// This class covers the in-world pawn record from the main .ark save.
/// </remarks>
public class Player
{
    /// <summary>
    /// The player's in-game name.
    /// </summary>
    public string? PlayerName { get; set; }

    public string? CharacterName { get; set;  }

    public long Level { get; set; } = 1;

    /// <summary>
    /// The platform-specific player data ID (Steam ID, etc.).
    /// </summary>
    public long? PlayerDataId { get; set; }

    /// <summary>
    /// The tribe ID this player belongs to. 0 if not in a tribe.
    /// </summary>
    public int? TribeId { get; set; }
    public string? TribeName { get; set; }
    public string? UniqueNetId { get; set; }
    public string? SaveNetworkAddress { get; set; }
    
    public int NumOfDeaths { get; set; }
    public int SpawnDayNumber { get; set; }
    public float SpawnDayTime { get; set; }
    public List<string> HeardVoiceOvers { get; set; } = new List<string>();
    public float ExperiencePoints { get; set; }
    public int TotalEngramPoints { get; set; }
    public float PercentageOfHeadHairGrowth { get; set; }
    public float PercentageOfFacialHairGrowth { get; set; }
    public int TotalSkillPoints { get; set; }
    public int FreeSkillPoints { get; set; }
    public byte[] Stats = new byte[12];
    public float[] StatsCurrent = new float[12];
    public List<string> EngramsLearned { get; set; } = new List<string>();
    public string[] EquippedItems { get; set; } = new string[10];
    public List<int> ExplorerNotesFound { get; set; } = new List<int>();
    public List<string> NamedExplorerNotesFound { get; set; } = new List<string>();
    public List<string> EmotesUnlocked { get; set; } = new List<string>();
    public Dictionary<string, byte> Skills { get; set; } = new Dictionary<string, byte>();
    public Dictionary<string, float> CurrentMilestones { get; set; } = new Dictionary<string, float>();
    public List<string> CompletedMilestones { get; set; } = new List<string>();
    public Color[] BodyColors { get; set; } = new Color[3];
    

    public double? OriginalCreationGameTime { get; set; }
    internal double? LoginGameTime { get; set; }
    internal double? LastLoginGameTime { get; set; }

    public DateTime? OriginalCreationTimestamp { get; set; }
    public DateTime? LoginTimestamp { get; set; }
    public DateTime? LastLoginTimestamp { get; set; }



    public string Gender { get; set; } = "Male";
    public Inventory? Inventory { get; set; }

    /// <summary>
    /// Gets the current location represented as a three-dimensional vector, or null if the location is not set.
    /// </summary>
    public FVector? Location { get; private set; }

    /// <summary>
    /// Gets the rotation represented by a quaternion, if available.
    /// </summary>
    public FQuat? Rotation { get; private set; }
    public FVector2D? GPSLocation { get; private set; }

    public override string ToString() => PlayerName ?? string.Empty;

    /// <summary>
    /// Creates a new Player instance from a record.
    /// </summary>
    public static Player? Create(GameObjectRecord profileRecord, ActorTransform? transform)
    {
        //arkprofile data
        var myDataProperties = profileRecord.Properties.Get<StructProperty>("MyData")?.Value as List<Property>;
        if(myDataProperties == null)
        {           
            return null;
        }        
        
        var linkedPlayerDataId = myDataProperties.Get<ulong>("PlayerDataID"); //PlayerDataId
        var playerName = myDataProperties.Get<string>("PlayerName"); //PlayerName
        var tribeId = myDataProperties.Get<int>("TribeID"); //TribeId
        var uniqueNetIdProperty = (FUniqueNetIdRepl)myDataProperties.Get<StructProperty>("UniqueID").Value; 
        var uniqueNetId = Convert.ToHexString(uniqueNetIdProperty.Id);//UniqueID
        var savedNetAddress = myDataProperties.Get<string>("SavedNetworkAddress"); //SavedNetworkAddress
        var loginTime = myDataProperties.Get<double>("LoginTime"); //LoginTime
        var lastLoginTime = myDataProperties.Get<double>("LastLoginTime"); //LastLoginTime
        var numOfDeaths = myDataProperties.Get<float>("NumOfDeaths"); //NumOfDeaths
        var spawnDayNumber = myDataProperties.Get<int>("SpawnDayNumber"); //SpawnDayNumber
        var spawnDayTime = myDataProperties.Get<float>("SpawnDayTime"); //SpawnDayTime

        List<string> heardVoices = new List<string>();
        var voiceOvers = myDataProperties.Get<ArrayProperty>("HeardVoiceOvers")?.Value;
        if (voiceOvers != null)
        {
            foreach(FName voiceHeard in voiceOvers)
            {
                heardVoices.Add(voiceHeard.Name);
            }
        }
        //HeardVoiceOvers


        float experiencePoints = 0;
        int totalEngramPoints = 0;
        float headHairGrowth = 0;
        float facialHairGrowth = 0;
        int totalSkillPoints = 0;
        int freeSkillPoints = 0;
        byte[] playerStats = new byte[12];
        List<string> learnedEngrams = new List<string>();
        string[] equippedItems = new string[10];
        List<int> unlockedExplorerNotes = new List<int>();
        List<string> namedExplorerNotesFound = new List<string>();
        Dictionary<string, byte> unlockedSkills = new Dictionary<string, byte>(); //skill,rank
        Dictionary<string, float> currentMilestones = new Dictionary<string, float>(); //milestone,progress
        List<string> completedMilestones = new List<string>();
        List<string> unlockedEmotes = new List<string>();

        var persistentConfigProperties = myDataProperties.Get<StructProperty>("MyPersistentCharacterStats")?.Value as List<Property>;
        if (persistentConfigProperties != null)
        {
            experiencePoints = persistentConfigProperties.Get<float>("CharacterStatusComponent_ExperiencePoints");
            totalEngramPoints = persistentConfigProperties.Get<int>("PlayerState_TotalEngramPoints");

            headHairGrowth = persistentConfigProperties.Get<float>("PercentageOfHeadHairGrowth");
            facialHairGrowth = persistentConfigProperties.Get<float>("PercentageOfFacialHairGrowth");

            totalSkillPoints = persistentConfigProperties.Get<int>("PlayerState_TotaSkillPoints");
            freeSkillPoints = persistentConfigProperties.Get<int>("PlayerState_FreeSkillPoints");
           
            var levelsApplied = 0;
            for (int i = 0; i < playerStats.Length; i++)
            {
                playerStats[i] = persistentConfigProperties.Get<byte>("CharacterStatusComponent_NumberOfLevelUpPointsApplied", i);
                levelsApplied += playerStats[i];
            }

            var engramsUnlocked = persistentConfigProperties.Get<ArrayProperty>("PlayerState_EngramBlueprints")?.Value;
            if (engramsUnlocked != null)
            {
                foreach (ObjectReference engram in engramsUnlocked)
                {
                    learnedEngrams.Add(engram.Value);
                }
            }
            
            for (int i = 0; i < equippedItems.Length; i++)
            {
                var equippedItem = persistentConfigProperties.Get<ObjectProperty>("PlayerState_DefaultItemSlotClasses", i)?.Value?.ToString();
                equippedItems[i] = equippedItem != null ? equippedItem : string.Empty;
            }
            
            if (persistentConfigProperties.HasAny("PerMapExplorerNoteUnlocks"))
            {
                var mapExplorerNotesArray = persistentConfigProperties.Get<ArrayProperty>("PerMapExplorerNoteUnlocks")?.Value;
                if (mapExplorerNotesArray?.Count > 0)
                {
                    for (int i = 0; i < mapExplorerNotesArray.Count; i++)
                    {
                        var noteBytes = BitConverter.GetBytes((uint)mapExplorerNotesArray[i]);
                        BitArray b = new BitArray(noteBytes);
                        for (int x = 0; x < b.Length; x++)
                        {
                            int noteIndex = (i * 32) + x;
                            if ((b[x]))
                            {
                                unlockedExplorerNotes.Add(noteIndex);
                            }
                        }
                    }
                }
            }          

            var namedExplorerNotesUnlocked = persistentConfigProperties.Get<ArrayProperty>("PerMapNamedExplorerNoteUnlocks")?.Value;
            if (namedExplorerNotesUnlocked != null)
            {
                foreach (FName namedNote in namedExplorerNotesUnlocked)
                {
                    namedExplorerNotesFound.Add(namedNote.Name);
                }
            }

            var skillUnlocksArray = persistentConfigProperties.Get<ArrayProperty>("SkillUnlocks")?.Value;
            var skillRanksArray = persistentConfigProperties.Get<ArrayProperty>("SkillRanks")?.Value;


            if (skillUnlocksArray != null)
            {
                for (int x = 0; x < skillUnlocksArray.Count; x++)
                {
                    FName skillUnlock = (FName)skillUnlocksArray[x];
                    unlockedSkills.Add(skillUnlock.Name, (byte)skillRanksArray[x]);

                }
            }

            var currentMilestonesArray = persistentConfigProperties.Get<ArrayProperty>("CurrentMilestones")?.Value; //NameProperty
            var currentMilestonesProgressArray = persistentConfigProperties.Get<ArrayProperty>("MilestoneProgress")?.Value; //FloatProperty
            
            if (currentMilestonesArray != null)
            {
                for (int x = 0; x < currentMilestonesArray.Count; x++)
                {
                    FName milesStone = (FName)currentMilestonesArray[x];
                    currentMilestones.Add(milesStone.Name, (float)currentMilestonesProgressArray[x]);
                }
            }

            
            var milestonesCompleted = persistentConfigProperties.Get<ArrayProperty>("CompletedMilestones")?.Value;
            if (milestonesCompleted != null)
            {
                foreach (FName milestone in milestonesCompleted)
                {
                    completedMilestones.Add(milestone.Name);
                }
            }



            if (persistentConfigProperties.HasAny("EmoteUnlocks"))
            {
                var emoteUnlocks = persistentConfigProperties.Get<ArrayProperty>("EmoteUnlocks")?.Value;
                if (emoteUnlocks != null)
                {

                    foreach (FName emoteName in emoteUnlocks)
                    {
                        unlockedEmotes.Add(emoteName.Name);
                    }
                }
            }


        }

        Color[] bodyColors = new Color[3];
        string characterName = string.Empty;

        var persistentStatusProperties = myDataProperties.Get<StructProperty>("MyPlayerCharacterConfig")?.Value as List<Property>;
        if (persistentStatusProperties != null)
        {
            characterName = persistentStatusProperties.Get<string>("PlayerCharacterName");

            headHairGrowth = persistentStatusProperties.Get<float>("PercentOfFullHeadHairGrowth");

            for (int i = 0; i< bodyColors.Length; i++)
            {
                var bodyColorStruct = (FLinearColor?)persistentStatusProperties.Get<StructProperty>("BodyColors", i)?.Value;
                if (bodyColorStruct != null)
                {
                    var a = bodyColorStruct.Value.A > 1? 1: bodyColorStruct.Value.A;
                    var r = bodyColorStruct.Value.R > 1 ? 1 : bodyColorStruct.Value.R;
                    var g = bodyColorStruct.Value.G > 1 ? 1 : bodyColorStruct.Value.G;
                    var b = bodyColorStruct.Value.B > 1 ? 1 : bodyColorStruct.Value.B;

                    bodyColors[i] = Color.FromArgb(
                        (int)(a * 255),
                        (int)(r * 255),
                        (int)(g * 255),
                        (int)(b * 255));
                }
                else
                {
                    bodyColors[i] = Color.Empty;
                }
            }


        }

        var playerLevel = playerStats.Sum(s => s) + 1; //Level 1 at 0 applied stats, +1 for current level

        return new Player
        {
            PlayerDataId = (long)linkedPlayerDataId,
            PlayerName = playerName,
            CharacterName = characterName,
            Level = playerLevel,
            TribeId = (int)linkedPlayerDataId,
            BodyColors = bodyColors,
            CompletedMilestones = completedMilestones,
            CurrentMilestones = currentMilestones,
            FreeSkillPoints = freeSkillPoints,
            EngramsLearned = learnedEngrams,
            EmotesUnlocked = unlockedEmotes,
            EquippedItems = equippedItems,
            ExperiencePoints = experiencePoints,
            ExplorerNotesFound = unlockedExplorerNotes,
            NamedExplorerNotesFound = namedExplorerNotesFound,
            HeardVoiceOvers = heardVoices,
            LastLoginGameTime = lastLoginTime,
            LoginGameTime = loginTime,
            NumOfDeaths = (int)numOfDeaths,
            PercentageOfFacialHairGrowth = facialHairGrowth,
            PercentageOfHeadHairGrowth = headHairGrowth,
            SaveNetworkAddress = savedNetAddress,
            SpawnDayNumber = spawnDayNumber,
            SpawnDayTime = spawnDayTime,
            Stats = playerStats,
            TotalEngramPoints = totalEngramPoints,
            TotalSkillPoints = totalSkillPoints,
            UniqueNetId = uniqueNetId,
            Skills = unlockedSkills,
            Location = transform?.Location,
            Rotation = transform?.Rotation
        };

    }

    internal void IngestStatusRecord(GameObjectRecord statusComponent)
    {
        //in-game status is often newer than the last profile save.
        var properties = statusComponent.Properties;

        var experiencePoints = properties.Get<float>("ExperiencePoints");
        ExperiencePoints = experiencePoints;

        if (properties.HasAny("EmoteUnlocks"))
        {

        }
        var playerLevel = 1;
        byte[] currentStats = new byte[12];
        for (int i = 0; i < 12; i++)
        {
            currentStats[i] = properties.Get<byte>($"NumberOfLevelUpPointsApplied", i);
            playerLevel+= currentStats[i];
        }
        Stats = currentStats;
        Level = playerLevel;


        float[] currentStatusValues = new float[12];
        for (int i = 0; i < currentStatusValues.Length; i++)
        {
            currentStatusValues[i] = properties.Get<float>("CurrentStatusValues", i);
        }
        StatsCurrent = currentStatusValues; 
    }

    internal void IngestCharacterRecord(GameObjectRecord characterComponent)
    {
        var properties = characterComponent.Properties;
        if (properties.HasAny("EmoteUnlocks"))
        {

        }
        var playeName = properties.Get<string>("PlayerName");
        CharacterName = playeName;

        var platformProfileName = properties.Get<string>("PlatformProfileName");
        PlayerName = platformProfileName;

        if (characterComponent.GetClassName().Contains("Female"))
        {
            Gender = "Female";
        }

        //PlatformProfileID
        var platformProfileIdProperty = (FUniqueNetIdRepl)properties.Get<StructProperty>("PlatformProfileID")?.Value;
        var platformProfileId = Convert.ToHexString(platformProfileIdProperty.Id);
        UniqueNetId = platformProfileId;

        //BodyColors
        Color[] bodyColors = new Color[3];
        for (int i = 0; i < bodyColors.Length; i++)
        {
            var bodyColorStruct = (FLinearColor?)properties.Get<StructProperty>("BodyColors", i)?.Value;
            if (bodyColorStruct != null)
            {
                var a = bodyColorStruct.Value.A > 1 ? 1 : bodyColorStruct.Value.A;
                var r = bodyColorStruct.Value.R > 1 ? 1 : bodyColorStruct.Value.R;
                var g = bodyColorStruct.Value.G > 1 ? 1 : bodyColorStruct.Value.G;
                var b = bodyColorStruct.Value.B > 1 ? 1 : bodyColorStruct.Value.B;


                bodyColors[i] = Color.FromArgb(
                    (int)(a * 255),
                    (int)(r * 255),
                    (int)(g * 255),
                    (int)(b * 255));
            }
            else
            {
                bodyColors[i] = Color.Empty;
            }
        }
        BodyColors = bodyColors;


        var savedLastTimeHadController = properties.Get<double>("SavedLastTimeHadController");
        LastLoginGameTime = savedLastTimeHadController;

        var linkedPlayerDataId = properties.Get<ulong>("LinkedPlayerDataID");
        PlayerDataId = (long)linkedPlayerDataId;

        var percentOfFullHeadHairGrowth = properties.Get<float>("PercentOfFullHeadHairGrowth");
        PercentageOfHeadHairGrowth = percentOfFullHeadHairGrowth;

        var tribeName = properties.Get<string>("TribeName");
        TribeName = tribeName;

        var targetingTeam = properties.Get<int>("TargetingTeam");
        TribeId = targetingTeam;

        var originalCreationTime = properties.Get<double>("OriginalCreationTime");
        OriginalCreationGameTime = originalCreationTime;


    }

    internal void IngestInventory(Inventory inventory)
    {
        Inventory = inventory;
    }

    internal void RefreshTimestamps(DateTime saveTimestamp, double gameTime)
    {
        if (OriginalCreationGameTime != null)
        {
            OriginalCreationTimestamp = saveTimestamp.AddSeconds(OriginalCreationGameTime.Value - gameTime);
        }
        if (LoginGameTime != null)
        {
            LoginTimestamp = saveTimestamp.AddSeconds(LoginGameTime.Value - gameTime);
        }
        if (LastLoginGameTime != null)
        {
            LastLoginTimestamp = saveTimestamp.AddSeconds(LastLoginGameTime.Value - gameTime);
        }
    }

    internal void UpdateGPSLocation(MapDefinition? map)
    {
        if (map == null) return;
        if (Location == null) return; //no location to calcaulte gps co-ords

        GPSLocation = new FVector2D(map.GetLongitude(Location.Value.X), map.GetLatitude(Location.Value.Y));
    }
}
