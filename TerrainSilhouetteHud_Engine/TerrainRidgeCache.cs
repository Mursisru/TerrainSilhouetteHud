using UnityEngine;

namespace TerrainSilhouetteHud_Engine
{
    /// <summary>
    /// World-space forward corridor ridge; projected to screen each frame.
    /// </summary>
    internal static class TerrainRidgeCache
    {
        private const int MaxAz = 128;

        private static readonly Vector3[] _ridgeWorld = new Vector3[MaxAz];
        private static readonly Vector3[] _smoothRidgeWorld = new Vector3[MaxAz];
        private static readonly bool[] _ridgeValid = new bool[MaxAz];
        private static readonly bool[] _smoothRidgeValid = new bool[MaxAz];
        private static int _ridgeCount;
        private static bool _hasRidge;

        private static Vector3 _lastBuildPos;
        private static float _lastBuildTime = -999f;

        private static readonly Vector3[] _worldAzimuthTmp = new Vector3[MaxAz];
        private static readonly bool[] _worldAzimuthTmpValid = new bool[MaxAz];

        internal static bool TryFindBestThreat(
            Vector3 aircraftPos,
            Vector3 velocityFlatNormalized,
            float speedMps,
            out Vector3 threatPoint,
            out float summitY,
            out float timeToImpactSeconds)
        {
            threatPoint = default;
            summitY = Datum.LocalSeaY;
            timeToImpactSeconds = -1f;

            if (!_hasRidge)
                return false;

            float headingDotMin = Mathf.Clamp(TerrainSilhouetteHudPlugin.WarningHeadingDotMin.Value, 0.1f, 0.98f);
            float minClosing = TerrainSilhouetteHudPlugin.WarningMinClosingSpeedMps.Value;
            float bestTtc = float.MaxValue;
            bool found = false;

            int n = Mathf.Min(_ridgeCount, _ridgeWorld.Length);
            for (int i = 0; i < n; i++)
            {
                if (!_ridgeValid[i])
                    continue;

                Vector3 world = _ridgeWorld[i];
                Vector3 toFlat = Vector3.ProjectOnPlane(world - aircraftPos, Vector3.up);
                float horizDist = toFlat.magnitude;
                if (horizDist < 80f)
                    continue;

                Vector3 dir = toFlat / horizDist;
                float headingDot = Vector3.Dot(velocityFlatNormalized, dir);
                if (headingDot < headingDotMin)
                    continue;

                float closingSpeed = speedMps * headingDot;
                if (closingSpeed < minClosing)
                    continue;

                float peakY = TerrainCollisionAdvisor.EstimateSummitAltitude(world.x, world.z);
                if (aircraftPos.y >= peakY - TerrainSilhouetteHudPlugin.BelowPeakMarginMeters.Value)
                    continue;

                float ttc = horizDist / closingSpeed;
                if (ttc >= bestTtc)
                    continue;

                bestTtc = ttc;
                threatPoint = world;
                summitY = peakY;
                timeToImpactSeconds = ttc;
                found = true;
            }

            return found;
        }

        internal static bool NeedsRebuild(Vector3 aircraftPos)
        {
            if (_lastBuildTime < -900f)
                return true;

            float interval = Mathf.Max(0.02f, TerrainSilhouetteHudPlugin.HeightmapSampleInterval.Value);
            if (Time.unscaledTime - _lastBuildTime >= interval)
                return true;

            float move = Mathf.Max(50f, TerrainSilhouetteHudPlugin.HeightCacheRebuildMoveMeters.Value);
            return Vector3.Distance(aircraftPos, _lastBuildPos) >= move;
        }

        internal static void Rebuild(Camera cam, Vector3 aircraftPos)
        {
            _lastBuildTime = Time.unscaledTime;
            _lastBuildPos = aircraftPos;
            _hasRidge = false;
            TerrainHeightFieldCache.InvalidateIfMoved(aircraftPos);
            TerrainHeightService.RefreshTerrainsIfNeeded();

            float maxRange = Mathf.Max(500f, TerrainSilhouetteHudPlugin.HeightmapMaxRangeMeters.Value);
            float step = TerrainSilhouetteHudPlugin.HeightSampleStepMeters.Value;
            float minProm = TerrainSilhouetteHudPlugin.MinProminenceMeters.Value;

            TerrainCorridorProfiler.BuildRidgeWorld(
                cam,
                aircraftPos,
                TerrainSilhouetteHudPlugin.HeightmapAzimuthHalfAngle.Value,
                0f,
                TerrainSilhouetteHudPlugin.HeightmapAzimuthSamples.Value,
                maxRange,
                step,
                minProm,
                _ridgeWorld,
                _ridgeValid,
                out _ridgeCount);

            ApplyWorldAzimuthBlend(_ridgeWorld, _ridgeValid, _ridgeCount);
            _hasRidge = _ridgeCount > 0 && HasAnyValid(_ridgeValid, _ridgeCount);
        }

