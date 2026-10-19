using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using AsaSavegameToolkit.Porcelain;
using AsaSavegameToolkit.Tests.Helpers;

namespace AsaSavegameToolkit.Tests.Porcelain;

[TestClass]
public class PlayerTests : SaveTests
{
    [TestMethod]
    public void CanParseAsPlayer()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP.ark");
        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        var records = reader.ReadGameRecords(TestContext.CancellationToken);

        var profileRecords = records.Where(r => r.Value.IsProfile()).ToList();
        Assert.IsNotNull(profileRecords);
        Assert.IsNotEmpty(profileRecords);

        var firstProfile = profileRecords[0].Value;
        var player = Player.Create(firstProfile, null);
        Assert.IsNotNull(player);
        Assert.IsNotNull(player.PlayerName);
        Assert.IsGreaterThanOrEqualTo(player.Level, 1);
        Assert.IsNotNull(player.Stats);
        Assert.HasCount(12, player.Stats);
    }

    [TestMethod]
    public void ReadFrom_PopulatesTribesAndPlayers()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/TheIsland_WP.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);

        Assert.IsNotNull(saveGame.Players);
        Assert.IsNotNull(saveGame.Tribes);
        Assert.IsNotNull(saveGame.SaveFilename);
        Assert.IsTrue(saveGame.SaveTimestamp <= DateTime.UtcNow);
    }

    [TestMethod]
    public void ReadFrom_GameModeCustomBytes_PopulatesTribes()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_14/LostColony_WP_GameModCustomBytes.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);

        Assert.IsNotNull(saveGame.Tribes);
        Assert.IsNotEmpty(saveGame.Tribes);

        var tribe = saveGame.Tribes.Values.First();
        Assert.IsNotNull(tribe.TribeName);
        Assert.IsNotNull(tribe.MemberNames);
        Assert.IsNotNull(tribe.MemberIds);
    }

    [TestMethod]
    public void Version16_GenesisProfile_ReadsHexagonsMissionScoresAndBuffReferences()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_16/Genesis_WP.ark");
        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        var records = reader.ReadGameRecords(TestContext.CancellationToken);
        var profiles = records.Values.Where(r => r.IsProfile()).Select(r => Player.Create(r, null)).ToArray();
        Assert.HasCount(2, profiles);
        var latestPlayer = profiles.OrderByDescending(p => p.LoginGameTime).First();
        var profile = latestPlayer.Record;
        Assert.IsNotNull(profile);
        var pawn = records.Values.Single(r => r.Properties.Any(p => p.Tag.Name.FullName == "PlayerHexagonCount"));

        Assert.AreEqual(14570, profile.Properties.Get<int>("HexagonCount"));
        Assert.AreEqual(14570, pawn.Properties.Get<int>("PlayerHexagonCount"));
        Assert.AreEqual(14570, latestPlayer.Hexagons);
        Assert.AreEqual(14570, Player.Create(pawn, null).Hexagons);
        CollectionAssert.AreEquivalent(new[] { 890, 14570 }, profiles.Select(p => p.Hexagons).ToArray());

        var buffs = profile.Properties.Get<ArrayProperty>("MyPersistentBuffDatas");
        Assert.IsNotNull(buffs);
        CollectionAssert.AreEqual(new[] { 1, 2 }, buffs.Value.Cast<ObjectReference>().Select(r => r.ObjectIndex).ToArray());

        var myData = profile.Properties.Get<StructProperty>("MyData")?.Value as List<Property>;
        Assert.IsNotNull(myData);
        Assert.IsFalse(myData.HasAny("HexagonCount"));
        var scores = myData.Get<ArrayProperty>("LatestMissionScores");
        Assert.IsNotNull(scores);
        // Latest/best scores are not the complete mission-completion ledger.
        Assert.HasCount(2, scores.Value);
        var missions = scores.Value.Cast<List<Property>>().ToDictionary(m => m.Get<FName>("MissionTag").FullName);
        Assert.HasCount(2, latestPlayer.Genesis1MissionScores);
        foreach (var (missionTag, expectedIntValue, expectedFloatValue) in new[]
        {
            ("/Game/Genesis/Missions/Race/MissionType_Race_Dolphin_Easy.MissionType_Race_Dolphin_Easy_C", 1, 161.25484f),
            ("/Game/Genesis/Missions/GauntletWaves/MissionType_GauntletWaves_Bog_Medium.MissionType_GauntletWaves_Bog_Medium_C", 0, 755014.2f)
        })
        {
            var mission = missions[missionTag];
            var bestScore = latestPlayer.Genesis1MissionScores[missionTag];
            Assert.AreEqual(expectedIntValue, bestScore.IntValue);
            Assert.AreEqual(expectedFloatValue, bestScore.FloatValue);
            Assert.IsTrue(mission.Get<bool>("bHasValidScore"));
            foreach (var scoreName in new[] { "LatestScore", "BestScore" })
            {
                var score = mission.Get<StructProperty>(scoreName)?.Value as List<Property>;
                Assert.IsNotNull(score);
                Assert.AreEqual(missionTag, score.Get<FName>("MissionTag").FullName);
                Assert.IsGreaterThan(0, score.Get<float>("FloatValue"));
                Assert.AreEqual(expectedIntValue, score.Get<int>("IntValue"));
            }
        }
    }

    [TestMethod]
    public void Version16_Genesis_ReadFrom_PopulatesHexagonsAndMissionScores()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_16/Genesis_WP.ark");
        var saveGame = AsaSaveGame.ReadFrom(saveFile, TestContext.GetLogger(), cancellationToken: TestContext.CancellationToken);
        var players = saveGame.Players.Values.Where(p => p.PlayerDataId == 872165393L).ToArray();

        Assert.IsNotEmpty(players);
        foreach (var player in players)
            Assert.AreEqual(14570, player.Hexagons);

        var latestPlayer = players.OrderByDescending(p => p.LoginGameTime).First();
        Assert.HasCount(2, latestPlayer.Genesis1MissionScores);
        Assert.AreEqual(new MissionScore(1, 161.25484f),
            latestPlayer.Genesis1MissionScores["/Game/Genesis/Missions/Race/MissionType_Race_Dolphin_Easy.MissionType_Race_Dolphin_Easy_C"]);
    }

    [TestMethod]
    [DataRow(true, null, 0)]
    [DataRow(true, 0, 0)]
    [DataRow(true, 890, 890)]
    [DataRow(false, null, 0)]
    [DataRow(false, 0, 0)]
    [DataRow(false, 14570, 14570)]
    public void Hexagons_Create_ReadsProfileOrPawnBalance(bool isProfile, int? balance, int expected)
    {
        var player = Player.Create(CreatePlayerRecord(isProfile, balance), null);

        Assert.AreEqual(expected, player.Hexagons);
    }

    [TestMethod]
    [DataRow(null, 890)]
    [DataRow(0, 0)]
    [DataRow(14570, 14570)]
    public void Hexagons_IngestCharacterRecord_OnlyReplacesAnExplicitBalance(int? balance, int expected)
    {
        var player = new Player { Hexagons = 890 };

        player.IngestCharacterRecord(CreatePlayerRecord(false, balance));

        Assert.AreEqual(expected, player.Hexagons);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Genesis1MissionScores_AbsentScores_AreEmpty(bool isProfile)
    {
        var player = Player.Create(CreatePlayerRecord(isProfile), null);

        Assert.IsEmpty(player.Genesis1MissionScores);
    }

    [TestMethod]
    public void Genesis1MissionScores_PreservesBothBestValuesWithoutUsingLatestScores()
    {
        var profile = CreateMissionScoreProfile(
            MissionScoreEntry("Race", 1, 161.25484f),
            MissionScoreEntry("Points", 500, 12.5f),
            MissionScoreEntry("Zero", 0, 0f),
            MissionScoreEntry("Invalid", 42, 42f, false));

        var player = Player.Create(profile, null);

        Assert.HasCount(3, player.Genesis1MissionScores);
        Assert.AreEqual(new MissionScore(1, 161.25484f), player.Genesis1MissionScores["Race"]);
        Assert.AreEqual(new MissionScore(500, 12.5f), player.Genesis1MissionScores["Points"]);
        Assert.AreEqual(new MissionScore(0, 0f), player.Genesis1MissionScores["Zero"]);
    }

    [TestMethod]
    public void Genesis1MissionScores_EmptyArray_IsEmpty()
    {
        var player = Player.Create(CreateMissionScoreProfile(), null);

        Assert.IsEmpty(player.Genesis1MissionScores);
    }

    [TestMethod]
    [DataRow("IntValue", 0, 12.5f)]
    [DataRow("FloatValue", 500, 0f)]
    public void Genesis1MissionScores_OmittedNumericFields_DefaultToZero(string propertyName, int expectedInt, float expectedFloat)
    {
        var entry = MissionScoreEntry("Points", 500, 12.5f);
        var bestScore = Assert.IsInstanceOfType<List<Property>>(entry.Get<StructProperty>("BestScore")?.Value);
        bestScore.RemoveAll(p => p.Tag.Name.FullName == propertyName);

        var player = Player.Create(CreateMissionScoreProfile(entry), null);

        Assert.AreEqual(new MissionScore(expectedInt, expectedFloat), player.Genesis1MissionScores["Points"]);
    }

    [TestMethod]
    [DataRow("BestScore")]
    [DataRow("MissionTag")]
    public void Genesis1MissionScores_ValidEntryMissingRequiredData_Throws(string propertyName)
    {
        var entry = MissionScoreEntry("Race", 1, 161.25484f);
        entry.RemoveAll(p => p.Tag.Name.FullName == propertyName);

        Assert.Throws<InvalidDataException>(() => Player.Create(CreateMissionScoreProfile(entry), null));
    }

    [TestMethod]
    public void Genesis1MissionScores_MalformedEntry_Throws()
    {
        Assert.Throws<InvalidDataException>(() => Player.Create(CreateMissionScoreProfile("not a property list"), null));
    }

    [TestMethod]
    public void Version16_LostColonyProfile_ReadsSkillsAndMilestones()
    {
        var saveFile = Path.Combine(TestSettings.AssetsDirectory, "version_16/LostColony_WP.ark");
        using var reader = new AsaSaveReader(saveFile, TestContext.GetLogger());
        var embedded = reader.ReadGameModeCustomBytes(TestContext.CancellationToken);
        // Both profiles have identical login times; entry 1 contains the played character's progression.
        var entry = embedded.Profiles.Single(p => p.EosId == 1);
        using var archive = new AsaArchive(TestContext.GetLogger(), entry.RawBlob, saveFile);
        var profile = ArkProfileRecord.Read(archive, Guid.NewGuid());
        var player = Player.Create(profile, null);

        Assert.AreEqual(405945907L, player.PlayerDataId);
        Assert.AreEqual(6, player.TotalSkillPoints);
        Assert.AreEqual(4, player.FreeSkillPoints);
        Assert.HasCount(2, player.Skills);
        Assert.AreEqual((byte)1, player.Skills["HotBlooded"]);
        Assert.AreEqual((byte)1, player.Skills["BondedTaming"]);

        Assert.HasCount(3, player.CurrentMilestones);
        Assert.AreEqual(0.23600012f, player.CurrentMilestones["Task.LC.HarvestRawMetalA"], 0.0000001f);
        Assert.AreEqual(0.12300548f, player.CurrentMilestones["Task.TravelFootB"], 0.0000001f);
        Assert.AreEqual(0.33682907f, player.CurrentMilestones["Task.TravelTotalA"], 0.0000001f);
        Assert.AreEqual(118f, player.CurrentMilestones["Task.LC.HarvestRawMetalA"] * 500f, 0.001f);
        Assert.AreEqual(337d, Math.Round(player.CurrentMilestones["Task.TravelTotalA"] * 1000f));
        CollectionAssert.AreEquivalent(new[]
        {
            "StartMilestone",
            "Task.HarvestStoneB",
            "Task.CraftStoneArrowsB",
            "Task.CraftThatchStructuresA",
            "Task.HarvestWoodA",
            "Task.HarvestThatchA",
            "Task.CraftWoodStructuresA"
        }, player.CompletedMilestones);

        var myData = profile.Properties.Get<StructProperty>("MyData")?.Value as List<Property>;
        Assert.IsNotNull(myData);
        var stats = myData.Get<StructProperty>("MyPersistentCharacterStats")?.Value as List<Property>;
        Assert.IsNotNull(stats);
        var categories = stats.Get<ArrayProperty>("MilestoneLevelsAndIndexes");
        Assert.IsNotNull(categories);
        var categoryPositions = categories.Value.Cast<List<Property>>()
            .ToDictionary(c => c.Get<FName>("Name").FullName,
                c => Assert.IsInstanceOfType<FIntPoint>(c.Get<StructProperty>("LevelAndIndex")?.Value));
        Assert.AreEqual(2, categoryPositions["Global"].X);
        Assert.AreEqual(15, categoryPositions["Global"].Y);
        Assert.AreEqual(0, categoryPositions["LostColony"].X);
        Assert.AreEqual(21, categoryPositions["LostColony"].Y);
    }

    [TestMethod]
    public void Item_ItemId_BitshiftsCorrectly()
    {
        uint id1 = 12345;
        uint id2 = 67890;
        long expected = ((long)id1 << 32) | id2;

        var ancestry = new Ancestry(0, expected, "Father", expected + 1, "Mother");
        Assert.AreEqual(0, ancestry.TreeDepth);
        Assert.AreEqual(expected, ancestry.MaleId);
        Assert.AreEqual("Father", ancestry.MaleName);
        Assert.AreEqual(expected + 1, ancestry.FemaleId);
        Assert.AreEqual("Mother", ancestry.FemaleName);
    }

    private static GameObjectRecord CreateMissionScoreProfile(params object[] entries)
    {
        var profile = CreatePlayerRecord(true);
        var data = Assert.IsInstanceOfType<List<Property>>(profile.Properties.Get<StructProperty>("MyData")?.Value);
        data.Add(new ArrayProperty { Tag = Tag("LatestMissionScores", "ArrayProperty"), Value = entries });
        return profile;
    }

    private static List<Property> MissionScoreEntry(string missionTag, int intValue, float floatValue, bool isValid = true) =>
    [
        new NameProperty { Tag = Tag("MissionTag", "NameProperty"), Value = new FName(-1, 0, missionTag) },
        new BoolProperty { Tag = Tag("bHasValidScore", "BoolProperty"), Value = isValid },
        new StructProperty
        {
            Tag = Tag("BestScore", "StructProperty"),
            Value = new List<Property>
            {
                new IntProperty { Tag = Tag("IntValue", "IntProperty"), Value = intValue },
                new FloatProperty { Tag = Tag("FloatValue", "FloatProperty"), Value = floatValue }
            }
        },
        new StructProperty
        {
            Tag = Tag("LatestScore", "StructProperty"),
            Value = new List<Property>
            {
                new IntProperty { Tag = Tag("IntValue", "IntProperty"), Value = 999 },
                new FloatProperty { Tag = Tag("FloatValue", "FloatProperty"), Value = 999f }
            }
        }
    ];

    private static GameObjectRecord CreatePlayerRecord(bool isProfile, int? balance = null)
    {
        List<Property> properties = [];
        if (isProfile)
            properties.Add(new StructProperty { Tag = Tag("MyData", "StructProperty"), Value = new List<Property>() });
        if (balance.HasValue)
            properties.Add(new IntProperty
            {
                Tag = Tag(isProfile ? "HexagonCount" : "PlayerHexagonCount", "IntProperty"),
                Value = balance.Value
            });

        return new GameObjectRecord(Guid.NewGuid(), new FName(-1, 0, isProfile ? "PrimalPlayerDataBP_C" : "PlayerPawnTest_Male_C"),
            ["Player"], properties, 0, ObjectTypeFlags.None, extraGuids: []);
    }

    private static PropertyTag Tag(string name, string type) => new()
    {
        Name = new FName(-1, 0, name),
        Type = FPropertyTypeName.Create(new FName(-1, 0, type))
    };
}
