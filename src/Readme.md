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

---

## Quickstart

```csharp
using AsaSavegameToolkit;
using AsaSavegameToolkit.Porcelain; // high-level, typed API
using Microsoft.Extensions.Logging.Abstractions;

var savePath = @"D:\repos\AsaSavegameToolkit\tests\assets\version_14\Aberration_WP.ark";
var save = AsaSaveGame.ReadFrom(savePath, logger: NullLogger.Instance);

Console.WriteLine($"Players: {save.Players.Count}, Tamed Creatures: {save.TamedCreatures.Count}, Wild Creatures: {save.TamedCreatures.Count}, Structures: {save.Structures.Count}");

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

---

## Supported save formats & versions

- **Save version 14+** EU5.5
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
