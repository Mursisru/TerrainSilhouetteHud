using UnityEngine;

namespace TerrainSilhouetteHud_Engine
{
    internal struct TerrainThreatAssessment
    {
        public bool ShouldShow;
        public bool IsCritical;
        public float TimeToImpactSeconds;
        public Vector3 ThreatSurfacePoint;
        public float SummitAltitudeMeters;
    }

    internal static class TerrainCollisionAdvisor
    {
        private const int SummitSampleCount = 12;

        internal static bool TryEvaluate(Aircraft aircraft, Camera cam, out TerrainThreatAssessment assessment)
        {
            assessment = default;
            if (aircraft == null || cam == null || aircraft.cockpit == null)
                return false;

            if (TerrainSilhouetteHudPlugin.WarningRespectNight.Value
                && TerrainSilhouetteHudPlugin.NightOnly.Value
                && !TerrainProfileSampler.IsNightTime())
            {
                return true;
            }

            Vector3 aircraftPos = aircraft.transform.position;
            Rigidbody rb = aircraft.cockpit.rb;
            if (rb == null)
                return false;

            Vector3 velocity = rb.velocity;
            float speed = velocity.magnitude;
            if (speed < TerrainSilhouetteHudPlugin.WarningMinSpeedMps.Value)
                return true;

            Vector3 velFlat = Vector3.ProjectOnPlane(velocity, Vector3.up);
            if (velFlat.sqrMagnitude < 1f)
                return true;
            velFlat.Normalize();

            if (!TerrainRidgeCache.TryFindBestThreat(
                aircraftPos,
                velFlat,
                speed,
                out Vector3 threatPoint,
                out float summitY,
                out float timeToImpact))
            {
                return true;
            }

            float belowMargin = TerrainSilhouetteHudPlugin.BelowPeakMarginMeters.Value;
            if (aircraftPos.y >= summitY - belowMargin)
                return true;

            float maxTime = TerrainSilhouetteHudPlugin.WarningMaxTimeSeconds.Value;
            if (timeToImpact <= 0f || timeToImpact > maxTime)
                return true;

            assessment.ShouldShow = true;
            assessment.TimeToImpactSeconds = timeToImpact;
            assessment.ThreatSurfacePoint = threatPoint;
            assessment.SummitAltitudeMeters = summitY;
            assessment.IsCritical = timeToImpact < TerrainSilhouetteHudPlugin.WarningCriticalTimeSeconds.Value;
            return true;
        }

        internal static float EstimateSummitAltitude(float centerX, float centerZ)
        {
            float radius = Mathf.Max(100f, TerrainSilhouetteHudPlugin.SummitSearchRadiusMeters.Value);
            float maxY = Datum.LocalSeaY;

            if (TerrainHeightFieldCache.TryGetGroundHeight(centerX, centerZ, out float centerY))
                maxY = Mathf.Max(maxY, centerY);

            for (int i = 0; i < SummitSampleCount; i++)
            {
                float angle = (i / (float)SummitSampleCount) * Mathf.PI * 2f;
                float x = centerX + Mathf.Cos(angle) * radius;
                float z = centerZ + Mathf.Sin(angle) * radius;
                if (TerrainHeightFieldCache.TryGetGroundHeight(x, z, out float y))
                    maxY = Mathf.Max(maxY, y);

                float x2 = centerX + Mathf.Cos(angle) * radius * 0.5f;
                float z2 = centerZ + Mathf.Sin(angle) * radius * 0.5f;
                if (TerrainHeightFieldCache.TryGetGroundHeight(x2, z2, out float y2))
                    maxY = Mathf.Max(maxY, y2);
            }

            return maxY;
        }
    }
}
