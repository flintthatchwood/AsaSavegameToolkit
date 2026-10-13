using System.Collections;
using System.Drawing;
using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

namespace AsaSavegameToolkit.Porcelain;

/// <summary>
/// Represents a player character in an ARK save file.
/// Wraps a <see cref="GameObjectRecord"/> and exposes typed accessors for common player properties.
/// </summary>
/// <remarks>
/// Full player data (engrams, stats, ascensions) lives in .arkprofile files.
/// This class covers the in-world pawn record from the main .ark save and standalone/embedded profiles.
/// </remarks>
public class Player
{
    /// <summary>
    /// Unique object ID of this player.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The blueprint class name (e.g., "PlayerPawnTest_Male_C").
    /// </summary>
    public string ClassName { get; set; } = "PlayerPawnTest_Male_C";

    /// <summary>
    /// The player's account/platform name.
    /// </summary>
    public string? PlayerName { get; set; }

    /// <summary>
    /// The character's in-game name.
    /// </summary>
    public string? CharacterName { get; set; }

    public long Level { get; set; } = 1;

    /// <summary>
    /// The tribe ID this player belongs to. 0 if not in a tribe.
    /// </summary>
    public int TribeId { get; set; }

    public string? TribeName { get; set; }

    /// <summary>
    /// The platform-specific player data ID (Steam ID, etc.).
    /// </summary>
    public long? PlayerDataId { get; set; }

    public string? UniqueNetId { get; set; }
    public string? SaveNetworkAddress { get; set; }

    public int NumOfDeaths { get; set; }
    public int SpawnDayNumber { get; set; }
    public float SpawnDayTime { get; set; }
    public List<string> HeardVoiceOvers { get; set; } = [];
    public float ExperiencePoints { get; set; }
    public int TotalEngramPoints { get; set; }
    public float PercentageOfHeadHairGrowth { get; set; }
    public float PercentageOfFacialHairGrowth { get; set; }
    public int TotalSkillPoints { get; set; }
    public int FreeSkillPoints { get; set; }
    public byte[] Stats { get; set; } = new byte[12];
    public float[] StatsCurrent { get; set; } = new float[12];
    public List<string> EngramsLearned { get; set; } = [];
    public string[] EquippedItems { get; set; } = new string[10];
    public List<int> ExplorerNotesFound { get; set; } = [];
    public List<string> NamedExplorerNotesFound { get; set; } = [];
    public List<string> EmotesUnlocked { get; set; } = [];
    public Dictionary<string, byte> Skills { get; set; } = [];
    public Dictionary<string, float> CurrentMilestones { get; set; } = [];
    public List<string> CompletedMilestones { get; set; } = [];
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
    public FVector? Location { get; set; }

    /// <summary>
    /// Gets the rotation represented by a quaternion, if available.
    /// </summary>
    public FQuat? Rotation { get; set; }

    /// <summary>
    /// The underlying raw record for direct property access.
    /// </summary>
    public GameObjectRecord? Record { get; set; }

    public override string ToString() => CharacterName ?? PlayerName ?? ClassName;

    /// <summary>
    /// Creates a new Player instance from a record (pawn or profile).
    /// </summary>
    public static Player Create(GameObjectRecord record, ActorTransform? transform)
    {
        if (record.Properties.Get<StructProperty>("MyData")?.Value is List<Property> myData)
        {
            return CreateFromProfileData(record, myData, transform);
        }

        var player = new Player
        {
            Id = record.Uuid,
            ClassName = record.GetClassName(),
            PlayerName = record.Properties.Get<string>("PlayerName"),
            CharacterName = record.Properties.Get<string>("PlayerName"),
            TribeId = record.Properties.Get<int>("TribeID"),
            TribeName = record.Properties.Get<string>("TribeName"),
            PlayerDataId = record.Properties.TryGet<long>("PlayerDataID", out var value) ? value :
                           record.Properties.TryGet<ulong>("LinkedPlayerDataID", out var uval) ? (long)uval : null,
            Location = transform?.Location,
            Rotation = transform?.Rotation,
            Record = record
        };

        if (record.GetClassName().Contains("Female"))
        {
            player.Gender = "Female";
        }

        var statusComponent = record.GetCharacterStatusComponent();
        if (statusComponent != null)
        {
            player.IngestStatusRecord(statusComponent);
        }

        return player;
    }

