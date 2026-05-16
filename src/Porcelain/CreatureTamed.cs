using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using System.ComponentModel.DataAnnotations;

namespace AsaSavegameToolkit.Porcelain
{
    public class CreatureTamed: Creature
    {

        // Tamed info


        public long FatherId { get; set; } = 0;
        public string FatherName { get; set; } = string.Empty;
        public long MotherId { get; set; } = 0;
        public string MotherName { get; set; } = string.Empty;
        public bool IsClaimed { get; set; } = true;

        /// <summary>
        /// The player-given tame name (e.g., "Fluffy"). Null if not tamed or unnamed.
        /// </summary>
        public string? TamedName { get; set; }


        /// <summary>
        /// The name of the tribe that owns this creature. Null if untamed.
        /// </summary>
        /// 

        public string? TribeName { get; set; }

        /// <summary>
        /// The player who last tamed this creature (player identifier string).
        /// </summary>
        public string? TamerString { get; set; }


        /// <summary>
        /// Imprint quality from 0.0 (no imprint) to 1.0 (100% imprint).
        /// </summary>
        public float ImprintQuality { get; set; }

        /// <summary>
        /// The player name who imprinted this creature.
        /// </summary>
        public string? ImprinterName { get; set; }

        public bool IsWandering { get; set; } = false;
        public bool IsMating { get; set; } = false;
        public bool IsClone { get; set; } = false;
        public float ExperiencePoints { get; set; } = 0;

        /// <summary>
        /// Server time (seconds since epoch) when this creature was tamed. Null if not tamed.
        /// </summary>
        internal double? TamedAtGameTime { get; set; }
        internal double? LastAllyInRangeGameTime { get; set; }
        internal double? OriginalCreationGameTime { get; set; }

        public DateTime? TamedTimestamp { get; set; } = null;
        public DateTime? LastAllyInRangeTimestamp { get; set; } = null;
        public DateTime? OriginalCreationTimestamp { get; set; } = null;


        public byte[] TamedStats { get; set; } = new byte[12];

        public byte[] TamedMutations { get; set; } = new byte[12];

        public int RandomMutationsMale { get; set; } = 0;
        public int RandomMutationsFemale { get; set; } = 0;

        public string TamedServer { get; set; } = string.Empty;
        public string UploadedServer { get; set; } = string.Empty;

        public double? UploadedTime { get; set; }

        public static new CreatureTamed Create(GameObjectRecord actor, ActorTransform? transform)
        {
            var properties = actor.Properties;

            //read base properties
            int targetingTeam = properties.Get<int>("TargetingTeam");
            var dinoId1 = properties.Get<uint>("DinoID1");
            var dinoId2 = properties.Get<uint>("DinoID2");
            var dinoId = long.Parse($"{dinoId1}{dinoId2}");

            bool isInCryo = properties.Get<bool>("IsStored");
            var colorRegions = new byte[6];
            for (int i = 0; i < colorRegions.Length; i++)
            {
                colorRegions[i] = properties.Get<byte>($"ColorSetIndices", i);
            }
            List<string> geneTraits = new List<string>();
            var geneTraitsArray = properties.Get<ArrayProperty>("GeneTraits");
            if (geneTraitsArray != null && geneTraitsArray.Value.Count > 0)
            {
                foreach (FName geneTraitValue in geneTraitsArray.Value)
                {
                    geneTraits.Add(geneTraitValue.ToString());
                }
            }
            bool isFemale = properties.Get<bool>("bIsFemale");
            bool isJuvenile = properties.Get<bool>("bBabyInitialized");
            bool isTameable = properties.Get<bool>("bForceDisablingTaming") == false;
            float babyAge = properties.Get<float>("BabyAge");

            var wildScale = properties.Get<float>("WildRandomScale");

            var tamedName = properties.Get<string>("TamedName")??"";
            var tamedTimestamp = properties.Get<string>("TamedTimeStamp");
            var imprintNetId = properties.Get<string>("ImprinterPlayerUniqueNetId");
            var imprinterName = properties.Get<string>("ImprinterName")??"";
            var uploadedFromServer = properties.Get<string>("UploadedFromServerName")??"";
            var tamedOnServer = properties.Get<string>("TamedOnServerName")??"";
            var imprintClassName = properties.Get<ObjectProperty>("BabyCuddleFood")?.Value?.Path;


            var originalCreationTime = properties.Get<double>("OriginalCreationTime");
            var lastTameConsumedFoodTime = properties.Get<double>("LastTameConsumedFoodTime");
            var lastAllyInRangeGameTime = properties.Get<double>("LastInAllyRangeSerialized");
            var tamedAtTime = properties.Get<double>("TamedAtTime");

            var tamedAggressionLevel = properties.Get<int>("TamedAggressionLevel");
            var isMating = properties.Get<bool>("bEnableTamedMating");
            var isWandering = properties.Get<bool>("bEnableTamedWandering");
            var tribeName = properties.Get<string>("TribeName")??"";
            var isNeutered = properties.Get<bool>("bNeutered");
            var isClone = properties.Get<bool>("bIsClone");

            List<Ancestry> femaleAncestry = new(); //depth, data
            var femaleAncestorsProperty = properties.Get<ArrayProperty>("DinoAncestors")?.Value;
            if (femaleAncestorsProperty != null)
            {
                for (int i = 0; i < femaleAncestorsProperty.Count; i++)
                {
                    List<Property> propertyList = femaleAncestorsProperty[i] as List<Property>;
                    var maleName = propertyList.Get<string>("MaleName") ?? "";
                    var maleId1 = propertyList.Get<uint>("MaleDinoID1");
                    var maleId2 = propertyList.Get<uint>("MaleDinoID2");
                    long maleId = maleId1 + maleId2; //TODO:// verify if its just concatenation of strings same as the normal creature data or if it needs bit masking


                    var femaleName = propertyList.Get<string>("FemaleName") ?? "";
                    var femaleId1 = propertyList.Get<uint>("FemaleDinoID1");
                    var femaleId2 = propertyList.Get<uint>("FemaleDinoID2");
                    long femaleId = femaleId1 + maleId2;//TODO:// verify if its just concatenation of strings same as the normal creature data or if it needs bit masking

                    femaleAncestry.Add(new Ancestry(i, maleId, maleName, femaleId, femaleName));

                }

            }

            List<Ancestry> maleAncestry = new();
            var maleAncestorsProperty = properties.Get<ArrayProperty>("DinoAncestorsMale")?.Value;
            if (maleAncestorsProperty != null)
            {
                for (int i = 0; i < maleAncestorsProperty.Count; i++)
                {
                    List<Property> propertyList = maleAncestorsProperty[i] as List<Property>;
                    var maleName = propertyList.Get<string>("MaleName") ?? "";
                    var maleId1 = propertyList.Get<uint>("MaleDinoID1");
                    var maleId2 = propertyList.Get<uint>("MaleDinoID2");
                    long maleId = 0;

                    var femaleName = propertyList.Get<string>("FemaleName") ?? "";
                    var femaleId1 = propertyList.Get<uint>("FemaleDinoID1");
                    var femaleId2 = propertyList.Get<uint>("FemaleDinoID2");
                    long femaleId = 0;

                    maleAncestry.Add(new Ancestry(i, maleId, maleName, femaleId, femaleName));
                }
            }

            var tamerString = properties.Get<string>("TamerString")??"";
            

            //tamed
            return new CreatureTamed
            {
                Id = actor.Uuid,
                ClassName = actor.GetClassName(),               
                IsCryo = isInCryo,
                IsFemale = isFemale,
                BabyAge = babyAge,
                IsJuvenile = isJuvenile,
                DinoId = dinoId,
                ColorRegions = colorRegions,
                Scale = wildScale,
                TribeId = targetingTeam,
                TribeName = tribeName,
                Traits = geneTraits.ToArray(),
                ImprinterName = imprinterName,
                TamerString = tamerString,
                IsMating = isMating,
                IsNeutered = isNeutered,
                TamedName = tamedName,
                OriginalCreationGameTime = originalCreationTime,
                LastAllyInRangeGameTime = lastAllyInRangeGameTime,
                TamedAtGameTime = tamedAtTime,
                Location = transform?.Location,
                Rotation = transform?.Rotation
            };
        }

