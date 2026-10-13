using System;
using System.Collections.Generic;

using AsaSavegameToolkit.Plumbing.Primitives;

namespace AsaSavegameToolkit.Porcelain
{
    public class MapDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public List<MapRegion> Regions { get; set; } = new() { new MapRegion() };

        public double GetLatitude(double y)
        {
            return 50.0 + (y - CenterY) / ScaleY;
        }

        public double GetLongitude(double x)
        {
            return 50.0 + (x - CenterX) / ScaleX;
        }

        public MapCoordinates GetCoordinates(FVector location)
        {
            if (!double.IsFinite(location.X) || !double.IsFinite(location.Y) || !double.IsFinite(location.Z))
                throw new ArgumentOutOfRangeException(nameof(location), "World coordinates must be finite.");

            if (!double.IsFinite(CenterX) || !double.IsFinite(CenterY)
                || !double.IsFinite(ScaleX) || ScaleX <= 0 || !double.IsFinite(ScaleY) || ScaleY <= 0)
                throw new InvalidOperationException("Map coordinates require finite centers and positive finite scales.");

            MapRegion? region = null;
            foreach (var candidate in Regions)
            {
                if (candidate.Contains(location))
                {
                    region = candidate;
                    break;
                }
            }

            return new MapCoordinates(GetLatitude(location.Y), GetLongitude(location.X), region);
        }

        public MapCoordinates? GetCoordinates(FVector? location)
        {
            return location.HasValue ? GetCoordinates(location.Value) : null;
        }
    }

    public readonly record struct MapCoordinates(double Latitude, double Longitude, MapRegion? Region);

    public readonly record struct MapRegion
    {
        public string Name { get; init; } = "default";
        public double? MinX { get; init; }
        public double? MaxX { get; init; }
        public double? MinY { get; init; }
        public double? MaxY { get; init; }
        public double? MinZ { get; init; }
        public double? MaxZ { get; init; }

        public MapRegion() { }

        public bool Contains(FVector location)
        {
            return double.IsFinite(location.X) && double.IsFinite(location.Y) && double.IsFinite(location.Z)
                && (!MinX.HasValue || location.X >= MinX.Value) && (!MaxX.HasValue || location.X <= MaxX.Value)
                && (!MinY.HasValue || location.Y >= MinY.Value) && (!MaxY.HasValue || location.Y <= MaxY.Value)
                && (!MinZ.HasValue || location.Z >= MinZ.Value) && (!MaxZ.HasValue || location.Z <= MaxZ.Value);
        }
    }
}
