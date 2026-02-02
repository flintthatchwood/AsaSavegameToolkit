#!/usr/bin/env pwsh
param(
    [string]$OutputPath
)

if (!$OutputPath) {
    $OutputPath = Join-Path $PSScriptRoot "../.work/packages"
}

New-Item -ItemType Directory -Path $OutputPath -ErrorAction SilentlyContinue | Out-Null
$OutputPath = (Resolve-Path $OutputPath).Path

Write-Host "Packing AsaSavegameToolkit NuGet package into $OutputPath" -ForegroundColor Yellow
dotnet pack $PSScriptRoot/../src/AsaSavegameToolkit.csproj `
    -c Release `
    -o $OutputPath `
    /p:DebugType=embedded `
    /p:PackageOutputPath=$OutputPath
Write-Host "Packing completed." -ForegroundColor Green
