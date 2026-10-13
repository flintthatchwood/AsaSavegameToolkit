using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Porcelain;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace AsaSavegameToolkit.Tests.Porcelain;

[TestClass]
public class MapDefinitionTests
{
    [TestMethod]
    [DataRow(0.0, 0.0, 6858.0, 6858.0)]
    [DataRow(-1.0, 0.0, 22150.0, 22150.0)]
    [DataRow(-157499.0, -81901.0, 7937.9, 7181.88)]
    [DataRow(-5662.0, 181487.0, 10374.04, 10374.04)]
    public void Coordinates_MapCenterAndBoundsToGPS(double centerX, double centerY, double scaleX, double scaleY)
    {
        var map = new MapDefinition
        {
            CenterX = centerX,
            CenterY = centerY,
            ScaleX = scaleX,
            ScaleY = scaleY
        };

        Assert.AreEqual(50.0, map.GetLongitude(centerX), 1e-10);
        Assert.AreEqual(50.0, map.GetLatitude(centerY), 1e-10);
        Assert.AreEqual(0.0, map.GetLongitude(centerX - 50 * scaleX), 1e-10);
        Assert.AreEqual(100.0, map.GetLongitude(centerX + 50 * scaleX), 1e-10);
        Assert.AreEqual(0.0, map.GetLatitude(centerY - 50 * scaleY), 1e-10);
        Assert.AreEqual(100.0, map.GetLatitude(centerY + 50 * scaleY), 1e-10);
        Assert.AreEqual(101.0, map.GetLongitude(centerX + 51 * scaleX), 1e-10);
        Assert.AreEqual(-1.0, map.GetLatitude(centerY - 51 * scaleY), 1e-10);

        var coordinates = map.GetCoordinates(new FVector(centerX + 10 * scaleX, centerY - 20 * scaleY, -500));
        Assert.AreEqual(30.0, coordinates.Latitude, 1e-10);
        Assert.AreEqual(60.0, coordinates.Longitude, 1e-10);
        Assert.AreEqual("default", coordinates.Region?.Name);
    }

    [TestMethod]
    [DataRow("Aberration_WP", 0.0, 0.0, 8000.0, 8000.0)]
    [DataRow("Astraeos_WP", 0.0, 0.0, 16000.0, 16000.0)]
    [DataRow("Extinction_WP", 0.0, 0.0, 6858.0, 6858.0)]
    [DataRow("Genesis_WP", -1.0, 0.0, 22150.0, 22150.0)]
    [DataRow("LostColony_WP", 0.0, 0.0, 8160.0, 8160.0)]
    [DataRow("Ragnarok_WP", 0.0, 0.0, 13100.0, 13100.0)]
    [DataRow("ScorchedEarth_WP", 50.0, 50.0, 7874.0, 7874.0)]
    [DataRow("TheCenter_WP", -5662.0, 181487.0, 10374.04, 10374.04)]
    [DataRow("TheIsland_WP", 0.0, 0.0, 6858.0, 6858.0)]
    [DataRow("Valguero_WP", 0.0, 0.0, 8160.0, 8160.0)]
    [DataRow("Forglar_WP", -157499.0, -81901.0, 7937.9, 7181.88)]
    [DataRow("ASurviveTheNight", -8766.0, 4424.0, 4033.0, 4033.0)]
    [DataRow("Svartalfheim_WP", 0.0, 0.0, 4065.0, 4065.0)]
    public void BundledDefinitions_MatchArkparser(string name, double centerX, double centerY, double scaleX, double scaleY)
    {
        var maps = ReadBundledDefinitions();
        var map = maps[name];

        Assert.AreEqual(centerX, map.CenterX, 1e-10);
        Assert.AreEqual(centerY, map.CenterY, 1e-10);
        Assert.AreEqual(scaleX, map.ScaleX, 1e-10);
        Assert.AreEqual(scaleY, map.ScaleY, 1e-10);
        Assert.IsNotEmpty(map.Regions);
    }

