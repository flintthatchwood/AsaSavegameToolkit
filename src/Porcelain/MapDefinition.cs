using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsaSavegameToolkit.Porcelain
{
    public class MapDefinition
    {
        public string Name { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? ImageFile { get; set; }
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }

        //used to define regions within a map
        public string? BaseName { get; set; }
        public (double,double) LatitudeRange { get; set; } = new (0,100);
        public (double, double) LongitudeRange { get; set; } = new(0, 100);
        public (double, double) ZRange { get; set; } = new(0, double.MaxValue);

        public double GetLatitude(double y)
        {
            return (double)OffsetY + (y / ScaleY);
        }

        public double GetLongitude(double x)
        {
            return (double)OffsetX + (x / ScaleX);
        }
    }
}
