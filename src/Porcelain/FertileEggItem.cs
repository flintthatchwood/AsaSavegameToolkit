using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

namespace AsaSavegameToolkit.Porcelain;

public class FertileEggItem : Item
{
    public byte[] MutationStats { get; set; } = new byte[12];
    public bool IsFemale { get; set; }
    public int RandomMutationsFemale { get; set; }
    public int RandomMutationsMale { get; set; }
    public List<Ancestry> FemaleAncestry { get; set; } = [];
    public List<Ancestry> MaleAncestry { get; set; } = [];
    public List<string> GeneTraits { get; set; } = [];

    internal static new FertileEggItem Create(GameObjectRecord record, ActorTransform? location = default)
    {
        var properties = record.Properties;
        var item = new FertileEggItem
        {
            ClassName = record.GetClassName()
        };
        item.PopulateFromRecord(record, location);

        byte[] baseStatValues = new byte[12];
        for (int i = 0; i < baseStatValues.Length; i++)
        {
            baseStatValues[i] = properties.Get<byte>("EggNumberOfLevelUpPointsApplied", i);
        }

        byte[] mutationStatValues = new byte[12];
        for (int i = 0; i < mutationStatValues.Length; i++)
        {
            mutationStatValues[i] = properties.Get<byte>("EggNumberOfMutationsApplied", i);
        }

        int[] dinoColors = new int[6];
        for (int i = 0; i < dinoColors.Length; i++)
        {
            dinoColors[i] = properties.Get<byte>("EggColorSetIndices", i);
        }

        int randomMutationsFemale = properties.Get<int>("EggRandomMutationsFemale");
        int randomMutationsMale = properties.Get<int>("EggRandomMutationsMale");

        var genderOverride = properties.Get<int>("EggGenderOverride"); // 0=unassigned, 1=male, 2=female
        var dinoLevel = baseStatValues.Sum(x => x) + mutationStatValues.Sum(x => x) + 1;
        var customItemName = genderOverride switch
        {
            1 => $"Male - Lvl {dinoLevel}",
            2 => $"Female - Lvl {dinoLevel}",
            _ => $"Lvl {dinoLevel}"
        };

        List<Ancestry> femaleAncestry = [];
        var femaleAncestorsProperty = properties.Get<ArrayProperty>("EggDinoAncestors")?.Value;
        if (femaleAncestorsProperty != null)
        {
            for (int i = 0; i < femaleAncestorsProperty.Count; i++)
            {
                if (femaleAncestorsProperty[i] is not List<Property> propertyList)
                    continue;

                var maleName = propertyList.Get<string>("MaleName") ?? "";
                var maleId1 = propertyList.Get<uint>("MaleDinoID1");
                var maleId2 = propertyList.Get<uint>("MaleDinoID2");
                long maleId = ((long)maleId1 << 32) | (maleId2 & 0xFFFFFFFFL);

                var femaleName = propertyList.Get<string>("FemaleName") ?? "";
                var femaleId1 = propertyList.Get<uint>("FemaleDinoID1");
                var femaleId2 = propertyList.Get<uint>("FemaleDinoID2");
                long femaleId = ((long)femaleId1 << 32) | (femaleId2 & 0xFFFFFFFFL);

                femaleAncestry.Add(new Ancestry(i, maleId, maleName, femaleId, femaleName));
            }
        }

        List<Ancestry> maleAncestry = [];
        var maleAncestorsProperty = properties.Get<ArrayProperty>("EggDinoAncestorsMale")?.Value;
        if (maleAncestorsProperty != null)
        {
            for (int i = 0; i < maleAncestorsProperty.Count; i++)
            {
                if (maleAncestorsProperty[i] is not List<Property> propertyList)
                    continue;

                var maleName = propertyList.Get<string>("MaleName") ?? "";
                var maleId1 = propertyList.Get<uint>("MaleDinoID1");
                var maleId2 = propertyList.Get<uint>("MaleDinoID2");
                long maleId = ((long)maleId1 << 32) | (maleId2 & 0xFFFFFFFFL);

                var femaleName = propertyList.Get<string>("FemaleName") ?? "";
                var femaleId1 = propertyList.Get<uint>("FemaleDinoID1");
                var femaleId2 = propertyList.Get<uint>("FemaleDinoID2");
                long femaleId = ((long)femaleId1 << 32) | (femaleId2 & 0xFFFFFFFFL);

                maleAncestry.Add(new Ancestry(i, maleId, maleName, femaleId, femaleName));
            }
        }

        List<string> geneTraits = [];
        var geneTraitsArray = properties.Get<ArrayProperty>("EggDinoGeneTraits");
        if (geneTraitsArray != null && geneTraitsArray.Value.Count > 0)
        {
            foreach (FName geneTraitValue in geneTraitsArray.Value)
            {
                geneTraits.Add(geneTraitValue.ToString());
            }
        }

        item.IsBlueprint = false;
        item.IsEngram = false;
        item.IsRecipe = false;
        item.IsInitialItem = false;
        item.Durability ??= 0;
        item.ItemRating ??= 0;
        item.CustomItemDescription ??= "";
        item.BaseStats = baseStatValues;
        item.MutationStats = mutationStatValues;
        item.Colors = dinoColors;
        item.CustomItemName = customItemName;
        item.FemaleAncestry = femaleAncestry;
        item.MaleAncestry = maleAncestry;
        item.GeneTraits = geneTraits;
        item.IsFemale = genderOverride == 2;
        item.RandomMutationsFemale = randomMutationsFemale;
        item.RandomMutationsMale = randomMutationsMale;
        return item;
    }
}