        internal static void ProjectForwardDense(Camera cam, Vector2[] screenDense, bool[] screenDenseValid, out int azimuthSlotCount)
        {
            azimuthSlotCount = 0;
            if (!_hasRidge)
                return;

            float worldSmooth = TerrainSilhouetteHudPlugin.HeightmapWorldSmoothTime.Value;
            Vector3[] worldSrc = _ridgeWorld;
            bool[] validSrc = _ridgeValid;
            if (worldSmooth > 0.001f)
            {
                SmoothWorldRidge(_ridgeWorld, _ridgeValid, _smoothRidgeWorld, _smoothRidgeValid, _ridgeCount, worldSmooth);
                worldSrc = _smoothRidgeWorld;
                validSrc = _smoothRidgeValid;
            }

            TerrainCorridorProfiler.ProjectRidgeToScreenDense(
                cam,
                worldSrc,
                validSrc,
                _ridgeCount,
                screenDense,
                screenDenseValid);
            azimuthSlotCount = _ridgeCount;
        }

        internal static void Clear()
        {
            _ridgeCount = 0;
            _hasRidge = false;
            _lastBuildTime = -999f;
            for (int i = 0; i < MaxAz; i++)
            {
                _ridgeValid[i] = false;
                _smoothRidgeValid[i] = false;
            }
        }

        private static bool HasAnyValid(bool[] valid, int count)
        {
            int n = Mathf.Min(count, valid.Length);
            for (int i = 0; i < n; i++)
            {
                if (valid[i])
                    return true;
            }

            return false;
        }

        private static void ApplyWorldAzimuthBlend(Vector3[] world, bool[] valid, int count)
        {
            int passes = Mathf.Clamp(TerrainSilhouetteHudPlugin.RidgeWorldAzimuthSmoothPasses.Value, 0, 4);
            if (passes <= 0)
                return;

            int n = Mathf.Min(count, world.Length, valid.Length, _worldAzimuthTmp.Length, _worldAzimuthTmpValid.Length);
            for (int p = 0; p < passes; p++)
            {
                for (int i = 0; i < n; i++)
                {
                    _worldAzimuthTmpValid[i] = false;
                    if (!valid[i])
                        continue;

                    Vector3 sum = world[i];
                    int c = 1;
                    if (i > 0 && valid[i - 1])
                    {
                        sum += world[i - 1];
                        c++;
                    }

                    if (i < n - 1 && valid[i + 1])
                    {
                        sum += world[i + 1];
                        c++;
                    }

                    _worldAzimuthTmp[i] = sum / c;
                    _worldAzimuthTmpValid[i] = true;
                }

                for (int i = 0; i < n; i++)
                {
                    valid[i] = _worldAzimuthTmpValid[i];
                    if (_worldAzimuthTmpValid[i])
                        world[i] = _worldAzimuthTmp[i];
                }
            }

            for (int i = n; i < MaxAz; i++)
                valid[i] = false;
        }

        private static void SmoothWorldRidge(
            Vector3[] source,
            bool[] sourceValid,
            Vector3[] dest,
            bool[] destValid,
            int count,
            float smoothTime)
        {
            float smooth = Mathf.Max(0f, smoothTime);
            float t = smooth <= 0f
                ? 1f
                : 1f - Mathf.Exp(-Time.unscaledDeltaTime / smooth);

            for (int i = 0; i < count; i++)
            {
                if (!sourceValid[i])
                {
                    destValid[i] = false;
                    continue;
                }

                if (destValid[i])
                    dest[i] = Vector3.Lerp(dest[i], source[i], t);
                else
                    dest[i] = source[i];

                destValid[i] = true;
            }

            for (int i = count; i < MaxAz; i++)
                destValid[i] = false;
        }
    }
}
