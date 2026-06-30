**Developer:** Mursisru

# Terrain Silhouette HUD

[![Nuclear Option](https://img.shields.io/badge/Game-Nuclear%20Option-blue)](https://store.steampowered.com/app/2168680/Nuclear_Option/) [![BepInEx 5](https://img.shields.io/badge/Loader-BepInEx%205-orange)](https://docs.bepinex.dev/) [![Version](https://img.shields.io/badge/Version-1.3.2-green)](https://github.com/Mursisru/TerrainSilhouetteHud/releases/tag/v1.3.2)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow)](https://github.com/Mursisru/TerrainSilhouetteHud/blob/main/LICENSE)

---

## Critical warnings
> [!IMPORTANT]
> - **BepInEx 5 (x64) required** - install [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) before this mod.
> - **Copy `TerrainSilhouetteHud_Data\` with the DLL** - **Gpu** mode needs built `terrainsilhouette_shaders` AssetBundle (see `BUILD_SHADER_BUNDLE.md`).

> [!WARNING]
> **Collision line only under threat conditions** - below summit, flying toward terrain, TTI < 15 s (red < 4 s); respects `NightOnly` by default.

> [!NOTE]
> **Default `RenderMode=Heightmap`** - `Gpu` and `LegacyCpu` remain available in config.

Night **terrain ridge outline** on the **Flight HUD** for **Nuclear Option** (BepInEx 5).

**Current version:** **1.3.2** · GUID `com.at747.terrainsilhouettehud` · License: [MIT](LICENSE)

## Features (v1.3.x)

- **Heightmap mode (default):** forward fan samples ground height (`Terrain.SampleHeight` + raycast fallback), ridge silhouette as HUD polyline.
- **Collision warning:** line appears only when below the mountain summit, flying toward it, and estimated time to impact **&lt; 15 s**; **red** when **&lt; 4 s**.
- **HUD band:** line drawn in the central HUD window (between fuel/throttle gauges), stable when rolling.
- **Gpu** / **LegacyCpu** modes still available in config.

## Install

> [!IMPORTANT]
> **BepInEx 5 (x64) required** - install [BepInEx](https://docs.bepinex.dev/) before this mod.

1. `Directory.Build.user.props` ← copy from `Directory.Build.user.props.example`, set `NuclearOptionRoot`.
2. Build **Release** (`TerrainSilhouetteHud_Engine.slnx`).
3. Copy to `BepInEx\plugins\`:
   - `TerrainSilhouetteHud_Engine.dll`
   - folder `TerrainSilhouetteHud_Data\` (shader sources; **Gpu** mode also needs built `terrainsilhouette_shaders` — see `TerrainSilhouetteHud_Data\BUILD_SHADER_BUNDLE.md` or `scripts\build-shader-bundle.ps1`).

## Config highlights (`com.at747.terrainsilhouettehud.cfg`)

| Section | Key | Default | Notes |
|---------|-----|---------|--------|
| General | `RenderMode` | `Heightmap` | `Gpu`, `LegacyCpu` |
| General | `NightOnly` | `true` | Warning uses `Warning.RespectNightOnly` (default **false**) |
| Heightmap | `AzimuthSamples` | `96` | More = more terrain bends |
| Heightmap | `RidgeScreenNeighborPasses` | `0` | &gt;0 softens the line |
| Heightmap | `SmoothTime` / `WorldSmoothTime` | `0.05` / `0.06` | `0` = sharpest |
| Heightmap | `ScreenBandHalfWidthPixels` | `285` | HUD center band half-width |
| Warning | `MaxTimeSeconds` | `15` | Show threshold |
| Warning | `CriticalTimeSeconds` | `4` | Red line threshold |

See [CHANGELOG.md](CHANGELOG.md).

## Paths

| Role | Path |
|------|------|
| Dev (VS) | `C:\Users\at747\source\repos\TerrainSilhouetteHud_Engine\` |
| GitHub local | `C:\Users\at747\OneDrive\Desktop\GITHUB local\TerrainSilhouetteHud\` |

Build: set `NuclearOptionRoot` in local `Directory.Build.user.props` (not committed).

---

## Keywords

nuclear-option, bepinex, harmony, mod, terrainsilhouettehud, csharp, unity
