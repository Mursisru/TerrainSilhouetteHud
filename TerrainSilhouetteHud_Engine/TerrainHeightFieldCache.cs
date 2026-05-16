using System.Collections.Generic;
using UnityEngine;

namespace TerrainSilhouetteHud_Engine
{
    /// <summary>
    /// Quantized (X,Z) height cache to avoid repeated raycasts / terrain samples.
    /// </summary>
    internal static class TerrainHeightFieldCache
    {
        private static readonly Dictionary<long, float> _cells = new Dictionary<long, float>(4096);
        private static Vector3 _anchorPos;
        private static bool _hasAnchor;

        internal static void Clear()
        {
            _cells.Clear();
            _hasAnchor = false;
        }

        internal static void InvalidateIfMoved(Vector3 aircraftPos)
        {
            float cell = Mathf.Max(50f, TerrainSilhouetteHudPlugin.HeightCacheCellMeters.Value);
            float move = Mathf.Max(cell, TerrainSilhouetteHudPlugin.HeightCacheRebuildMoveMeters.Value);
            if (!_hasAnchor)
            {
                _anchorPos = aircraftPos;
                _hasAnchor = true;
                return;
            }

            if (Vector3.Distance(aircraftPos, _anchorPos) > move)
            {
                _cells.Clear();
                _anchorPos = aircraftPos;
            }
        }

        internal static bool TryGetGroundHeight(float worldX, float worldZ, out float groundY)
        {
            float cell = Mathf.Max(50f, TerrainSilhouetteHudPlugin.HeightCacheCellMeters.Value);
            int ix = Mathf.FloorToInt(worldX / cell);
            int iz = Mathf.FloorToInt(worldZ / cell);
            long key = PackKey(ix, iz);

            if (_cells.TryGetValue(key, out groundY))
                return true;

            if (!TerrainHeightService.TryGetGroundHeightUncached(worldX, worldZ, out groundY))
                return false;

            int max = Mathf.Max(256, TerrainSilhouetteHudPlugin.HeightCacheMaxCells.Value);
            if (_cells.Count >= max)
                _cells.Clear();

            _cells[key] = groundY;
            return true;
        }

        private static long PackKey(int ix, int iz)
        {
            unchecked
            {
                return ((long)ix << 32) | (uint)iz;
            }
        }
    }
}
