using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using AsaSavegameToolkit.Porcelain;

namespace AsaSavegameToolkit
{
    public class FertileEggItem : Item
    {
        public byte[] MutationStats { get; set; } = new byte[12];
        public bool IsFemale { get; set; } = false;
        public int RandomMutationsFemale { get; set; }
        public int RandomMutationsMale { get; set; }
        public List<Ancestry> FemaleAncestry { get; set; } = new List<Ancestry>();
        public List<Ancestry> MaleAncestry { get; set; } = new List<Ancestry>();
        public List<string> GeneTraits { get; set; } = new List<string>();


        internal static FertileEggItem Create(GameObjectRecord record, ActorTransform? location = default)
        {
            var className = record.GetClassName();

            var properties = record.Properties;

            uint itemId1 = 0;
            uint itemId2 = 0;
            int itemQuantity = 1;
            float savedDurability = 0;
            float itemRating = 0;
            string customItemName = string.Empty;
            string customItemDescription = string.Empty;

            
            var itemIdProperties = (List<Property>?)properties.Get<StructProperty>("ItemID")?.Value;
            if (itemIdProperties != null)
            {
                itemId1 = itemIdProperties.Get<uint>("ItemID1");
                itemId2 = itemIdProperties.Get<uint>("ItemID2");
            }
            var itemId = long.Parse($"{itemId1}{itemId2}");

            itemQuantity = properties.Get<int>("ItemQuantity");
            if (itemQuantity == 0)
                itemQuantity = 1;


            savedDurability = properties.Get<float>("SavedDurability"); //tools, saddles, structures, clothing etc.
            itemRating = properties.Get<float>("ItemRating");
            customItemDescription = properties.Get<string>("CustomItemDescription") ?? ""; //parents

            //EggNumberOfLevelUpPointsApplied
            byte[] baseStatValues = new byte[12];
            for (int i = 0; i < baseStatValues.Length; i++)
            {
                baseStatValues[i] = properties.Get<byte>("EggNumberOfLevelUpPointsApplied",i);
            }


            //EggNumberMutationsApplied //byte
            byte[] mutationStatValues = new byte[12];
            for (int i = 0; i < mutationStatValues.Length; i++)
            {
                mutationStatValues[i] = properties.Get<byte>("EggNumberOfMutationsApplied",i);
            }


            //EggColorSetIndices //byte
            int[] dinoColors = new int[6];
            for (int i = 0; i < dinoColors.Length; i++)
            {
                dinoColors[i] = properties.Get<byte>("EggColorSetIndices",i);
            }

            //EggRandomMutationsFemale //int
            int randomMutationsFemale = properties.Get<int>("EggRandomMutationsFemale");

            //EggRandomMutationsMale //int
            int randomMutationsMale = properties.Get<int>("EggRandomMutationsMale");

            //EggGenderOverride //int
            var genderOverride = properties.Get<int>("EggGenderOverride"); //0=unassigned,1=male,2=female
            var dinoLevel = baseStatValues.Sum(x => x) + mutationStatValues.Sum(x => x) + 1;
            customItemName = genderOverride switch
            {
                1 => $"Male - Lvl {dinoLevel}",
                2 => $"Female - Lvl {dinoLevel}",
                _ => $"Lvl {dinoLevel}"
            };

            //EggDinoAncestors
            List<Ancestry> femaleAncestry = new(); //depth, data
            var femaleAncestorsProperty = properties.Get<ArrayProperty>("EggDinoAncestors")?.Value;
            if (femaleAncestorsProperty != null)
            {
                for (int i = 0; i < femaleAncestorsProperty.Count; i++)
                {
                    List<Property> propertyList = femaleAncestorsProperty[i] as List<Property>;
                    if (propertyList == null)
                        continue;//skip empty properties

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

            //EggDinoAncestorsMale
            List<Ancestry> maleAncestry = new();
            var maleAncestorsProperty = properties.Get<ArrayProperty>("EggDinoAncestorsMale")?.Value;
            if (maleAncestorsProperty != null)
            {
                for (int i = 0; i < maleAncestorsProperty.Count; i++)
                {
                    List<Property> propertyList = maleAncestorsProperty[i] as List<Property>;
                    if (propertyList == null)
                        continue;

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

            //EggDinoGeneTraits
            List<string> geneTraits = new List<string>();
            var geneTraitsArray = properties.Get<ArrayProperty>("EggDinoGeneTraits");
            if (geneTraitsArray != null && geneTraitsArray.Value.Count > 0)
            {
                foreach (FName geneTraitValue in geneTraitsArray.Value)
                {
                    geneTraits.Add(geneTraitValue.ToString());
                }
            }

            return new FertileEggItem
            {
                Id = record.Uuid,
                ClassName = className,
                ItemId = itemId,
                Quantity = itemQuantity,
                IsBlueprint = false,
                IsEngram = false,
                IsRecipe = false,
                Durability = savedDurability,
                ItemRating = itemRating,
                BaseStats = baseStatValues,
                MutationStats = mutationStatValues,
                Colors = dinoColors,
                FemaleAncestry = femaleAncestry,
                MaleAncestry = maleAncestry,
                GeneTraits = geneTraits,
                IsFemale = genderOverride == 2,
                RandomMutationsFemale = randomMutationsFemale,
                RandomMutationsMale = randomMutationsMale
            };

        }
    }
}