        internal override void IngestStatusRecord(GameObjectRecord statusComponent)
        {
            var properties = statusComponent.Properties;

            //wild
            var baseLevel = properties.Get<int>("BaseCharacterLevel");
            var extraLevels = properties.Get<int>("ExtraCharacterLevel");

            var wildLevels = 1;
            byte[] wildStats = new byte[12];
            for (int i = 0; i < wildStats.Length; i++)
            {
                wildStats[i] = properties.Get<byte>("NumberOfLevelUpPointsApplied", i);
                wildLevels += wildStats[i];
            }


            float[] currentStatusValues = new float[12];
            for (int i = 0; i < currentStatusValues.Length; i++)
            {
                currentStatusValues[i] = properties.Get<float>("CurrentStatusValues", i);
            }


            //tamed
            var randomMutationsMale = properties.Get<int>("RandomMutationsMale");
            var randomMutationsFemale = properties.Get<int>("RandomMutationsFemale");
            var imprintQuality = properties.Get<float>("DinoImprintQuality");
            var experiencePoints = properties.Get<float>("ExperiencePoints");

            var tameLevels = 0;
            byte[] tameStats = new byte[12];
            for (int i = 0; i < tameStats.Length; i++)
            {
                tameStats[i] = properties.Get<byte>("NumberOfLevelUpPointsAppliedTamed", i);
                tameLevels += tameStats[i];
            }

            var tameMutations = 0;
            byte[] mutationStats = new byte[12];
            for (int i = 0; i < mutationStats.Length; i++)
            {
                mutationStats[i] = properties.Get<byte>("NumberOfMutationsAppliedTamed", i);
                tameMutations += mutationStats[i];
            }

            WildStats = wildStats;
            TamedStats = tameStats;
            TamedMutations = mutationStats;

            BaseLevel = baseLevel;
            if (baseLevel == 0)
                BaseLevel = 1;

            TotalLevel = wildLevels + tameLevels;
            if (TotalLevel == 0)
                TotalLevel = 1;

            MutationsFemale = randomMutationsFemale;
            MutationsMale = randomMutationsMale;
            TotalMutations = tameMutations + randomMutationsMale + randomMutationsFemale;
        }

        internal override void RefreshTimestamps(DateTime saveDate, double gameTime)
        {
            if (OriginalCreationGameTime != null)
            {
                OriginalCreationTimestamp = saveDate.AddSeconds(OriginalCreationGameTime.Value - gameTime);
            }
            if (TamedAtGameTime != null)
            {
                TamedTimestamp = saveDate.AddSeconds(TamedAtGameTime.Value - gameTime);
            }
            if (LastAllyInRangeGameTime != null)
            {
                LastAllyInRangeTimestamp = saveDate.AddSeconds(LastAllyInRangeGameTime.Value - gameTime);
            }
        }

        public override string ToString()
        {
            var name = TamedName ?? ClassName;
            var tribe = TribeName != null ? $" [{TribeName}]" : "";
            var level = TotalLevel.HasValue ? $" Lv{TotalLevel}" : "";
            return $"{name}{level}{tribe}";
        }
    }
}
