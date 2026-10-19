# AsaSavegameToolkit

[![NuGet](https://img.shields.io/nuget/v/AsaSavegameToolkit.svg)](https://www.nuget.org/packages/AsaSavegameToolkit)
[![Build & Test](https://github.com/flintthatchwood/AsaSavegameToolkit/workflows/Build%20and%20Test/badge.svg)](https://github.com/flintthatchwood/AsaSavegameToolkit/actions/workflows/build-and-test.yml)

This repo started as a direct copy of parts of the https://github.com/miragedmuk/ASV repo. `ASV` uses `ArkSavegameToolkit` from https://github.com/DodoCooker/ArkSavegameToolkit and `ArkSavegameToolkit` is a .NET port of https://github.com/Qowyn/ark-savegame-toolkit

## Features

- C# library for reading ARK: Survival Ascended save files
- Support for save version 14+ (UE5.5 structure)
- Parallel processing for improved performance
- Cross-platform support

## Building

```bash
# Restore dependencies
dotnet restore

# Build library
dotnet build

# Run tests
dotnet run --project tests/AsaSavegameToolkit.Tests/AsaSavegameToolkit.Tests.csproj -- --results-directory .work/output/tests --report-trx
```

## Test fixtures

See [public API changes](docs/PublicApiChanges.md) for the consumer-facing changes in this update.

Fixtures in [tests/assets](tests/assets) cover save versions 13, 14 and 16. Keep one canonical save per map and version, preserving upgraded worlds and shared object IDs where available.

The [version 16 fixtures](tests/assets/version_16) include Aberration, Extinction, Genesis Part 1, Lost Colony, Scorched Earth, The Center and The Island. The earlier canonical fixtures are retained.

- The Island contains two cryopodded Parasaurs named `Original` and `Duplicate`, with different pod item IDs but the same embedded creature ID. There are 172 cryopodded creatures and 171 distinct creature IDs.
- Genesis contains an older and a newer profile for the same character. The newer profile (selected by login time) and pawn contain 14,570 hexagons. The secondary profile objects record two repaired Lunar glitches, three completed missions (easy Dolphin Race, easy Space Jam, and the medium Bog gauntlet), and a creative-mode helper buff. `LatestMissionScores` contains only two score entries and must not be treated as the complete mission-completion list. The normal profile reader currently returns only the primary object; see [profile-format notes](docs/RecordTypes/ProfileFile.md#secondary-profile-objects).
- Lost Colony contains two embedded profile entries with identical login times. Entry 1 has 6 total skill points, 4 unspent points, and rank 1 in Hot Blooded and Bonded Taming. Its saved milestone progress corresponds to approximately 337/1,000 travel and 118/500 raw metal, with seven completed task tags. Two dropped cryopods exercise mesh-path object references.

## CI/CD

The project uses GitHub Actions for continuous integration:
- **Build and Test**: Builds and tests on Ubuntu
- **Code Coverage**: Generates coverage reports
- **Release**: Publishes NuGet packages and GitHub releases. The source-controlled `VersionPrefix` sets the base version; nonproduction runs append `alpha.<run number>` and create a prerelease, while production runs publish the base version.

See [.github/workflows/build-and-test.yml](.github/workflows/build-and-test.yml) for details.