# Builds a self-contained, single-file SpotiFade.exe — no .NET runtime needed
# on the target machine. Output goes to .\publish\SpotiFade.exe.
#
# Usage:  .\publish.ps1
#
# Requires the .NET 8 SDK.

$ErrorActionPreference = "Stop"

$projectDir = Join-Path $PSScriptRoot "SpotiFade"
$outputDir  = Join-Path $PSScriptRoot "publish"

if (Test-Path $outputDir) { Remove-Item -Recurse -Force $outputDir }

dotnet publish $projectDir `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $outputDir

Write-Host ""
Write-Host "Built: $(Join-Path $outputDir 'SpotiFade.exe')" -ForegroundColor Green
Write-Host "Size:  $((Get-Item (Join-Path $outputDir 'SpotiFade.exe')).Length / 1MB) MB"