    [TestMethod]
    [DataRow("amissa_wp", 48.1, 46.9, 11400.0, 11375.0)]
    [DataRow("insaluna_wp", 50.0, 50.0, 9400.0, 9400.0)]
    [DataRow("nyrandil.ark", 50.0, 50.0, 8175.0, 8175.0)]
    [DataRow("reverence_wp", 50.0, 50.0, 8125.0, 8125.0)]
    [DataRow("temptress_wp", 50.0, 50.0, 8150.0, 8150.0)]
    public void BundledDefinitions_WithoutExportsPreserveCalibration(
        string name, double offsetX, double offsetY, double scaleX, double scaleY)
    {
        var map = ReadBundledDefinitions()[name];

        foreach (var coordinate in new[] { -500000.0, 0.0, 500000.0 })
        {
            Assert.AreEqual(offsetX + coordinate / scaleX, map.GetLongitude(coordinate), 1e-10);
            Assert.AreEqual(offsetY + coordinate / scaleY, map.GetLatitude(coordinate), 1e-10);
        }
    }

    [TestMethod]
    public void BundledDefinitions_GenesisPreservesRegionBounds()
    {
        var maps = ReadBundledDefinitions();
        Assert.HasCount(18, maps);
        var regions = maps["genesis_wp"].Regions;

        Assert.HasCount(2, regions);
        Assert.AreEqual("Main", regions[0].Name);
        Assert.IsNull(regions[0].MinZ);
        Assert.AreEqual(150000.0, regions[0].MaxZ);
        Assert.AreEqual("Ocean", regions[1].Name);
        Assert.AreEqual(150000.0, regions[1].MinZ);
        Assert.IsNull(regions[1].MaxZ);

        foreach (var map in maps.Values.Where(m => m.Name != "genesis_wp"))
        {
            var region = map.Regions.Single();
            Assert.AreEqual("default", region.Name);
            Assert.IsNull(region.MinX);
            Assert.IsNull(region.MaxX);
            Assert.IsNull(region.MinY);
            Assert.IsNull(region.MaxY);
            Assert.IsNull(region.MinZ);
            Assert.IsNull(region.MaxZ);
            Assert.IsTrue(region.Contains(new FVector(-1000000, 1000000, -100000)));
        }
    }

    [TestMethod]
    [DataRow(-100000.0, "Main")]
    [DataRow(149999.0, "Main")]
    [DataRow(150000.0, "Main")]
    [DataRow(150001.0, "Ocean")]
    [DataRow(300000.0, "Ocean")]
    public void GetCoordinates_GenesisSelectsFirstMatchingRegion(double z, string expectedRegion)
    {
        var map = ReadBundledDefinitions()["genesis_wp"];
        var coordinates = map.GetCoordinates(new FVector(-1, 0, z));

        Assert.AreEqual(50.0, coordinates.Latitude);
        Assert.AreEqual(50.0, coordinates.Longitude);
        Assert.AreEqual(expectedRegion, coordinates.Region?.Name);
    }

    [TestMethod]
    [DataRow(-10.0, 0.0, 0.0, true)]
    [DataRow(10.0, 0.0, 0.0, true)]
    [DataRow(0.0, -20.0, 0.0, true)]
    [DataRow(0.0, 20.0, 0.0, true)]
    [DataRow(0.0, 0.0, -30.0, true)]
    [DataRow(0.0, 0.0, 30.0, true)]
    [DataRow(-10.1, 0.0, 0.0, false)]
    [DataRow(10.1, 0.0, 0.0, false)]
    [DataRow(0.0, -20.1, 0.0, false)]
    [DataRow(0.0, 20.1, 0.0, false)]
    [DataRow(0.0, 0.0, -30.1, false)]
    [DataRow(0.0, 0.0, 30.1, false)]
    public void GetCoordinates_RegionUsesAllThreeWorldAxes(double x, double y, double z, bool expectedMatch)
    {
        var region = new MapRegion
        {
            Name = "Bounded",
            MinX = -10, MaxX = 10,
            MinY = -20, MaxY = 20,
            MinZ = -30, MaxZ = 30
        };
        var map = new MapDefinition { ScaleX = 2, ScaleY = 4, Regions = [region] };
        var position = new FVector(x, y, z);
        var coordinates = map.GetCoordinates(position);

        Assert.AreEqual(expectedMatch, region.Contains(position));
        Assert.AreEqual(50 + y / 4, coordinates.Latitude);
        Assert.AreEqual(50 + x / 2, coordinates.Longitude);
        Assert.AreEqual(expectedMatch ? (MapRegion?)region : null, coordinates.Region);
    }

