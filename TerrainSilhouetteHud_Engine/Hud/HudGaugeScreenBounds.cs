using UnityEngine;

namespace TerrainSilhouetteHud_Engine.Hud
{
    /// <summary>
    /// Fixed-width screen band centered on FlightHud (stable when rolling/turning).
    /// </summary>
    internal static class HudGaugeScreenBounds
    {
        private static float _measuredHalfWidth = -1f;

        internal static bool TryGetSilhouetteBand(out float minX, out float maxX)
        {
            float centerX = ResolveHudCenterScreenX();
            float halfWidth = ResolveBandHalfWidthPixels();
            float shrink = Mathf.Clamp(TerrainSilhouetteHudPlugin.ScreenBandShrinkRatio.Value, 0.5f, 1f);
            halfWidth *= shrink;

            minX = centerX - halfWidth;
            maxX = centerX + halfWidth;

            float inset = Mathf.Max(0f, TerrainSilhouetteHudPlugin.ScreenBandInsetPixels.Value);
            minX += inset;
            maxX -= inset;

            if (maxX <= minX + 16f)
                return TryGetFallbackBand(out minX, out maxX);

            return true;
        }

        internal static void InvalidateCache()
        {
            _measuredHalfWidth = -1f;
        }

        private static float ResolveHudCenterScreenX()
        {
            FlightHud fh = SceneSingleton<FlightHud>.i;
            if (fh != null)
            {
                Transform hudCenter = fh.GetHUDCenter();
                if (hudCenter != null)
                    return hudCenter.position.x;
            }

            return Screen.width * 0.5f;
        }

        private static float ResolveBandHalfWidthPixels()
        {
            float configured = TerrainSilhouetteHudPlugin.ScreenBandHalfWidthPixels.Value;
            if (configured > 8f)
                return configured;

            if (_measuredHalfWidth > 8f)
                return _measuredHalfWidth;

            if (TryMeasureGaugeHalfSpan(out float measured))
                _measuredHalfWidth = measured;

            if (_measuredHalfWidth > 8f)
                return _measuredHalfWidth;

            return Screen.width * 0.16f;
        }

        private static bool TryMeasureGaugeHalfSpan(out float halfWidth)
        {
            halfWidth = 0f;
            FuelGauge fuel = Object.FindObjectOfType<FuelGauge>();
            ThrottleGauge throttle = Object.FindObjectOfType<ThrottleGauge>();
            if (fuel == null || throttle == null)
                return false;

            if (!TryGetHierarchyInnerEdgeX(fuel.transform, isLeftGauge: true, out float innerLeft))
                return false;

            if (!TryGetHierarchyInnerEdgeX(throttle.transform, isLeftGauge: false, out float innerRight))
                return false;

            float span = innerRight - innerLeft;
            if (span < 32f)
                return false;

            halfWidth = span * 0.5f;
            return true;
        }

        private static bool TryGetHierarchyInnerEdgeX(Transform root, bool isLeftGauge, out float edgeX)
        {
            edgeX = isLeftGauge ? float.NegativeInfinity : float.PositiveInfinity;
            if (root == null)
                return false;

            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(false);
            bool any = false;
            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform rt = rects[i];
                if (rt == null || !rt.gameObject.activeInHierarchy)
                    continue;

                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                for (int c = 0; c < 4; c++)
                {
                    float x = corners[c].x;
                    if (isLeftGauge)
                        edgeX = Mathf.Max(edgeX, x);
                    else
                        edgeX = Mathf.Min(edgeX, x);
                    any = true;
                }
            }

            return any && !float.IsInfinity(Mathf.Abs(edgeX));
        }

        private static bool TryGetFallbackBand(out float minX, out float maxX)
        {
            float w = Mathf.Max(640f, Screen.width);
            float centerX = w * 0.5f;
            float half = w * 0.16f * Mathf.Clamp(TerrainSilhouetteHudPlugin.ScreenBandShrinkRatio.Value, 0.5f, 1f);
            minX = centerX - half;
            maxX = centerX + half;
            return maxX > minX + 16f;
        }
    }
}
