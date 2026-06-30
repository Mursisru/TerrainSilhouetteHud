#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Build / Terrain Silhouette Shaders
/// Batch: Unity -batchmode -projectPath UnityBundleBuilder -executeMethod BuildTerrainBundle.BuildBatch -quit
/// </summary>
public static class BuildTerrainBundle
{
    private const string BundleName = "terrainsilhouette_shaders";
    private const string ShaderAssetPath = "Assets/TerrainSilhouette/TerrainSilhouetteEdge.shader";

    [MenuItem("Build/Terrain Silhouette Shaders")]
    public static void BuildFromMenu()
    {
        if (!BuildInternal())
            EditorUtility.DisplayDialog("Terrain Silhouette", "Build failed — see Console.", "OK");
        else
            EditorUtility.DisplayDialog("Terrain Silhouette", "Bundle built and copied.\nSee Console for paths.", "OK");
    }

    public static void BuildBatch()
    {
        bool ok = BuildInternal();
        EditorApplication.Exit(ok ? 0 : 1);
    }

    private static bool BuildInternal()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderAssetPath);
        if (shader == null)
        {
            Debug.LogError("[TerrainSilhouette] Shader not found at " + ShaderAssetPath);
            return false;
        }

        AssetImporter importer = AssetImporter.GetAtPath(ShaderAssetPath);
        if (importer == null)
        {
            Debug.LogError("[TerrainSilhouette] No importer for " + ShaderAssetPath);
            return false;
        }

        importer.assetBundleName = BundleName;
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outDir = Path.Combine(projectRoot, "BuiltBundles");
        if (Directory.Exists(outDir))
            Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);

        BuildPipeline.BuildAssetBundles(
            outDir,
            BuildAssetBundleOptions.ForceRebuildAssetBundle,
            BuildTarget.StandaloneWindows64);

        string bundlePath = Path.Combine(outDir, BundleName);
        if (!File.Exists(bundlePath))
        {
            Debug.LogError("[TerrainSilhouette] Bundle file missing: " + bundlePath);
            return false;
        }

        string repoRoot = Directory.GetParent(projectRoot).FullName;
        string[] copyTargets =
        {
            Path.Combine(repoRoot, "TerrainSilhouetteHud_Data", BundleName),
            Path.Combine(repoRoot, "TerrainSilhouetteHud_Engine", "bin", "Release", "TerrainSilhouetteHud_Data", BundleName),
            Path.Combine(repoRoot, "TerrainSilhouetteHud_Engine", "bin", "Debug", "TerrainSilhouetteHud_Data", BundleName),
        };

        foreach (string target in copyTargets)
        {
            string dir = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.Copy(bundlePath, target, true);
            Debug.Log("[TerrainSilhouette] Copied bundle -> " + target);
        }

        Debug.Log("[TerrainSilhouette] OK: " + bundlePath + " (" + new FileInfo(bundlePath).Length + " bytes)");
        return true;
    }
}
#endif
