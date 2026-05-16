using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace TerrainSilhouetteHud_Engine
{
    internal static class TerrainSilhouetteAssets
    {
        private const string BundleFileName = "terrainsilhouette_shaders";
        private const string ShaderAssetName = "TerrainSilhouetteEdge.shader";
        private const string ShaderName = "Hidden/at747/TerrainSilhouette/Edge";

        private static Shader _edgeShader;
        private static bool _loadAttempted;

        internal static Shader EdgeShader
        {
            get
            {
                if (!_loadAttempted)
                    TryLoad();
                return _edgeShader;
            }
        }

        internal static bool IsGpuReady => EdgeShader != null;

        private static void TryLoad()
        {
            _loadAttempted = true;

            _edgeShader = LoadFromBundle();
            if (_edgeShader != null)
            {
                TerrainSilhouetteHudPlugin.Logger?.LogInfo("Terrain silhouette edge shader loaded from asset bundle.");
                return;
            }

            _edgeShader = Shader.Find(ShaderName);
            if (_edgeShader != null)
            {
                TerrainSilhouetteHudPlugin.Logger?.LogInfo("Terrain silhouette edge shader found via Shader.Find.");
                return;
            }

            TerrainSilhouetteHudPlugin.Logger?.LogWarning(
                "Terrain silhouette GPU shader not found. Place '" + BundleFileName +
                "' in BepInEx/plugins/TerrainSilhouetteHud_Data/ (build with Unity — see README). Falling back to CPU lines if enabled.");
        }

        private static Shader LoadFromBundle()
        {
            try
            {
                string pluginDir = Path.GetDirectoryName(TerrainSilhouetteHudPlugin.Instance.Info.Location);
                if (string.IsNullOrEmpty(pluginDir))
                    return null;

                string[] candidates =
                {
                    Path.Combine(pluginDir, "TerrainSilhouetteHud_Data", BundleFileName),
                    Path.Combine(pluginDir, BundleFileName),
                    Path.Combine(Paths.PluginPath, "TerrainSilhouetteHud_Data", BundleFileName),
                };

                foreach (string path in candidates)
                {
                    if (!File.Exists(path))
                        continue;

                    AssetBundle bundle = AssetBundle.LoadFromFile(path);
                    if (bundle == null)
                        continue;

                    Shader shader = bundle.LoadAsset<Shader>(ShaderAssetName);
                    if (shader == null)
                        shader = bundle.LoadAsset<Shader>("TerrainSilhouetteEdge");

                    if (shader != null)
                        return shader;
                }
            }
            catch (Exception ex)
            {
                TerrainSilhouetteHudPlugin.Logger?.LogWarning("Asset bundle load failed: " + ex.Message);
            }

            return null;
        }
    }
}
