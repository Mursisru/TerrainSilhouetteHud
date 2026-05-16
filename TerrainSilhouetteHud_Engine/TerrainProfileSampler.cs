using System.Collections.Generic;
using UnityEngine;

namespace TerrainSilhouetteHud_Engine
{
    internal static class TerrainProfileSampler
    {
        private const int MaxPoints = 96;

        internal struct SampleResult
        {
            public int Count;
            public float ClosestTerrainMeters;
            public float ClosestAzimuthDeg;
        }

        internal static SampleResult SampleWideProfile(
            Camera cam,
            int terrainMask,
            float maxRange,
            float halfAngleDeg,
            int sampleCount,
            float pitchStepDeg,
            Vector2[] screenScratch,
            out int validCount)
        {
            return SampleFan(cam, terrainMask, maxRange, halfAngleDeg, 0f, sampleCount, pitchStepDeg, screenScratch, out validCount);
        }

        internal static SampleResult SampleNearProfile(
            Camera cam,
            int terrainMask,
            float maxRange,
            float halfAngleDeg,
            float centerAzimuthDeg,
            int sampleCount,
            float pitchStepDeg,
            Vector2[] screenScratch,
            out int validCount)
        {
            return SampleFan(cam, terrainMask, maxRange, halfAngleDeg, centerAzimuthDeg, sampleCount, pitchStepDeg, screenScratch, out validCount);
        }

        private static SampleResult SampleFan(
            Camera cam,
            int terrainMask,
            float maxRange,
            float halfAngleDeg,
            float centerAzimuthOffsetDeg,
            int sampleCount,
            float pitchStepDeg,
            Vector2[] screenScratch,
            out int validCount)
        {
            validCount = 0;
            var result = new SampleResult
            {
                Count = 0,
                ClosestTerrainMeters = float.MaxValue,
                ClosestAzimuthDeg = 0f,
            };

            if (cam == null || sampleCount < 2 || screenScratch == null || screenScratch.Length < sampleCount)
                return result;

            Vector3 origin = cam.transform.position;
            Vector3 forwardFlat = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (forwardFlat.sqrMagnitude < 1e-6f)
                return result;
            forwardFlat.Normalize();

            Vector3 rightFlat = Vector3.Cross(Vector3.up, forwardFlat).normalized;
            float step = pitchStepDeg > 0.05f ? pitchStepDeg : 0.65f;

            int n = Mathf.Clamp(sampleCount, 2, MaxPoints);
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0f : (i / (float)(n - 1)) - 0.5f;
                float yaw = centerAzimuthOffsetDeg + t * halfAngleDeg * 2f;
                Vector3 horizontalDir = (Quaternion.AngleAxis(yaw, Vector3.up) * forwardFlat).normalized;
                Vector3 axisRight = Vector3.Cross(Vector3.up, horizontalDir).normalized;

                if (!TryFindSilhouettePoint(origin, horizontalDir, axisRight, maxRange, terrainMask, step, out Vector3 worldPoint, out float distance))
                    continue;

                if (distance < result.ClosestTerrainMeters)
                {
                    result.ClosestTerrainMeters = distance;
                    result.ClosestAzimuthDeg = yaw;
                }

                Vector3 screen = cam.WorldToScreenPoint(worldPoint);
                if (screen.z <= 0.5f)
                    continue;

                screenScratch[validCount++] = new Vector2(screen.x, screen.y);
            }

            result.Count = validCount;
            if (result.ClosestTerrainMeters == float.MaxValue)
                result.ClosestTerrainMeters = -1f;

            return result;
        }

        private static bool TryFindSilhouettePoint(
            Vector3 origin,
            Vector3 horizontalDir,
            Vector3 axisRight,
            float maxRange,
            int terrainMask,
            float pitchStepDeg,
            out Vector3 worldPoint,
            out float distance)
        {
            worldPoint = default;
            distance = -1f;

            float bestElev = -999f;
            bool found = false;

            for (float pitch = 1.5f; pitch >= -38f; pitch -= pitchStepDeg)
            {
                Vector3 dir = (Quaternion.AngleAxis(-pitch, axisRight) * horizontalDir).normalized;
                RaycastHit hit;
                if (!Physics.Raycast(origin, dir, out hit, maxRange, terrainMask))
                    continue;

                Vector3 toHit = hit.point - origin;
                float horiz = new Vector2(toHit.x, toHit.z).magnitude;
                if (horiz < 1f)
                    continue;

                float elev = Mathf.Atan2(toHit.y, horiz) * Mathf.Rad2Deg;
                if (elev > bestElev)
                {
                    bestElev = elev;
                    worldPoint = hit.point;
                    distance = hit.distance;
                    found = true;
                }
            }

            return found;
        }

        internal static bool IsNightTime()
        {
            if (!TerrainSilhouetteHudPlugin.NightOnly.Value)
                return true;

            LevelInfo level = NetworkSceneSingleton<LevelInfo>.i;
            if (level == null)
                return false;

            float tod = level.timeOfDay;
            float nightStart = TerrainSilhouetteHudPlugin.NightStartHour.Value;
            float nightEnd = TerrainSilhouetteHudPlugin.NightEndHour.Value;

            if (nightStart > nightEnd)
                return tod >= nightStart || tod <= nightEnd;

            return tod >= nightStart && tod <= nightEnd;
        }

        internal static float TryGetAglMeters(Aircraft aircraft)
        {
            if (aircraft == null)
                return -1f;

            if (aircraft.radarAlt > 0.05f)
                return aircraft.radarAlt;

            Vector3 origin = aircraft.transform.position;
            RaycastHit hit;
            if (Physics.Linecast(origin, origin - Vector3.up * 10000f, out hit, TerrainSilhouetteHudPlugin.TerrainLayerMask.Value))
                return Mathf.Max(0f, hit.distance - aircraft.definition.spawnOffset.y);

            return -1f;
        }
    }
}