    private static Player CreateFromProfileData(GameObjectRecord profileRecord, List<Property> myDataProperties, ActorTransform? transform)
    {
        var linkedPlayerDataId = myDataProperties.Get<ulong>("PlayerDataID");
        var playerName = myDataProperties.Get<string>("PlayerName");
        var tribeId = myDataProperties.Get<int>("TribeID");
        var savedNetAddress = myDataProperties.Get<string>("SavedNetworkAddress");
        var loginTime = myDataProperties.Get<double>("LoginTime");
        var lastLoginTime = myDataProperties.Get<double>("LastLoginTime");
        var numOfDeaths = myDataProperties.Get<float>("NumOfDeaths");
        var spawnDayNumber = myDataProperties.Get<int>("SpawnDayNumber");
        var spawnDayTime = myDataProperties.Get<float>("SpawnDayTime");

        string? uniqueNetId = null;
        if (myDataProperties.Get<StructProperty>("UniqueID")?.Value is FUniqueNetIdRepl uniqueNetIdProperty &&
            uniqueNetIdProperty.Id != null)
        {
            uniqueNetId = Convert.ToHexString(uniqueNetIdProperty.Id);
        }

        List<string> heardVoices = [];
        var voiceOvers = myDataProperties.Get<ArrayProperty>("HeardVoiceOvers")?.Value;
        if (voiceOvers != null)
        {
            foreach (FName voiceHeard in voiceOvers)
            {
                heardVoices.Add(voiceHeard.Name);
            }
        }

        float experiencePoints = 0;
        int totalEngramPoints = 0;
        float headHairGrowth = 0;
        float facialHairGrowth = 0;
        int totalSkillPoints = 0;
        int freeSkillPoints = 0;
        byte[] playerStats = new byte[12];
        List<string> learnedEngrams = [];
        string[] equippedItems = new string[10];
        List<int> unlockedExplorerNotes = [];
        List<string> namedExplorerNotesFound = [];
        Dictionary<string, byte> unlockedSkills = [];
        Dictionary<string, float> currentMilestones = [];
        List<string> completedMilestones = [];
        List<string> unlockedEmotes = [];

        if (myDataProperties.Get<StructProperty>("MyPersistentCharacterStats")?.Value is List<Property> persistentConfigProperties)
        {
            experiencePoints = persistentConfigProperties.Get<float>("CharacterStatusComponent_ExperiencePoints");
            totalEngramPoints = persistentConfigProperties.Get<int>("PlayerState_TotalEngramPoints");
            headHairGrowth = persistentConfigProperties.Get<float>("PercentageOfHeadHairGrowth");
            facialHairGrowth = persistentConfigProperties.Get<float>("PercentageOfFacialHairGrowth");
            totalSkillPoints = persistentConfigProperties.Get<int>("PlayerState_TotaSkillPoints");
            freeSkillPoints = persistentConfigProperties.Get<int>("PlayerState_FreeSkillPoints");

            for (var i = 0; i < playerStats.Length; i++)
            {
                playerStats[i] = persistentConfigProperties.Get<byte>("CharacterStatusComponent_NumberOfLevelUpPointsApplied", i);
            }

            var engramsUnlocked = persistentConfigProperties.Get<ArrayProperty>("PlayerState_EngramBlueprints")?.Value;
            if (engramsUnlocked != null)
            {
                foreach (ObjectReference engram in engramsUnlocked)
                {
                    learnedEngrams.Add(engram.Value);
                }
            }

            for (var i = 0; i < equippedItems.Length; i++)
            {
                var equippedItem = persistentConfigProperties.Get<ObjectProperty>("PlayerState_DefaultItemSlotClasses", i)?.Value?.ToString();
                equippedItems[i] = equippedItem ?? string.Empty;
            }

            if (persistentConfigProperties.HasAny("PerMapExplorerNoteUnlocks"))
            {
                var mapExplorerNotesArray = persistentConfigProperties.Get<ArrayProperty>("PerMapExplorerNoteUnlocks")?.Value;
                if (mapExplorerNotesArray?.Count > 0)
                {
                    for (var i = 0; i < mapExplorerNotesArray.Count; i++)
                    {
                        var noteBytes = BitConverter.GetBytes(Convert.ToUInt32(mapExplorerNotesArray[i]));
                        var b = new BitArray(noteBytes);
                        for (var x = 0; x < b.Length; x++)
                        {
                            var noteIndex = (i * 32) + x;
                            if (b[x])
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
            if (skillUnlocksArray != null && skillRanksArray != null)
            {
                for (var x = 0; x < Math.Min(skillUnlocksArray.Count, skillRanksArray.Count); x++)
                {
                    if (skillUnlocksArray[x] is FName skillUnlock && skillRanksArray[x] != null)
                    {
                        unlockedSkills[skillUnlock.Name] = Convert.ToByte(skillRanksArray[x]);
                    }
                }
            }

            var currentMilestonesArray = persistentConfigProperties.Get<ArrayProperty>("CurrentMilestones")?.Value;
            var currentMilestonesProgressArray = persistentConfigProperties.Get<ArrayProperty>("MilestoneProgress")?.Value;
            if (currentMilestonesArray != null && currentMilestonesProgressArray != null)
            {
                for (var x = 0; x < Math.Min(currentMilestonesArray.Count, currentMilestonesProgressArray.Count); x++)
                {
                    if (currentMilestonesArray[x] is FName milesStone && currentMilestonesProgressArray[x] != null)
                    {
                        currentMilestones[milesStone.Name] = Convert.ToSingle(currentMilestonesProgressArray[x]);
                    }
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

            var emoteUnlocks = persistentConfigProperties.Get<ArrayProperty>("EmoteUnlocks")?.Value;
            if (emoteUnlocks != null)
            {
                foreach (FName emoteName in emoteUnlocks)
                {
                    unlockedEmotes.Add(emoteName.Name);
                }
            }
        }

        Color[] bodyColors = new Color[3];
        string characterName = string.Empty;

        if (myDataProperties.Get<StructProperty>("MyPlayerCharacterConfig")?.Value is List<Property> persistentStatusProperties)
        {
            characterName = persistentStatusProperties.Get<string>("PlayerCharacterName") ?? string.Empty;
            headHairGrowth = persistentStatusProperties.Get<float>("PercentOfFullHeadHairGrowth");

            for (var i = 0; i < bodyColors.Length; i++)
            {
                if (persistentStatusProperties.Get<StructProperty>("BodyColors", i)?.Value is FLinearColor bodyColorStruct)
                {
                    var a = Math.Clamp(bodyColorStruct.A, 0f, 1f);
                    var r = Math.Clamp(bodyColorStruct.R, 0f, 1f);
                    var g = Math.Clamp(bodyColorStruct.G, 0f, 1f);
                    var b = Math.Clamp(bodyColorStruct.B, 0f, 1f);

                    bodyColors[i] = Color.FromArgb((int)(a * 255), (int)(r * 255), (int)(g * 255), (int)(b * 255));
                }
                else
                {
                    bodyColors[i] = Color.Empty;
                }
            }
        }

        var playerLevel = playerStats.Sum(s => (int)s) + 1;

        return new Player
        {
            Record = profileRecord,
            Id = profileRecord.Uuid,
            ClassName = profileRecord.GetClassName(),
            PlayerDataId = (long)linkedPlayerDataId,
            PlayerName = playerName,
            CharacterName = characterName,
            Level = playerLevel,
            TribeId = tribeId,
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
        var properties = statusComponent.Properties;

        if (properties.TryGet<float>("ExperiencePoints", out var exp))
            ExperiencePoints = exp;

        var playerLevel = 1;
        var currentStats = new byte[12];
        for (var i = 0; i < 12; i++)
        {
            currentStats[i] = properties.Get<byte>("NumberOfLevelUpPointsApplied", i);
            playerLevel += currentStats[i];
        }
        Stats = currentStats;
        Level = playerLevel;

        var currentStatusValues = new float[12];
        for (var i = 0; i < 12; i++)
        {
            currentStatusValues[i] = properties.Get<float>("CurrentStatusValues", i);
        }
        StatsCurrent = currentStatusValues;
    }

    internal void IngestCharacterRecord(GameObjectRecord characterComponent)
    {
        var properties = characterComponent.Properties;

        var charName = properties.Get<string>("PlayerName");
        if (!string.IsNullOrEmpty(charName))
            CharacterName = charName;

        var platformProfileName = properties.Get<string>("PlatformProfileName");
        if (!string.IsNullOrEmpty(platformProfileName))
            PlayerName = platformProfileName;

        if (characterComponent.GetClassName().Contains("Female"))
        {
            Gender = "Female";
        }

        if (properties.Get<StructProperty>("PlatformProfileID")?.Value is FUniqueNetIdRepl netId && netId.Id != null)
        {
            UniqueNetId = Convert.ToHexString(netId.Id);
        }

        for (var i = 0; i < BodyColors.Length; i++)
        {
            if (properties.Get<StructProperty>("BodyColors", i)?.Value is FLinearColor col)
            {
                var a = Math.Clamp(col.A, 0f, 1f);
                var r = Math.Clamp(col.R, 0f, 1f);
                var g = Math.Clamp(col.G, 0f, 1f);
                var b = Math.Clamp(col.B, 0f, 1f);
                BodyColors[i] = Color.FromArgb((int)(a * 255), (int)(r * 255), (int)(g * 255), (int)(b * 255));
            }
        }

        if (properties.TryGet<double>("SavedLastTimeHadController", out var lastController))
            LastLoginGameTime = lastController;

        if (properties.TryGet<long>("LinkedPlayerDataID", out var linkedId))
            PlayerDataId = linkedId;
        else if (properties.TryGet<ulong>("LinkedPlayerDataID", out var linkedUId))
            PlayerDataId = (long)linkedUId;

        if (properties.TryGet<float>("PercentOfFullHeadHairGrowth", out var hairGrowth))
            PercentageOfHeadHairGrowth = hairGrowth;

        var tribeName = properties.Get<string>("TribeName");
        if (!string.IsNullOrEmpty(tribeName))
            TribeName = tribeName;

        var targetingTeam = properties.Get<int>("TargetingTeam");
        if (targetingTeam != 0)
            TribeId = targetingTeam;

        if (properties.TryGet<double>("OriginalCreationTime", out var origTime))
            OriginalCreationGameTime = origTime;
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

}
