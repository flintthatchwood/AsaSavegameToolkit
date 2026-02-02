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
dotnet test
```

## CI/CD

The project uses GitHub Actions for continuous integration:
- **Build and Test**: Builds and tests on Ubuntu
- **Code Coverage**: Generates coverage reports
- **Release Artifacts**: Creates nuget packages that will eventually be released to nuget.org

See [.github/workflows/build-and-test.yml](.github/workflows/build-and-test.yml) for details.