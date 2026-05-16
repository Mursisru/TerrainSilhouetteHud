using UnityEngine;

namespace TerrainSilhouetteHud_Engine
{
    /// <summary>
    /// Samples a forward corridor on the height field into world-space ridge points (bearing order).
    /// </summary>
    internal static class TerrainCorridorProfiler
    {
        private const int MaxPoints = 128;

        internal struct ProfileResult
        {
            public int Count;
            public float ClosestHorizMeters;
            public float ClosestAzimuthDeg;
        }

        /// <summary>Wide fan scan to find bearing of the nearest prominent ridge.</summary>
        internal static bool TryFindDominantTerrainPointAlongRay(
            Vector3 aircraftPos,
            Vector3 camPos,
            Vector3 horizDir,
            float sea,
            float maxR,
            float step,
            float minProminenceM,
            float fineRefineFraction,
            out Vector3 dominantWorld)
        {
            dominantWorld = default;

            float bestElev = -999f;
            Vector3 bestWorld = default;
            bool found = false;
            float bestAlong = step;

            for (float dist = step; dist <= maxR; dist += step)
            {
                float x = aircraftPos.x + horizDir.x * dist;
                float z = aircraftPos.z + horizDir.z * dist;
                if (!TerrainHeightFieldCache.TryGetGroundHeight(x, z, out float groundY))
                    continue;

                if (groundY - sea < minProminenceM)
                    continue;

                Vector3 world = new Vector3(x, groundY, z);
                Vector3 to = world - camPos;
                float horiz = new Vector2(to.x, to.z).magnitude;
                if (horiz < 1f)
                    continue;

                float elevDeg = Mathf.Atan2(to.y, horiz) * Mathf.Rad2Deg;
                if (elevDeg <= bestElev)
                    continue;

                bestElev = elevDeg;
                bestWorld = world;
                bestAlong = dist;
                found = true;
            }

            if (!found)
                return false;

            float frac = Mathf.Clamp01(fineRefineFraction);
            if (frac <= 1e-4f)
            {
                dominantWorld = bestWorld;
                return true;
            }

            float fineStep = Mathf.Max(12f, step * frac);
            float dStart = Mathf.Max(step, bestAlong - step);
            float dEnd = Mathf.Min(maxR, bestAlong + step);
            for (float dist = dStart; dist <= dEnd + 1e-3f; dist += fineStep)
            {
                float x = aircraftPos.x + horizDir.x * dist;
                float z = aircraftPos.z + horizDir.z * dist;
                if (!TerrainHeightFieldCache.TryGetGroundHeight(x, z, out float groundY))
                    continue;

                if (groundY - sea < minProminenceM)
                    continue;

                Vector3 world = new Vector3(x, groundY, z);
                Vector3 to = world - camPos;
                float horiz = new Vector2(to.x, to.z).magnitude;
                if (horiz < 1f)
                    continue;

                float elevDeg = Mathf.Atan2(to.y, horiz) * Mathf.Rad2Deg;
                if (elevDeg > bestElev)
                {
                    bestElev = elevDeg;
                    bestWorld = world;
                }
            }

            dominantWorld = bestWorld;
            return true;
        }

        internal static ProfileResult FindClosestMountain(
            Camera cam,
            Vector3 aircraftPos,
            float halfAngleDeg,
            int azimuthSamples,
            float maxRange,
            float stepMeters,
            float minProminenceM)
        {
            var result = new ProfileResult
            {
                Count = 0,
                ClosestHorizMeters = float.MaxValue,
                ClosestAzimuthDeg = 0f,
            };

            if (cam == null || azimuthSamples < 2)
                return result;

            Vector3 forwardFlat = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (forwardFlat.sqrMagnitude < 1e-6f)
                return result;
            forwardFlat.Normalize();

            Vector3 camPos = cam.transform.position;
            float sea = Datum.LocalSeaY;
            int n = Mathf.Clamp(azimuthSamples, 2, MaxPoints);
            float step = Mathf.Max(50f, stepMeters);
            float maxR = Mathf.Max(step, maxRange);
            float fineFrac = TerrainSilhouetteHudPlugin.RidgeFineRefineFraction.Value;

            for (int i = 0; i < n; i++)
            {
                float t = (i / (float)(n - 1)) - 0.5f;
                float yaw = t * halfAngleDeg * 2f;
                Vector3 horizDir = (Quaternion.AngleAxis(yaw, Vector3.up) * forwardFlat).normalized;

                if (!TryFindDominantTerrainPointAlongRay(
                    aircraftPos,
                    camPos,
                    horizDir,
                    sea,
                    maxR,
                    step,
                    minProminenceM,
                    fineFrac,
                    out Vector3 bestWorld))
                    continue;

                float rangeFromAc = new Vector2(
                    bestWorld.x - aircraftPos.x,
                    bestWorld.z - aircraftPos.z).magnitude;

                if (rangeFromAc < result.ClosestHorizMeters)
                {
                    result.ClosestHorizMeters = rangeFromAc;
                    result.ClosestAzimuthDeg = yaw;
                }
            }

            if (result.ClosestHorizMeters == float.MaxValue)
                result.ClosestHorizMeters = -1f;

            return result;
        }

