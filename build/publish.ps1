<#
.SYNOPSIS
  Pubblica FinePdf come singolo eseguibile self-contained.
.EXAMPLE
  ./build/publish.ps1                # win-x64, Release
  ./build/publish.ps1 -Rid linux-x64
#>
param(
    [string]$Rid = "win-x64",
    [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "artifacts/$Rid"

Write-Host "==> dotnet test"
dotnet test (Join-Path $root "FinePdf.sln") -c $Configuration --nologo

Write-Host "==> dotnet publish ($Rid, $Configuration) -> $out"
dotnet publish (Join-Path $root "src/FinePdf.App/FinePdf.App.csproj") `
    -c $Configuration -r $Rid `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $out

Write-Host "==> fatto: $out"
