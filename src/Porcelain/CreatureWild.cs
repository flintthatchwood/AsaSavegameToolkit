using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

namespace AsaSavegameToolkit.Porcelain
{
    public class CreatureWild: Creature
    {
        public static new CreatureWild Create(GameObjectRecord actor, ActorTransform? transform)
        {
            var properties = actor.Properties;

            //read properties for wild 
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

            var originalCreationTime = properties.Get<double>("OriginalCreationTime");
            var wildScale = properties.Get<float>("WildRandomScale");

            //return wild
            return new CreatureWild
            {
                Id = actor.Uuid,
                ClassName = actor.GetClassName(),
                OriginalCreationGameTime = originalCreationTime,
                IsCryo = isInCryo,
                IsFemale = isFemale,
                BabyAge = babyAge,
                IsJuvenile = isJuvenile,
                DinoId = dinoId,
                ColorRegions = colorRegions,
                Scale = wildScale,
                TribeId = targetingTeam,
                Traits = geneTraits.ToArray(),
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

            BaseLevel = baseLevel;
            if(BaseLevel==0)
                BaseLevel= 1;

            TotalLevel = wildLevels;
            if(TotalLevel==0)
                TotalLevel = 1;
        }

        internal override void RefreshTimestamps(DateTime saveDate, double gameTime)
        {
            if (OriginalCreationGameTime > 0)
            {
                OriginalCreationTimestamp = saveDate.AddSeconds(OriginalCreationGameTime - gameTime);
            }


        }
    }
}
