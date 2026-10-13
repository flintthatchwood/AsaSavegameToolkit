# AsaSavegameToolkit

A .NET 8 library for reading **ARK: Survival Ascended** save files (`*.ark`, `*.arkprofile`, `*.arktribe`). Originally derived from portions of [ASV](https://github.com/miragedmuk/ASV) and the [ArkSavegameToolkit](https://github.com/DodoCooker/ArkSavegameToolkit) port of [Qowyn/ark-savegame-toolkit](https://github.com/Qowyn/ark-savegame-toolkit). This repo modernizes the parser for UE5.5-era save formats (ASA).

---

## Install

```bash
dotnet add package AsaSavegameToolkit
```

Or add directly to your project file:

```xml
<ItemGroup>
  <PackageReference Include="AsaSavegameToolkit" Version="$(LatestVersion)" />
</ItemGroup>
```

> Targets **net8.0**. Works cross‑platform (Windows/Linux/macOS). Uses `Microsoft.Extensions.Logging` for diagnostics and `Microsoft.Data.Sqlite` for parsing.

The native SQLite dependency is pinned to `SQLitePCLRaw.lib.e_sqlite3` 2.1.12
to address [CVE-2025-6965 / GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q).
The loaded SQLite engine is tested to be version 3.50.2 or newer.

---

## Quickstart

```csharp
using AsaSavegameToolkit;
using AsaSavegameToolkit.Porcelain; // high-level, typed API
using Microsoft.Extensions.Logging.Abstractions;

var savePath = @"D:\repos\AsaSavegameToolkit\tests\assets\version_14\Aberration_WP.ark";
var save = AsaSaveGame.ReadFrom(savePath, logger: NullLogger.Instance);

Console.WriteLine($"Players: {save.Players.Count}, Tamed Creatures: {save.TamedCreatures.Count}, Wild Creatures: {save.WildCreatures.Count}, Structures: {save.Structures.Count}");

// Example: list player names
foreach (var player in save.Players)
{
    Console.WriteLine($"{player.PlayerName} (Tribe: {player.TribeId}, Location: {player.Location})");
}
```

### Optional settings
```csharp
var settings = new AsaReaderSettings { MaxCores = Environment.ProcessorCount / 2 };
var save = AsaSaveGame.ReadFrom(savePath, settings: settings);
```

---

## API surface

| Layer                      | Namespace                       | Use case                                                                                                                                                |
| -------------------------- | ------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Porcelain** (high-level) | `AsaSavegameToolkit.Porcelain`  | Typed models like `AsaSaveGame`, `Creature`, `Structure`, `Inventory`, `Player`, `Tribe` with convenience helpers.                                      |
| **Plumbing** (low-level)   | `AsaSavegameToolkit.Plumbing.*` | Readers/parsers (`AsaSaveReader`, `AsaArchive`), property primitives (`FVector`, `FColor`, `FName`), and raw record types if you need custom traversal. |

Key types:
- `AsaSaveGame.ReadFrom(string path, ILogger? logger = null, AsaReaderSettings? settings = null)`
- `Creature`, `Structure`, `Item`, `Inventory`, `TeamInfo`, `Player`, `Tribe`
- Raw property readers in `Plumbing.Properties.*` when you need exact UE property formats

`CraftedItem` derives from `Item` and adds crafter names, crafting skill, and
crafting skill bonus. `Item.IsCrafted` is a read-only runtime-type indicator,
included when serializing items with System.Text.Json, even through the base
`Item` type. It does not enable serialization of subclass-specific properties.
`CraftedItem` and `FertileEggItem` share base item parsing; fertile eggs then
apply their own stats, colors, generated name, ancestry, and mutation data.

`PlayerStructure` derives from `Structure`. Both use shared base parsing for
names, activation state, transforms, and record identity; player structures add
ownership, placement, operational state, and additional timestamps. Factory
selection uses `TargetingTeam`, while `IsPlayerBuilt` remains based on the
decay-reset flag or recognized special classes (including rafts), not runtime type.

`AsaSaveGame.DeathItemCaches` exposes temporary loot-cache actors separately from
`Structures`, keyed by object ID. Each `DeathItemCache` contains its world position
and rotation, saved owner name and optional team/player identifiers, inventory,
and creation/expiration timestamps when present. Caches can originate from wild
creatures; they are not assumed to be player-owned. Inventories resolve the saved
`InventoryItems` object references to typed items; unresolved references generate
warnings rather than fabricated contents. Missing transforms or ownership data
remain `null`. Creature corpses remain creatures, not death caches.

**Classification correction:** death-cache blueprint paths are matched by short
class name. Caches previously misclassified as structures now appear only in
`DeathItemCaches`. Use `save.Map?.GetCoordinates(cache.Location)` for GPS and region.

### Map coordinates

`AsaSaveGame.Map` exposes the map definition, or `null` if no matching definition
is available. Locations on creatures, players, structures, and items are raw
world-space `FVector?` positions; rotations remain separate. Convert a position
explicitly when needed:

```csharp
var coordinates = save.Map?.GetCoordinates(creature.Location);
// coordinates?.Latitude, coordinates?.Longitude, coordinates?.Region?.Name
```

`GetCoordinates` returns a `MapCoordinates` value containing latitude, longitude,
and an optional `MapRegion`. A missing position returns `null`; a position outside
all defined regions still has coordinates but no region. Invalid non-finite
positions or invalid map calibration throw exceptions.

Definitions in [maps.json](maps.json) form a top-level JSON array and
use `CenterX`/`CenterY` in Unreal world units and `ScaleX`/`ScaleY` in world units
per GPS unit. The world-space center always maps to longitude/latitude **50, 50**:

```csharp
longitude = 50 + (x - map.CenterX) / map.ScaleX;
latitude = 50 + (y - map.CenterY) / map.ScaleY;
```

Built-in definitions are embedded in the library; no external `maps.json` is
required or loaded from the application's output directory. Supply complete
per-map replacements through `AsaReaderSettings.MapOverrides`:

```csharp
var settings = new AsaReaderSettings
{
    MapOverrides = new[]
    {
        new MapDefinition
        {
            Name = "TheIsland_WP",
            DisplayName = "The Island",
            CenterX = 0, CenterY = 0,
            ScaleX = 6858, ScaleY = 6858
        },
        new MapDefinition
        {
            Name = "MyCustomMap_WP",
            DisplayName = "My Custom Map",
            CenterX = 1000, CenterY = -2000,
            ScaleX = 8000, ScaleY = 8000
        }
    }
};
var save = AsaSaveGame.ReadFrom(savePath, settings: settings);
```

Embedded defaults load first. Override names are matched case-insensitively:
an existing name replaces its entire definition, while a new name adds a map.
If names repeat, the last supplied definition wins. Omitted or empty regions
receive a whole-map default. Each read copies the overrides, so map edits do not
mutate caller settings or other saves. Invalid overrides throw argument exceptions
rather than silently falling back to defaults.

`MapDefinition.Regions` contains named `MapRegion` structs with optional
`MinX`/`MaxX`, `MinY`/`MaxY`, and `MinZ`/`MaxZ` world-space bounds. An absent bound
means that side is unbounded. Bounds are inclusive, matching arkparser; the first
matching region wins when regions overlap. Genesis has Main and Ocean regions
separated at Z = 150000, with that shared boundary resolving to Main. Missing or
empty region data produces one unbounded `default` region covering the whole map,
including negative Z. Region bounds do not change GPS conversion.
Resources and spawn data are not included.

**Breaking changes:** `GPSLocation` has been removed; use
`save.Map?.GetCoordinates(object.Location)` instead. `Item.Location` is now a
nullable position rather than an `ActorTransform`; use `Item.Rotation` for
rotation. Items without a world transform have no location rather than an origin.
`Layers` is now `Regions`, and the unused map-level `BaseName`, `LatitudeRange`,
`LongitudeRange`, and `ZRange` properties have been removed.

`OffsetX`/`OffsetY` have been replaced by `CenterX`/`CenterY`
in both the model and JSON. To migrate an old calibration without changing its
GPS results, use `CenterX = (50 - OffsetX) * ScaleX` and likewise for Y.
Bundled definitions use arkparser's calibration where available; maps without
exports retain their previous equivalent calibration.

---

## Supported save formats & versions

- **Save version 14+** UE5.5
- **Save version 13** Earlier versions may work but aren’t officially covered.

---

## Roadmap

- Cryopods
- Inventories
- Tribes
- Profiles
- Unreal 5.7 update

---

### Attribution
Derived from
- [miragedmuk/ASV](https://github.com/miragedmuk/ASV)
- [DodoCooker/ArkSavegameToolkit](https://github.com/DodoCooker/ArkSavegameToolkit)
- [cadon/ArkSavegameToolkit](https://github.com/cadon/ArkSavegameToolkit)
- [Qowyn/ark-savegame-toolkit](https://github.com/Qowyn/ark-savegame-toolkit).
