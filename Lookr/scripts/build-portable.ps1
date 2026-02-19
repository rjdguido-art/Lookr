param(
    [string]$Version = "1.0.0",
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\\LookrQuickText\\LookrQuickText.csproj"
$distDir = Join-Path $repoRoot "dist\\portable"
$publishTempDir = Join-Path $distDir "publish-temp-$Runtime"
$portableExeName = "LookrQuickText-$Version-$Runtime.exe"
$portableExePath = Join-Path $distDir $portableExeName
$hashFilePath = Join-Path $distDir "$portableExeName.sha256"

Write-Host "Publishing single-file portable EXE..."

if (Test-Path $publishTempDir) {
    Remove-Item $publishTempDir -Recurse -Force
}

if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir | Out-Null
}

dotnet publish $project `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishTempDir

$publishedExePath = Join-Path $publishTempDir "LookrQuickText.exe"
if (-not (Test-Path $publishedExePath)) {
    throw "Portable EXE was not produced at expected path: $publishedExePath"
}

Copy-Item $publishedExePath $portableExePath -Force
Remove-Item $publishTempDir -Recurse -Force

$hash = (Get-FileHash $portableExePath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $portableExeName" | Out-File -FilePath $hashFilePath -Encoding ascii

Write-Host "Portable EXE created:"
Write-Host "  $portableExePath"
Write-Host "SHA256 file created:"
Write-Host "  $hashFilePath"
