# Builds terrainsilhouette_shaders via Unity 2022.3 (Nuclear Option uses 2022.3.62f2).
$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$ProjectPath = Join-Path $RepoRoot "UnityBundleBuilder"
$UnityExe = "C:\Program Files\Unity\Hub\Editor\2022.3.6f1\Editor\Unity.exe"
if (-not (Test-Path $UnityExe)) {
    Write-Error "Unity 2022.3.6f1 not found at: $UnityExe"
}
$LogFile = Join-Path $ProjectPath "build-bundle.log"
Write-Host "Building shader bundle..."
& $UnityExe -batchmode -nographics -projectPath $ProjectPath `
    -executeMethod BuildTerrainBundle.BuildBatch -quit -logFile $LogFile
$Bundle = Join-Path $RepoRoot "TerrainSilhouetteHud_Data\terrainsilhouette_shaders"
if (Test-Path $Bundle) {
    Write-Host "OK: $Bundle ($((Get-Item $Bundle).Length) bytes)"
    if (Test-Path $LogFile) {
        Select-String -Path $LogFile -Pattern "\[TerrainSilhouette\] OK:" | Select-Object -Last 1
    }
    exit 0
}
if (Test-Path $LogFile) { Get-Content $LogFile -Tail 60 }
Write-Error "Bundle not found after build: $Bundle (see $LogFile)"
