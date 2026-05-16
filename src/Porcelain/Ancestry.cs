using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsaSavegameToolkit.Porcelain
{
    public record Ancestry
    {
        public int TreeDepth { get; set; } = 0;
        public long MaleId { get;}
        public string MaleName { get; } = string.Empty;
        public long FemaleId { get; }
        public string FemaleName { get; } = string.Empty;

        public Ancestry(int treeDepth, long maleId, string maleName, long femaleId, string femaleName) 
        {
            TreeDepth = treeDepth;
            MaleId = maleId;
            MaleName = maleName;
            FemaleId = femaleId;
            FemaleName = femaleName;
        }
    }
}
