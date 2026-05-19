using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Utilities;

namespace AsaSavegameToolkit.Plumbing.Records
{
    public class DinoAncestorsEntryRecord
    {
        public string? MaleName { get; set; }
        public uint MaleId1 { get; set; }
        public uint MaleId2 { get; set; }
        public string? FemaleName { get; set; }
        public uint FemaleId1 { get; set; }
        public uint FemaleId2 { get; set; }

        internal static DinoAncestorsEntryRecord Read(AsaArchive archive)
        {
            var properties = Property.ReadList(archive);

            var maleName = properties.Get<string>("MaleName");
            var maleId1 = properties.Get<uint>("MaleDinoID1");
            var maleId2 = properties.Get<uint>("MaleDinoID2");
            var femaleName = properties.Get<string>("FemaleName");
            var femaleId1 = properties.Get<uint>("FemaleDinoID1");
            var femaleId2 = properties.Get<uint>("FemaleDinoID2");

            return new DinoAncestorsEntryRecord
            {
                MaleName = maleName,
                MaleId1 = maleId1,
                MaleId2 = maleId2,
                FemaleName = femaleName,
                FemaleId1 = femaleId1,
                FemaleId2 = femaleId2
            };
        }
    }
}