    [TestMethod]
    public void GetCoordinates_NoPositionReturnsNull()
    {
        Assert.IsNull(new MapDefinition().GetCoordinates((FVector?)null));
    }

    [TestMethod]
    public void GetCoordinates_RecomputesAfterLocationOrMapChanges()
    {
        var map = new MapDefinition { ScaleX = 2, ScaleY = 4 };
        var location = new FVector(10, 20, 30);
        Assert.AreEqual(55.0, map.GetCoordinates(location).Longitude);
        map.CenterX = 10;
        Assert.AreEqual(50.0, map.GetCoordinates(location).Longitude);
        location = new FVector(20, 20, 30);
        Assert.AreEqual(55.0, map.GetCoordinates(location).Longitude);
        map.Regions = [new MapRegion { Name = "Changed" }];
        Assert.AreEqual("Changed", map.GetCoordinates(location).Region?.Name);
    }

    [TestMethod]
    [DataRow(double.NaN, 0.0, 0.0)]
    [DataRow(0.0, double.PositiveInfinity, 0.0)]
    [DataRow(0.0, 0.0, double.NegativeInfinity)]
    public void GetCoordinates_NonFinitePositionThrows(double x, double y, double z)
    {
        var map = new MapDefinition { ScaleX = 1, ScaleY = 1 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => map.GetCoordinates(new FVector(x, y, z)));
    }

