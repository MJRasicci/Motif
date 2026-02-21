param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$project = "Source/GPIO.NET.Tool/GPIO.NET.Tool.csproj"
$outDir = Join-Path $repoRoot "artifacts/publish/GPIO.NET.Tool/$($Configuration.ToLower())_win-x64"

Write-Host "Publishing $project ($Configuration, win-x64) -> $outDir ..."

dotnet publish $project -c $Configuration -r win-x64 --self-contained true -o $outDir

$exePath = Join-Path $outDir "GPIO.NET.Tool.exe"
if (-not (Test-Path $exePath)) {
    throw "Expected publish output not found: $exePath"
}

Write-Host "Publish completed: $outDir"
