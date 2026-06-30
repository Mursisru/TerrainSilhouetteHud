# GPU shader bundle (`terrainsilhouette_shaders`)

The mod loads `Hidden/at747/TerrainSilhouette/Edge` from an **AssetBundle** next to the DLL.

## Автоматическая сборка (рекомендуется)

Из корня репозитория `TerrainSilhouetteHud_Engine`:

```powershell
.\scripts\build-shader-bundle.ps1
```

Нужен **Unity 2022.3.6f1** (Hub). Игра — **2022.3.62f2**, та же линейка 2022.3.

Результат копируется в:

- `TerrainSilhouetteHud_Data/terrainsilhouette_shaders`
- `TerrainSilhouetteHud_Engine/bin/Release/TerrainSilhouetteHud_Data/`

Скопируй файл в игру: `BepInEx/plugins/TerrainSilhouetteHud_Data/terrainsilhouette_shaders`

## Ручная сборка в Unity

1. Открой папку **`UnityBundleBuilder`** в Unity Hub (2022.3.x).
2. Меню **Build → Terrain Silhouette Shaders**.
3. Скопируй `UnityBundleBuilder/BuiltBundles/terrainsilhouette_shaders` в plugins (см. выше).

Editor-скрипт: `UnityBundleBuilder/Assets/Editor/BuildTerrainBundle.cs`

## Without bundle

- Set `RenderMode = LegacyCpu` in config, or enable `FallbackToLegacyCpu = true` (default).
- GPU mode without bundle shows a warning in `BepInEx/LogOutput.log`.