    [TestMethod]
    [DataRow(0.0)]
    [DataRow(-1.0)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    public void GetCoordinates_InvalidScaleThrows(double scale)
    {
        var map = new MapDefinition { ScaleX = scale, ScaleY = 1 };
        Assert.ThrowsExactly<InvalidOperationException>(() => map.GetCoordinates(new FVector(0, 0, 0)));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(""","Regions":[]""")]
    [DataRow(""","Regions":null""")]
    public void ReadMapDefinitions_MissingOrEmptyRegionsGetWholeMapDefault(string regionsJson)
    {
        var maps = ReadDefinitions($$"""
            [{"MapName":"test","CenterX":0,"CenterY":0,"ScaleX":8000,"ScaleY":8000{{regionsJson}}}]
            """, NullLogger.Instance);

        var region = maps["test"].Regions.Single();
        Assert.AreEqual("default", region.Name);
        Assert.IsTrue(region.Contains(new FVector(-1000000, 1000000, -100000)));
    }

    [TestMethod]
    public void ReadMapDefinitions_ParsesAllRegionBounds()
    {
        var maps = ReadDefinitions("""
            [{"MapName":"test","CenterX":0,"CenterY":0,"ScaleX":8000,"ScaleY":8000,
              "Regions":[{"Name":"Box","MinX":-10,"MaxX":10,"MinY":-20,"MaxY":20,"MinZ":-30,"MaxZ":30}]}]
            """, NullLogger.Instance);

        Assert.AreEqual(new MapRegion
        {
            Name = "Box", MinX = -10, MaxX = 10, MinY = -20, MaxY = 20, MinZ = -30, MaxZ = 30
        }, maps["test"].Regions.Single());
    }

    [TestMethod]
    public void ReadMapDefinitions_OmittedCentersDefaultToWorldOrigin()
    {
        var maps = ReadDefinitions("""
            [{"MapName":"Origin_WP","ScaleX":1000,"ScaleY":2000}]
            """, NullLogger.Instance);

        Assert.AreEqual(0.0, maps["origin_wp"].CenterX);
        Assert.AreEqual(0.0, maps["origin_wp"].CenterY);
    }

    [TestMethod]
    [DataRow("""{"maps":[]}""")]
    [DataRow("{}")]
    [DataRow("null")]
    public void ReadMapDefinitions_NonArrayRootLogsWarning(string json)
    {
        var logger = new Mock<ILogger>();

        var maps = ReadDefinitions(json, logger.Object);

        Assert.IsEmpty(maps);
        Assert.IsTrue(logger.Invocations.Any(call =>
            call.Method.Name == nameof(ILogger.Log) && Equals(call.Arguments[0], LogLevel.Warning)));
    }

    [TestMethod]
    [DataRow("""{"Name":"","MinX":0}""")]
    [DataRow("""{"Name":"Invalid","MinX":10,"MaxX":0}""")]
    [DataRow("""{"Name":"Invalid","MinY":10,"MaxY":0}""")]
    [DataRow("""{"Name":"Invalid","MinZ":10,"MaxZ":0}""")]
    public void ReadMapDefinitions_InvalidRegionLogsWarning(string regionJson)
    {
        var logger = new Mock<ILogger>();
        var maps = ReadDefinitions($$"""
            [{"MapName":"test","CenterX":0,"CenterY":0,"ScaleX":8000,"ScaleY":8000,
              "Regions":[{{regionJson}}]}]
            """, logger.Object);

        Assert.IsEmpty(maps);
        Assert.IsTrue(logger.Invocations.Any(call =>
            call.Method.Name == nameof(ILogger.Log) && Equals(call.Arguments[0], LogLevel.Warning)));
    }

    [TestMethod]
    [DataRow("""{"ScaleX":8000,"ScaleY":8000}""")]
    [DataRow("""{"MapName":"invalid","CenterX":0,"CenterY":0,"ScaleX":0,"ScaleY":8000}""")]
    [DataRow("""{"MapName":"invalid","CenterX":0,"CenterY":0,"ScaleX":8000}""")]
    [DataRow("""{"MapName":"invalid","CenterX":"bad","CenterY":0,"ScaleX":8000,"ScaleY":8000}""")]
    public void ReadMapDefinitions_InvalidCalibrationLogsWarningAndDiscardsPartialData(string invalidMap)
    {
        var logger = new Mock<ILogger>();
        var maps = ReadDefinitions($$"""
                [
                    {"MapName":"valid","CenterX":0,"CenterY":0,"ScaleX":8000,"ScaleY":8000},
                    {{invalidMap}}
                ]
                """, logger.Object);

        Assert.IsEmpty(maps);
        Assert.IsTrue(logger.Invocations.Any(call =>
            call.Method.Name == nameof(ILogger.Log) && Equals(call.Arguments[0], LogLevel.Warning)));
    }

    private static Dictionary<string, MapDefinition> ReadDefinitions(string json, ILogger logger) =>
        AsaSaveGame.ReadMapDefinitions(json, logger);

    private static Dictionary<string, MapDefinition> ReadBundledDefinitions() =>
        AsaSaveGame.LoadMapDefinitions(new AsaReaderSettings(), NullLogger.Instance);

    [TestMethod]
    public void LoadMapDefinitions_UsesEmbeddedDefaults()
    {
        var resourceNames = typeof(AsaSaveGame).Assembly.GetManifestResourceNames();
        CollectionAssert.Contains(resourceNames, "AsaSavegameToolkit.maps.json");
        var maps = ReadBundledDefinitions();

        Assert.HasCount(18, maps);
        Assert.AreEqual(6858.0, maps["THEISLAND_WP"].ScaleX);
        Assert.HasCount(2, maps["GENESIS_WP"].Regions);
    }

    [TestMethod]
    public void LoadMapDefinitions_OverridesReplaceEntireDefinitionCaseInsensitively()
    {
        var replacement = new MapDefinition
        {
            Name = "GeNeSiS_Wp",
            DisplayName = "Replacement",
            CenterX = 123, CenterY = -456, ScaleX = 1000, ScaleY = 2000,
            Regions = [new MapRegion { Name = "Custom", MinZ = -100 }]
        };
        var settings = new AsaReaderSettings { MapOverrides = [replacement] };

        var maps = AsaSaveGame.LoadMapDefinitions(settings, NullLogger.Instance);
        var map = maps["genesis_wp"];

        Assert.HasCount(18, maps);
        Assert.AreEqual(replacement.Name, map.Name);
        Assert.AreEqual("Replacement", map.DisplayName);
        Assert.AreEqual(123.0, map.CenterX);
        Assert.AreEqual(-456.0, map.CenterY);
        Assert.AreEqual(1000.0, map.ScaleX);
        Assert.AreEqual(2000.0, map.ScaleY);
        Assert.HasCount(1, map.Regions);
        Assert.AreEqual(replacement.Regions[0], map.Regions[0]);
        Assert.AreEqual(8000.0, maps["aberration_wp"].ScaleX);
    }

    [TestMethod]
    public void LoadMapDefinitions_AddsCustomMapAndLastDuplicateWins()
    {
        var settings = new AsaReaderSettings
        {
            MapOverrides =
            [
                new MapDefinition { Name = "Custom_WP", ScaleX = 1000, ScaleY = 1000 },
                new MapDefinition { Name = "CUSTOM_wp", CenterX = 100, ScaleX = 2000, ScaleY = 3000 }
            ]
        };

        var maps = AsaSaveGame.LoadMapDefinitions(settings, NullLogger.Instance);

        Assert.HasCount(19, maps);
        Assert.AreEqual(100.0, maps["custom_wp"].CenterX);
        Assert.AreEqual(2000.0, maps["custom_wp"].ScaleX);
        Assert.AreEqual(3000.0, maps["custom_wp"].ScaleY);
    }

    [TestMethod]
    public void LoadMapDefinitions_EmptyOverrideRegionsGetDefaultWithoutMutatingSettings()
    {
        var replacement = new MapDefinition { Name = "Custom_WP", ScaleX = 1000, ScaleY = 1000, Regions = [] };
        var maps = AsaSaveGame.LoadMapDefinitions(new AsaReaderSettings { MapOverrides = [replacement] }, NullLogger.Instance);

        Assert.AreEqual("default", maps["custom_wp"].Regions.Single().Name);
        Assert.IsEmpty(replacement.Regions);
    }

    [TestMethod]
    public void LoadMapDefinitions_MapsAreIndependentOfSettingsAndOtherLoads()
    {
        var replacement = new MapDefinition { Name = "TheIsland_WP", ScaleX = 1000, ScaleY = 1000 };
        var settings = new AsaReaderSettings { MapOverrides = [replacement] };
        var first = AsaSaveGame.LoadMapDefinitions(settings, NullLogger.Instance);

        first["theisland_wp"].ScaleX = 1;
        first["theisland_wp"].Regions.Clear();
        first["aberration_wp"].ScaleX = 1;
        var second = AsaSaveGame.LoadMapDefinitions(settings, NullLogger.Instance);
        var defaults = ReadBundledDefinitions();

        Assert.AreEqual(1000.0, replacement.ScaleX);
        Assert.HasCount(1, replacement.Regions);
        Assert.AreEqual(1000.0, second["theisland_wp"].ScaleX);
        Assert.HasCount(1, second["theisland_wp"].Regions);
        Assert.AreEqual(8000.0, second["aberration_wp"].ScaleX);
        Assert.AreEqual(6858.0, defaults["theisland_wp"].ScaleX);
    }

    [TestMethod]
    [DataRow("", 1000.0)]
    [DataRow(" ", 1000.0)]
    [DataRow("Custom_WP", 0.0)]
    [DataRow("Custom_WP", -1000.0)]
    [DataRow("Custom_WP", double.NaN)]
    [DataRow("Custom_WP", double.PositiveInfinity)]
    public void LoadMapDefinitions_InvalidOverridesThrowRatherThanUsingDefaults(string name, double scale)
    {
        var settings = new AsaReaderSettings
        {
            MapOverrides = [new MapDefinition { Name = name, ScaleX = scale, ScaleY = 1000 }]
        };

        Assert.ThrowsExactly<ArgumentException>(() => AsaSaveGame.LoadMapDefinitions(settings, NullLogger.Instance));
    }

    [TestMethod]
    public void LoadMapDefinitions_InvalidOverrideRegionsThrow()
    {
        var settings = new AsaReaderSettings
        {
            MapOverrides =
            [
                new MapDefinition
                {
                    Name = "TheIsland_WP", ScaleX = 1000, ScaleY = 1000,
                    Regions = [new MapRegion { Name = "Invalid", MinZ = 10, MaxZ = 0 }]
                }
            ]
        };

        Assert.ThrowsExactly<ArgumentException>(() => AsaSaveGame.LoadMapDefinitions(settings, NullLogger.Instance));
    }
}
