param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$project = "Source/GPIO.NET.Tool/GPIO.NET.Tool.csproj"
Write-Host "Publishing $project ($Configuration, win-x64)..."

dotnet publish $project -c $Configuration -r win-x64 --self-contained true

$outDir = Join-Path $repoRoot "artifacts/publish/GPIO.NET.Tool/${Configuration.ToLower()}_win-x64"
if (-not (Test-Path $outDir)) {
    throw "Expected publish output not found: $outDir"
}

Write-Host "Publish completed: $outDir"
