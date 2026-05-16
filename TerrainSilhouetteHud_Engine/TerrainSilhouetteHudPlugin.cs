using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace TerrainSilhouetteHud_Engine
{
    public enum TerrainRenderMode
    {
        Heightmap = 0,
        Gpu = 1,
        LegacyCpu = 2,
    }

    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class TerrainSilhouetteHudPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.at747.terrainsilhouettehud";
        public const string PluginName = "Terrain Silhouette HUD";
        public const string PluginVersion = "1.3.2";

        internal static TerrainSilhouetteHudPlugin Instance { get; private set; }
        internal static new ManualLogSource Logger { get; private set; }

        internal static ConfigEntry<bool> Enabled { get; private set; }
        internal static ConfigEntry<TerrainRenderMode> RenderMode { get; private set; }
        internal static ConfigEntry<bool> FallbackToLegacyCpu { get; private set; }
        internal static ConfigEntry<bool> ShowOnlyWhenFlightControlsEnabled { get; private set; }
        internal static ConfigEntry<bool> NightOnly { get; private set; }
        internal static ConfigEntry<float> NightStartHour { get; private set; }
        internal static ConfigEntry<float> NightEndHour { get; private set; }

        internal static ConfigEntry<float> GpuResolutionScale { get; private set; }
        internal static ConfigEntry<int> RtDepthBits { get; private set; }
        internal static ConfigEntry<float> EdgeThreshold { get; private set; }
        internal static ConfigEntry<float> EdgeStrength { get; private set; }
        internal static ConfigEntry<float> NearDepthBias { get; private set; }
        internal static ConfigEntry<float> NearDepthScale { get; private set; }
        internal static ConfigEntry<bool> UseDepthTint { get; private set; }
        internal static ConfigEntry<float> OverlayAlpha { get; private set; }
        internal static ConfigEntry<bool> UseHudColor { get; private set; }
        internal static ConfigEntry<string> LineColorHex { get; private set; }
        internal static ConfigEntry<string> NearColorHex { get; private set; }
        internal static ConfigEntry<int> TerrainLayerMask { get; private set; }

        internal static ConfigEntry<int> WideSampleCount { get; private set; }
        internal static ConfigEntry<int> NearSampleCount { get; private set; }
        internal static ConfigEntry<float> AzimuthHalfAngleDeg { get; private set; }
        internal static ConfigEntry<float> NearAzimuthHalfAngleDeg { get; private set; }
        internal static ConfigEntry<float> MaxRangeMeters { get; private set; }
        internal static ConfigEntry<float> NearTerrainDistanceMeters { get; private set; }
        internal static ConfigEntry<float> NearAglThresholdMeters { get; private set; }
        internal static ConfigEntry<float> PitchScanStepDeg { get; private set; }
        internal static ConfigEntry<float> NearPitchScanStepDeg { get; private set; }
        internal static ConfigEntry<float> SampleIntervalSeconds { get; private set; }
        internal static ConfigEntry<float> MaxScreenGapPixels { get; private set; }
        internal static ConfigEntry<float> LineThicknessPixels { get; private set; }
        internal static ConfigEntry<float> NearLineThicknessPixels { get; private set; }
        internal static ConfigEntry<float> FarLineAlpha { get; private set; }
        internal static ConfigEntry<float> NearLineAlpha { get; private set; }
        internal static ConfigEntry<float> SmoothTime { get; private set; }

        internal static ConfigEntry<bool> UseUnityTerrain { get; private set; }
        internal static ConfigEntry<bool> UseRaycastFallback { get; private set; }
        internal static ConfigEntry<float> TerrainRefreshInterval { get; private set; }
        internal static ConfigEntry<float> CorridorLengthMeters { get; private set; }
        internal static ConfigEntry<float> HeightSampleStepMeters { get; private set; }
        internal static ConfigEntry<float> MinGroundClearance { get; private set; }
        internal static ConfigEntry<float> MinProminenceMeters { get; private set; }
        internal static ConfigEntry<int> HeightmapAzimuthSamples { get; private set; }
        internal static ConfigEntry<int> HeightmapNearAzimuthSamples { get; private set; }
        internal static ConfigEntry<float> HeightmapAzimuthHalfAngle { get; private set; }
        internal static ConfigEntry<float> HeightmapNearHalfAngle { get; private set; }
        internal static ConfigEntry<float> HeightmapMaxRangeMeters { get; private set; }
        internal static ConfigEntry<float> HeightmapNearRangeMeters { get; private set; }
        internal static ConfigEntry<float> HeightmapNearAglMeters { get; private set; }
        internal static ConfigEntry<float> HeightmapSampleInterval { get; private set; }
        internal static ConfigEntry<float> HeightmapLineThickness { get; private set; }
        internal static ConfigEntry<float> HeightmapNearLineThickness { get; private set; }
        internal static ConfigEntry<float> HeightmapLineAlpha { get; private set; }
        internal static ConfigEntry<float> HeightmapFarAlpha { get; private set; }
        internal static ConfigEntry<float> HeightmapNearAlpha { get; private set; }
        internal static ConfigEntry<float> HeightmapSmoothTime { get; private set; }
        internal static ConfigEntry<float> HeightmapMaxScreenGap { get; private set; }
        internal static ConfigEntry<float> HeightCacheCellMeters { get; private set; }
        internal static ConfigEntry<int> HeightCacheMaxCells { get; private set; }
        internal static ConfigEntry<float> HeightCacheRebuildMoveMeters { get; private set; }
        internal static ConfigEntry<float> HeightmapWorldSmoothTime { get; private set; }
        internal static ConfigEntry<float> ScreenBandShrinkRatio { get; private set; }
        internal static ConfigEntry<float> ScreenBandInsetPixels { get; private set; }
        internal static ConfigEntry<float> ScreenBandLeftFraction { get; private set; }
        internal static ConfigEntry<float> ScreenBandRightFraction { get; private set; }
        internal static ConfigEntry<float> ScreenBandRefreshSeconds { get; private set; }
        internal static ConfigEntry<float> ScreenBandHalfWidthPixels { get; private set; }
        internal static ConfigEntry<float> WarningMaxTimeSeconds { get; private set; }
        internal static ConfigEntry<float> WarningCriticalTimeSeconds { get; private set; }
        internal static ConfigEntry<float> WarningHeadingDotMin { get; private set; }
        internal static ConfigEntry<float> WarningMinClosingSpeedMps { get; private set; }
        internal static ConfigEntry<float> WarningMinSpeedMps { get; private set; }
        internal static ConfigEntry<float> BelowPeakMarginMeters { get; private set; }
        internal static ConfigEntry<float> SummitSearchRadiusMeters { get; private set; }
        internal static ConfigEntry<bool> WarningRespectNight { get; private set; }
        internal static ConfigEntry<string> WarningColorHex { get; private set; }
        internal static ConfigEntry<float> RidgeFineRefineFraction { get; private set; }
        internal static ConfigEntry<int> RidgeWorldAzimuthSmoothPasses { get; private set; }
        internal static ConfigEntry<int> RidgeScreenNeighborSmoothPasses { get; private set; }
        internal static ConfigEntry<float> RidgeScreenNeighborBlend { get; private set; }
        internal static ConfigEntry<int> RidgeMaxGapBridgeSlots { get; private set; }

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "Draw terrain silhouette on the Flight HUD.");
            RenderMode = Config.Bind("General", "RenderMode", TerrainRenderMode.Heightmap,
                "Heightmap = corridor height field (TerrainData + raycast). Gpu = RT camera. LegacyCpu = old ray fan.");
            FallbackToLegacyCpu = Config.Bind("General", "FallbackToLegacyCpu", true,
                "If Gpu shader bundle is missing, use LegacyCpu when RenderMode is Gpu.");
            ShowOnlyWhenFlightControlsEnabled = Config.Bind("General", "ShowOnlyWhenFlightControlsEnabled", true,
                "Hide while menus or non-flight controls are active.");
            NightOnly = Config.Bind("General", "NightOnly", true,
                "Only show during dark hours.");
            NightStartHour = Config.Bind("General", "NightStartHour", 18.4f, "Night begins (LevelInfo.timeOfDay).");
            NightEndHour = Config.Bind("General", "NightEndHour", 5.6f, "Night ends at dawn.");

            GpuResolutionScale = Config.Bind("Gpu", "ResolutionScale", 0.4f,
                "Terrain camera RT scale vs screen (0.25–0.6 recommended).");
            RtDepthBits = Config.Bind("Gpu", "DepthBits", 24, "Depth buffer bits for terrain RT.");
            EdgeThreshold = Config.Bind("Gpu", "EdgeThreshold", 0.06f, "Sobel edge cutoff.");
            EdgeStrength = Config.Bind("Gpu", "EdgeStrength", 1.6f, "Edge brightness multiplier.");
            NearDepthBias = Config.Bind("Gpu", "NearDepthBias", 0.002f, "Depth tint bias for nearby terrain.");
            NearDepthScale = Config.Bind("Gpu", "NearDepthScale", 2400f, "How fast nearby terrain shifts to NearColor.");
            UseDepthTint = Config.Bind("Gpu", "UseDepthTint", true,
                "Tint closer terrain toward NearColor (needs URP CopyDepth shader in game).");
            OverlayAlpha = Config.Bind("Gpu", "OverlayAlpha", 0.88f, "Master alpha for GPU overlay on HUD.");
            UseHudColor = Config.Bind("Gpu", "UseHudColor", true, "Use Settings → HUD color for edges.");
            LineColorHex = Config.Bind("Gpu", "LineColorHex", "#00FF00FF", "Edge color when UseHudColor is false.");
            NearColorHex = Config.Bind("Gpu", "NearColorHex", "#FF5533FF", "Near-terrain highlight color.");
            TerrainLayerMask = Config.Bind("Gpu", "TerrainLayerMask", 2112,
                "Culling mask for terrain-only camera and heightmap raycast fallback (2112).");

            UseUnityTerrain = Config.Bind("Heightmap", "UseUnityTerrain", true,
                "Read height from Unity Terrain tiles (Terrain.SampleHeight) when in range.");
            UseRaycastFallback = Config.Bind("Heightmap", "UseRaycastFallback", true,
                "If Terrain tiles miss, raycast down (needed for mesh mountains).");
            TerrainRefreshInterval = Config.Bind("Heightmap", "TerrainRefreshInterval", 3f,
                "Seconds between rescans for Terrain.activeTerrains.");
            CorridorLengthMeters = Config.Bind("Heightmap", "CorridorLengthMeters", 14000f,
                "How far ahead (m) to sample the height corridor.");
            HeightSampleStepMeters = Config.Bind("Heightmap", "HeightSampleStepMeters", 100f,
                "Distance step along each bearing when searching the ridge line.");
            MinGroundClearance = Config.Bind("Heightmap", "MinGroundClearance", 0.5f,
                "Ignore ground at or below sea level + this margin.");
            MinProminenceMeters = Config.Bind("Heightmap", "MinProminenceMeters", 12f,
                "Minimum elevation above Datum.LocalSeaY; lower shows smaller ridges (more noise risk).");
            HeightmapAzimuthSamples = Config.Bind("Heightmap", "AzimuthSamples", 96,
                "Horizontal samples for the wide silhouette (more = more bends).");
            HeightmapNearAzimuthSamples = Config.Bind("Heightmap", "NearAzimuthSamples", 40,
                "Samples for nearby mountain detail.");
            HeightmapAzimuthHalfAngle = Config.Bind("Heightmap", "AzimuthHalfAngleDeg", 50f,
                "Half-width of forward fan in degrees.");
            HeightmapNearHalfAngle = Config.Bind("Heightmap", "NearHalfAngleDeg", 18f,
                "Half-width of near-detail fan.");
            HeightmapMaxRangeMeters = Config.Bind("Heightmap", "MaxRangeMeters", 14000f,
                "Max horizontal distance for ridge search.");
            HeightmapNearRangeMeters = Config.Bind("Heightmap", "NearRangeMeters", 6000f,
                "Enable near pass when closest terrain is within this range.");
            HeightmapNearAglMeters = Config.Bind("Heightmap", "NearAglMeters", 4000f,
                "Also enable near pass below this AGL.");
            HeightmapSampleInterval = Config.Bind("Heightmap", "SampleIntervalSeconds", 0.12f,
                "Min seconds between ridge rebuilds (screen projection runs every frame).");
            HeightCacheCellMeters = Config.Bind("Heightmap", "HeightCacheCellMeters", 180f,
                "Grid cell size (m) for cached ground heights.");
            HeightCacheMaxCells = Config.Bind("Heightmap", "HeightCacheMaxCells", 12000,
                "Max cached height cells before clearing.");
            HeightCacheRebuildMoveMeters = Config.Bind("Heightmap", "HeightCacheRebuildMoveMeters", 400f,
                "Also rebuild ridge when aircraft moves this far (m).");
            HeightmapWorldSmoothTime = Config.Bind("Heightmap", "WorldSmoothTime", 0.06f,
                "Smooth ridge world positions between rebuilds (0 = follow terrain sharply).");
            RidgeFineRefineFraction = Config.Bind("Heightmap", "RidgeFineRefineFraction", 0.35f,
                "Sub-step refinement around coarse ridge peak (0 = off). Improves small bumps.");
            RidgeWorldAzimuthSmoothPasses = Config.Bind("Heightmap", "RidgeWorldAzimuthPasses", 0,
                "Low-pass ridges across horizontal fan (0 = max detail, 1+ softens bends).");
            RidgeScreenNeighborSmoothPasses = Config.Bind("Heightmap", "RidgeScreenNeighborPasses", 0,
                "Screen-space neighbor blur (0 = sharp terrain-following line).");
            RidgeScreenNeighborBlend = Config.Bind("Heightmap", "RidgeScreenNeighborBlend", 0.35f,
                "Blend toward neighbor average when RidgeScreenNeighborPasses > 0.");
            RidgeMaxGapBridgeSlots = Config.Bind("Heightmap", "RidgeMaxGapBridgeSlots", 4,
                "Interpolate across up to N empty azimuth slots (keeps line continuous).");
            HeightmapLineThickness = Config.Bind("Heightmap", "LineThicknessPixels", 2f, "Wide line thickness.");
            HeightmapNearLineThickness = Config.Bind("Heightmap", "NearLineThicknessPixels", 3f, "Near line thickness.");
            HeightmapLineAlpha = Config.Bind("Heightmap", "LineAlpha", 0.85f, "Forward silhouette line alpha.");
            HeightmapFarAlpha = Config.Bind("Heightmap", "FarLineAlpha", 0.5f, "Unused (legacy cfg).");
            HeightmapNearAlpha = Config.Bind("Heightmap", "NearLineAlpha", 0.95f, "Unused (legacy cfg).");
            HeightmapSmoothTime = Config.Bind("Heightmap", "SmoothTime", 0.05f,
                "Temporal smooth per azimuth slot on screen (0 = no lag, sharper).");
            HeightmapMaxScreenGap = Config.Bind("Heightmap", "MaxScreenGapPixels", 52f, "Break polyline if gap is larger.");
            ScreenBandShrinkRatio = Config.Bind("Heightmap", "ScreenBandShrinkRatio", 0.9f,
                "Narrow band vs fuel–throttle span (< 1 = slightly inside gauges).");
            ScreenBandInsetPixels = Config.Bind("Heightmap", "ScreenBandInsetPixels", 10f,
                "Extra inset (px) inside fuel/throttle inner edges.");
            ScreenBandLeftFraction = Config.Bind("Heightmap", "ScreenBandLeftFraction", 0.34f,
                "Fallback min X if gauges not found (fraction of screen width).");
            ScreenBandRightFraction = Config.Bind("Heightmap", "ScreenBandRightFraction", 0.66f,
                "Fallback max X if gauges not found.");
            ScreenBandRefreshSeconds = Config.Bind("Heightmap", "ScreenBandRefreshSeconds", 0.5f,
                "Unused (band is HUD-centered each frame).");
            ScreenBandHalfWidthPixels = Config.Bind("Heightmap", "ScreenBandHalfWidthPixels", 285f,
                "Half-width (px) of silhouette band from HUD center. 0 = measure once from gauges.");

            WarningMaxTimeSeconds = Config.Bind("Warning", "MaxTimeSeconds", 15f,
                "Show silhouette only if estimated time to mountain impact is below this.");
            WarningCriticalTimeSeconds = Config.Bind("Warning", "CriticalTimeSeconds", 4f,
                "Line turns red below this time to impact.");
            WarningHeadingDotMin = Config.Bind("Warning", "HeadingDotMin", 0.55f,
                "Min dot(velocity, direction-to-mountain). 1 = exactly ahead.");
            WarningMinClosingSpeedMps = Config.Bind("Warning", "MinClosingSpeedMps", 25f,
                "Min closing speed (m/s) toward the mountain.");
            WarningMinSpeedMps = Config.Bind("Warning", "MinSpeedMps", 20f,
                "Below this speed the warning is hidden.");
            BelowPeakMarginMeters = Config.Bind("Warning", "BelowPeakMarginMeters", 40f,
                "Aircraft must be at least this many meters below the summit.");
            SummitSearchRadiusMeters = Config.Bind("Warning", "SummitSearchRadiusMeters", 900f,
                "Radius to search for peak height around the threat point.");
            WarningRespectNight = Config.Bind("Warning", "RespectNightOnly", false,
                "If true, warning also requires NightOnly time window.");
            WarningColorHex = Config.Bind("Warning", "CriticalColorHex", "#FF4422FF",
                "Line color when time to impact is below CriticalTimeSeconds.");

            WideSampleCount = Config.Bind("LegacyCpu", "WideSampleCount", 56, "Legacy CPU: wide fan ray count.");
            NearSampleCount = Config.Bind("LegacyCpu", "NearSampleCount", 36, "Legacy CPU: near fan ray count.");
            AzimuthHalfAngleDeg = Config.Bind("LegacyCpu", "AzimuthHalfAngleDeg", 52f, "Legacy CPU: wide fan half-angle.");
            NearAzimuthHalfAngleDeg = Config.Bind("LegacyCpu", "NearAzimuthHalfAngleDeg", 20f, "Legacy CPU: near fan half-angle.");
            MaxRangeMeters = Config.Bind("LegacyCpu", "MaxRangeMeters", 18000f, "Legacy CPU: max ray distance.");
            NearTerrainDistanceMeters = Config.Bind("LegacyCpu", "NearTerrainDistanceMeters", 7000f, "Legacy CPU: near pass distance gate.");
            NearAglThresholdMeters = Config.Bind("LegacyCpu", "NearAglThresholdMeters", 3500f, "Legacy CPU: near pass AGL gate.");
            PitchScanStepDeg = Config.Bind("LegacyCpu", "PitchScanStepDeg", 1.25f, "Legacy CPU: pitch scan step.");
            NearPitchScanStepDeg = Config.Bind("LegacyCpu", "NearPitchScanStepDeg", 0.65f, "Legacy CPU: near pitch step.");
            SampleIntervalSeconds = Config.Bind("LegacyCpu", "SampleIntervalSeconds", 0.06f, "Legacy CPU: min seconds between ray batches.");
            MaxScreenGapPixels = Config.Bind("LegacyCpu", "MaxScreenGapPixels", 48f, "Legacy CPU: break polyline gaps.");
            LineThicknessPixels = Config.Bind("LegacyCpu", "LineThicknessPixels", 2f, "Legacy CPU: wide line thickness.");
            NearLineThicknessPixels = Config.Bind("LegacyCpu", "NearLineThicknessPixels", 3f, "Legacy CPU: near line thickness.");
            FarLineAlpha = Config.Bind("LegacyCpu", "FarLineAlpha", 0.42f, "Legacy CPU: wide line alpha.");
            NearLineAlpha = Config.Bind("LegacyCpu", "NearLineAlpha", 0.92f, "Legacy CPU: near line alpha.");
            SmoothTime = Config.Bind("LegacyCpu", "SmoothTime", 0.05f, "Legacy CPU: screen smoothing.");

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded (RenderMode={RenderMode.Value}).");
            if (TerrainSilhouetteAssets.EdgeShader == null && RenderMode.Value == TerrainRenderMode.Gpu)
                Logger.LogWarning("Gpu mode: build terrainsilhouette_shaders bundle (see README) or set RenderMode=LegacyCpu.");
        }

        private void LateUpdate()
        {
            TerrainSilhouetteHudController.Tick();
        }

        private void OnDestroy()
        {
            TerrainSilhouetteHudController.Shutdown();
        }
    }
}
