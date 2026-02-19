param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\\LookrQuickText\\LookrQuickText.csproj"
$publishDir = Join-Path $repoRoot "installer\\publish"
$installerScript = Join-Path $repoRoot "installer\\LookrQuickText.iss"

Write-Host "Publishing application..."
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish $project -c Release -r win-x64 --self-contained true -o $publishDir

if (-not (Test-Path $publishDir)) {
    throw "Publish directory not found: $publishDir"
}

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc) {
    throw "Inno Setup compiler (iscc.exe) not found. Install Inno Setup and ensure iscc.exe is in PATH."
}

Write-Host "Building installer..."
& $iscc.Source "/DMyAppVersion=$Version" "/DMySourceDir=$publishDir" $installerScript

Write-Host "Installer build completed. Output is in installer\\dist"
