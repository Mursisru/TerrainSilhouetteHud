using UnityEngine;

namespace TerrainSilhouetteHud_Engine
{
    /// <summary>
    /// Reads ground height: Unity Terrain.SampleHeight when available, else physics down-cast.
    /// </summary>
    internal static class TerrainHeightService
    {
        private const float RaycastHeight = 8000f;
        private static Terrain[] _terrains = System.Array.Empty<Terrain>();
        private static float _lastTerrainScan;
        private static bool _loggedTerrainCount;
        private static int _terrainHits;
        private static int _raycastHits;

        internal static int TerrainCount => _terrains.Length;

        internal static void RefreshTerrainsIfNeeded()
        {
            float interval = Mathf.Max(0.5f, TerrainSilhouetteHudPlugin.TerrainRefreshInterval.Value);
            if (Time.unscaledTime - _lastTerrainScan < interval && _terrains.Length > 0)
                return;

            _lastTerrainScan = Time.unscaledTime;
            _terrains = Terrain.activeTerrains ?? System.Array.Empty<Terrain>();

            if (!_loggedTerrainCount)
            {
                _loggedTerrainCount = true;
                TerrainSilhouetteHudPlugin.Logger?.LogInfo(
                    "TerrainHeightService: " + _terrains.Length + " Unity Terrain tile(s); raycast fallback "
                    + (TerrainSilhouetteHudPlugin.UseRaycastFallback.Value ? "on" : "off") + ".");
            }
        }

        internal static bool TryGetGroundHeight(float worldX, float worldZ, out float groundY)
        {
            return TerrainHeightFieldCache.TryGetGroundHeight(worldX, worldZ, out groundY);
        }

        internal static bool TryGetGroundHeightUncached(float worldX, float worldZ, out float groundY)
        {
            groundY = Datum.LocalSeaY;

            if (TerrainSilhouetteHudPlugin.UseUnityTerrain.Value)
            {
                RefreshTerrainsIfNeeded();
                for (int i = 0; i < _terrains.Length; i++)
                {
                    Terrain t = _terrains[i];
                    if (t == null || t.terrainData == null)
                        continue;

                    Vector3 tp = t.transform.position;
                    TerrainData data = t.terrainData;
                    float maxX = tp.x + data.size.x;
                    float maxZ = tp.z + data.size.z;
                    if (worldX < tp.x || worldX > maxX || worldZ < tp.z || worldZ > maxZ)
                        continue;

                    float h = t.SampleHeight(new Vector3(worldX, 0f, worldZ)) + tp.y;
                    groundY = h;
                    _terrainHits++;
                    return true;
                }
            }

            if (!TerrainSilhouetteHudPlugin.UseRaycastFallback.Value)
                return false;

            Vector3 top = new Vector3(worldX, RaycastHeight, worldZ);
            RaycastHit hit;
            int mask = TerrainSilhouetteHudPlugin.TerrainLayerMask.Value;
            if (!Physics.Raycast(top, Vector3.down, out hit, RaycastHeight * 2f, mask))
                return false;

            if (hit.point.y < Datum.LocalSeaY + TerrainSilhouetteHudPlugin.MinGroundClearance.Value)
                return false;

            groundY = hit.point.y;
            _raycastHits++;
            return true;
        }

        internal static void LogSampleStatsOncePerInterval()
        {
            if (_terrainHits + _raycastHits == 0)
                return;
            _terrainHits = 0;
            _raycastHits = 0;
        }
    }
}
