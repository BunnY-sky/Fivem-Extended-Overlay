$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "FiveMOverlay.csproj"

Write-Host "=== FiveM Overlay Build ===" -ForegroundColor Cyan
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Das .NET SDK wurde nicht gefunden. Installiere .NET 8 SDK von https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    exit 1
}

dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -o (Join-Path $PSScriptRoot "publish")

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Fertig: $PSScriptRoot\publish\FiveMOverlay.exe" -ForegroundColor Green