        internal static ProfileResult BuildRidgeWorld(
            Camera cam,
            Vector3 aircraftPos,
            float halfAngleDeg,
            float centerAzimuthDeg,
            int azimuthSamples,
            float maxRange,
            float stepMeters,
            float minProminenceM,
            Vector3[] worldOut,
            bool[] validOut,
            out int sampleCount)
        {
            sampleCount = 0;
            var result = new ProfileResult
            {
                Count = 0,
                ClosestHorizMeters = float.MaxValue,
                ClosestAzimuthDeg = 0f,
            };

            if (cam == null || worldOut == null || validOut == null || azimuthSamples < 2)
                return result;

            Vector3 forwardFlat = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (forwardFlat.sqrMagnitude < 1e-6f)
                return result;
            forwardFlat.Normalize();

            Vector3 camPos = cam.transform.position;
            float sea = Datum.LocalSeaY;
            int n = Mathf.Clamp(azimuthSamples, 2, MaxPoints);
            sampleCount = n;
            float step = Mathf.Max(50f, stepMeters);
            float maxR = Mathf.Max(step, maxRange);
            float fineFrac = TerrainSilhouetteHudPlugin.RidgeFineRefineFraction.Value;

            for (int i = 0; i < n; i++)
            {
                validOut[i] = false;
                if (i >= worldOut.Length || i >= validOut.Length)
                    continue;

                float t = (i / (float)(n - 1)) - 0.5f;
                float yaw = centerAzimuthDeg + t * halfAngleDeg * 2f;
                Vector3 horizDir = (Quaternion.AngleAxis(yaw, Vector3.up) * forwardFlat).normalized;

                if (!TryFindDominantTerrainPointAlongRay(
                    aircraftPos,
                    camPos,
                    horizDir,
                    sea,
                    maxR,
                    step,
                    minProminenceM,
                    fineFrac,
                    out Vector3 bestWorld))
                    continue;

                float rangeFromAc = new Vector2(
                    bestWorld.x - aircraftPos.x,
                    bestWorld.z - aircraftPos.z).magnitude;
                if (rangeFromAc < result.ClosestHorizMeters)
                {
                    result.ClosestHorizMeters = rangeFromAc;
                    result.ClosestAzimuthDeg = yaw;
                }

                worldOut[i] = bestWorld;
                validOut[i] = true;
            }

            result.Count = n;
            if (result.ClosestHorizMeters == float.MaxValue)
                result.ClosestHorizMeters = -1f;

            return result;
        }

        /// <summary>One dense entry per azimuth slot (fixes smoothing when validity gaps exist).</summary>
        internal static void ProjectRidgeToScreenDense(
            Camera cam,
            Vector3[] worldPoints,
            bool[] valid,
            int count,
            Vector2[] screenDenseOut,
            bool[] screenDenseValidOut)
        {
            if (cam == null || worldPoints == null || valid == null || screenDenseOut == null || screenDenseValidOut == null || count <= 0)
                return;

            int n = Mathf.Min(count, worldPoints.Length, valid.Length, screenDenseOut.Length, screenDenseValidOut.Length);
            for (int i = 0; i < n; i++)
            {
                screenDenseValidOut[i] = false;
                if (!valid[i])
                    continue;

                Vector3 screen = cam.WorldToScreenPoint(worldPoints[i]);
                if (screen.z <= 0.5f)
                    continue;

                screenDenseOut[i] = new Vector2(screen.x, screen.y);
                screenDenseValidOut[i] = true;
            }
        }

        internal static int ProjectRidgeToScreen(
            Camera cam,
            Vector3[] worldPoints,
            bool[] valid,
            int count,
            Vector2[] screenOut)
        {
            if (cam == null || worldPoints == null || valid == null || screenOut == null || count <= 0)
                return 0;

            int n = Mathf.Min(count, worldPoints.Length, valid.Length, screenOut.Length);
            int outCount = 0;
            for (int i = 0; i < n; i++)
            {
                if (!valid[i])
                    continue;

                Vector3 screen = cam.WorldToScreenPoint(worldPoints[i]);
                if (screen.z <= 0.5f)
                    continue;

                screenOut[outCount++] = new Vector2(screen.x, screen.y);
            }

            return outCount;
        }
    }
}
