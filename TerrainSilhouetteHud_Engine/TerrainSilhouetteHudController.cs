using TerrainSilhouetteHud_Engine.Hud;
using UnityEngine;

namespace TerrainSilhouetteHud_Engine
{
    internal static class TerrainSilhouetteHudController
    {
        private static TerrainSilhouetteGpuPipeline _gpu;
        private static TerrainSilhouetteOverlayView _overlay;
        private static TerrainPolylineView _lineView;

        private const int ScratchSize = 128;
        private static readonly Vector2[] _scratch = new Vector2[ScratchSize];
        private static readonly Vector2[] _smooth = new Vector2[ScratchSize];
        private static readonly Vector2[] _clipped = new Vector2[ScratchSize];
        private static readonly Vector2[] _denseA = new Vector2[ScratchSize];
        private static readonly Vector2[] _denseB = new Vector2[ScratchSize];
        private static readonly bool[] _denseValidA = new bool[ScratchSize];
        private static readonly bool[] _denseValidB = new bool[ScratchSize];
        private static int _smoothCount;
        private static int _clippedCount;
        private static float _lastSampleTime = -999f;
        private static bool _lastCritical;

        internal static void Tick()
        {
            if (!TerrainSilhouetteHudPlugin.Enabled.Value)
            {
                HideAll();
                return;
            }

            if (!AllowHud())
            {
                HideAll();
                return;
            }

            Aircraft aircraft;
            if (!GameManager.GetLocalAircraft(out aircraft) || aircraft == null || aircraft.disabled || aircraft.cockpit == null)
            {
                HideAll();
                return;
            }

            CameraStateManager csm = SceneSingleton<CameraStateManager>.i;
            if (csm == null || csm.mainCamera == null)
            {
                HideAll();
                return;
            }

            Camera cam = csm.mainCamera;
            var mode = TerrainSilhouetteHudPlugin.RenderMode.Value;

            if (mode == TerrainRenderMode.Heightmap)
            {
                TickHeightmap(cam, aircraft);
                return;
            }

            if (mode == TerrainRenderMode.Gpu)
            {
                if (TerrainSilhouetteAssets.IsGpuReady)
                    TickGpu(cam);
                else if (TerrainSilhouetteHudPlugin.FallbackToLegacyCpu.Value)
                    TickHeightmap(cam, aircraft);
                else
                    HideAll();
                return;
            }

            TickLegacyCpu(cam, aircraft);
        }

        private static void TickHeightmap(Camera cam, Aircraft aircraft)
        {
            HideOverlay();

            Vector3 acPos = aircraft.transform.position;
            if (TerrainRidgeCache.NeedsRebuild(acPos))
                TerrainRidgeCache.Rebuild(cam, acPos);

            if (!TerrainCollisionAdvisor.TryEvaluate(aircraft, cam, out TerrainThreatAssessment threat)
                || !threat.ShouldShow)
            {
                HideLine();
                return;
            }

            _lastCritical = threat.IsCritical;

            TerrainRidgeCache.ProjectForwardDense(cam, _denseA, _denseValidA, out int azimuthSlots);

            float screenSmooth = TerrainSilhouetteHudPlugin.HeightmapSmoothTime.Value;
            Vector2[] curPts;
            bool[] curVal;
            if (screenSmooth <= 0.001f)
            {
                curPts = _denseA;
                curVal = _denseValidA;
            }
            else
            {
                SmoothDenseTemporal(_denseA, _denseValidA, _denseB, _denseValidB, azimuthSlots, screenSmooth);
                curPts = _denseB;
                curVal = _denseValidB;
            }

            int neighborPasses = Mathf.Clamp(TerrainSilhouetteHudPlugin.RidgeScreenNeighborSmoothPasses.Value, 0, 6);
            if (neighborPasses > 0)
            {
                Vector2[] nxtPts = curPts == _denseA ? _denseB : _denseA;
                bool[] nxtVal = curVal == _denseValidA ? _denseValidB : _denseValidA;
                float blend = Mathf.Clamp01(TerrainSilhouetteHudPlugin.RidgeScreenNeighborBlend.Value);

                for (int p = 0; p < neighborPasses; p++)
                {
                    SmoothDenseNeighbors(curPts, curVal, nxtPts, nxtVal, azimuthSlots, blend);
                    Vector2[] swp = curPts;
                    curPts = nxtPts;
                    nxtPts = swp;

                    bool[] swv = curVal;
                    curVal = nxtVal;
                    nxtVal = swv;
                }
            }

            CollapseDenseToPolyline(curPts, curVal, azimuthSlots);

            DrawForwardSilhouette(_lastCritical);
        }

        private static void SmoothDenseTemporal(
            Vector2[] src,
            bool[] srcValid,
            Vector2[] dest,
            bool[] destValid,
            int slotCount,
            float smoothTime)
        {
            int m = Mathf.Min(slotCount, ScratchSize);
            float smooth = Mathf.Max(0f, smoothTime);
            float t = smooth <= 0f ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime / smooth);

            for (int i = 0; i < m; i++)
            {
                if (!srcValid[i])
                {
                    destValid[i] = false;
                    continue;
                }

                Vector2 tgt = src[i];
                if (!destValid[i])
                    dest[i] = tgt;
                else
                    dest[i] = Vector2.Lerp(dest[i], tgt, t);

                destValid[i] = true;
            }

            for (int i = m; i < ScratchSize; i++)
                destValid[i] = false;
        }

        private static void SmoothDenseNeighbors(
            Vector2[] src,
            bool[] srcValid,
            Vector2[] dst,
            bool[] dstValid,
            int slotCount,
            float neighborBlend)
        {
            int m = Mathf.Min(slotCount, ScratchSize);
            float w = Mathf.Clamp01(neighborBlend);
            for (int i = 0; i < m; i++)
                dstValid[i] = false;

            for (int i = 0; i < m; i++)
            {
                if (!srcValid[i])
                    continue;

                Vector2 raw = src[i];
                if (w <= 0.001f)
                {
                    dst[i] = raw;
                    dstValid[i] = true;
                    continue;
                }

                Vector2 sum = raw;
                int c = 1;
                if (i > 0 && srcValid[i - 1])
                {
                    sum += src[i - 1];
                    c++;
                }

                if (i < m - 1 && srcValid[i + 1])
                {
                    sum += src[i + 1];
                    c++;
                }

                Vector2 avg = sum / c;
                dst[i] = Vector2.Lerp(raw, avg, w);
                dstValid[i] = true;
            }

            for (int i = m; i < ScratchSize; i++)
                dstValid[i] = false;
        }

        private static void CollapseDenseToPolyline(Vector2[] dense, bool[] valid, int slotCount)
        {
            _smoothCount = 0;
            int m = Mathf.Min(slotCount, dense.Length, valid.Length, _smooth.Length);
            int maxBridge = Mathf.Clamp(TerrainSilhouetteHudPlugin.RidgeMaxGapBridgeSlots.Value, 0, 8);

            int i = 0;
            while (i < m && _smoothCount < _smooth.Length)
            {
                if (!valid[i])
                {
                    i++;
                    continue;
                }

                int next = i + 1;
                while (next < m && !valid[next])
                    next++;

                if (next < m && next - i > 1 && next - i - 1 <= maxBridge)
                {
                    int span = next - i;
                    _smooth[_smoothCount++] = dense[i];
                    for (int k = 1; k < span && _smoothCount < _smooth.Length; k++)
                    {
                        float t = k / (float)span;
                        _smooth[_smoothCount++] = Vector2.Lerp(dense[i], dense[next], t);
                    }

                    i = next;
                    continue;
                }

                _smooth[_smoothCount++] = dense[i];
                i++;
            }
        }

        private static void DrawForwardSilhouette(bool critical)
        {
            if (!EnsureLineView())
                return;

            if (_smoothCount < 2)
            {
                HideLine();
                return;
            }

            if (!HudGaugeScreenBounds.TryGetSilhouetteBand(out float minX, out float maxX))
            {
                HideLine();
                return;
            }

            float gap = TerrainSilhouetteHudPlugin.HeightmapMaxScreenGap.Value;
            _clippedCount = TerrainPolylineClipper.ClipHorizontalBand(
                _smooth, _smoothCount, minX, maxX, _clipped, gap);

            if (_clippedCount < 2)
            {
                HideLine();
                return;
            }

            Color line = critical ? ResolveWarningColor() : ResolveHudColor();
            line.a *= Mathf.Clamp01(TerrainSilhouetteHudPlugin.HeightmapLineAlpha.Value);
            _lineView.SetVisible(true);
            _lineView.SetPolyline(
                _clipped,
                _clippedCount,
                line,
                TerrainSilhouetteHudPlugin.HeightmapLineThickness.Value,
                gap,
                minX,
                maxX);
        }

        private static void TickGpu(Camera cam)
        {
            HideLine();

            if (_gpu == null)
                _gpu = new TerrainSilhouetteGpuPipeline();

            if (!TerrainSilhouetteAssets.IsGpuReady)
            {
                HideOverlay();
                return;
            }

            _gpu.EnsureResources(cam);
            Color hud = ResolveHudColor();
            Color near = ResolveNearColor();
            near.a = hud.a;
            _gpu.RenderFrame(cam, hud, near);

            if (!EnsureOverlay())
                return;

            RenderTexture tex = _gpu.OutputTexture;
            if (tex == null)
            {
                HideOverlay();
                return;
            }

            _overlay.SetVisible(true);
            _overlay.SetTexture(tex, TerrainSilhouetteHudPlugin.OverlayAlpha.Value);
        }

        private static void TickLegacyCpu(Camera cam, Aircraft aircraft)
        {
            HideOverlay();

            Vector3 acPos = aircraft.transform.position;
            if (TerrainRidgeCache.NeedsRebuild(acPos))
                TerrainRidgeCache.Rebuild(cam, acPos);

            if (!TerrainCollisionAdvisor.TryEvaluate(aircraft, cam, out TerrainThreatAssessment threat)
                || !threat.ShouldShow)
            {
                HideLine();
                return;
            }

            _lastCritical = threat.IsCritical;

            float interval = Mathf.Max(0f, TerrainSilhouetteHudPlugin.SampleIntervalSeconds.Value);
            if (interval > 0f && Time.unscaledTime - _lastSampleTime < interval)
            {
                DrawForwardSilhouette(_lastCritical);
                return;
            }

            _lastSampleTime = Time.unscaledTime;
            int mask = TerrainSilhouetteHudPlugin.TerrainLayerMask.Value;
            float maxRange = Mathf.Max(500f, TerrainSilhouetteHudPlugin.MaxRangeMeters.Value);

            TerrainProfileSampler.SampleWideProfile(
                cam, mask, maxRange,
                TerrainSilhouetteHudPlugin.AzimuthHalfAngleDeg.Value,
                TerrainSilhouetteHudPlugin.WideSampleCount.Value,
                TerrainSilhouetteHudPlugin.PitchScanStepDeg.Value,
                _scratch, out int count);

            _smoothCount = SmoothPoints(
                _scratch,
                _smooth,
                count,
                TerrainSilhouetteHudPlugin.SmoothTime.Value,
                ref _smoothCount);

            DrawForwardSilhouette(_lastCritical);
        }

        internal static void Shutdown()
        {
            _gpu?.Teardown();
            _gpu = null;
            _overlay?.Dispose();
            _overlay = null;
            TerrainRidgeCache.Clear();
            TerrainHeightFieldCache.Clear();
            HudGaugeScreenBounds.InvalidateCache();
            if (_lineView != null)
            {
                _lineView.Dispose();
                _lineView = null;
            }
        }

        private static int SmoothPoints(
            Vector2[] source,
            Vector2[] dest,
            int count,
            float smoothTime,
            ref int previousCount)
        {
            if (count <= 0)
                return 0;

            float smooth = Mathf.Max(0f, smoothTime);
            if (smooth <= 0f || previousCount <= 0)
            {
                for (int i = 0; i < count; i++)
                    dest[i] = source[i];
                return count;
            }

            float t = 1f - Mathf.Exp(-Time.unscaledDeltaTime / smooth);
            int max = Mathf.Min(count, dest.Length);
            for (int i = 0; i < max; i++)
            {
                if (i >= previousCount)
                    dest[i] = source[i];
                else
                    dest[i] = Vector2.Lerp(dest[i], source[i], t);
            }

            return count;
        }

        private static bool AllowHud()
        {
            GameState state = GameManager.gameState;
            if (state != GameState.SinglePlayer && state != GameState.Multiplayer)
                return false;

            if (TerrainSilhouetteHudPlugin.ShowOnlyWhenFlightControlsEnabled.Value && !GameManager.flightControlsEnabled)
                return false;

            FlightHud fh = SceneSingleton<FlightHud>.i;
            if (fh == null)
                return false;

            Canvas flightCanvas = fh.GetComponent<Canvas>();
            if (flightCanvas != null && !flightCanvas.gameObject.activeSelf)
                return false;

            return true;
        }

        private static Transform GetFlightHudCanvasTransform()
        {
            FlightHud fh = SceneSingleton<FlightHud>.i;
            if (fh == null)
                return null;
            Canvas c = fh.GetComponent<Canvas>();
            return c != null ? c.transform : fh.transform;
        }

        private static bool EnsureOverlay()
        {
            Transform parent = GetFlightHudCanvasTransform();
            if (parent == null)
                return false;

            if (_overlay != null && !_overlay.NeedsRebuild(parent))
                return true;

            _overlay?.Dispose();
            _overlay = new TerrainSilhouetteOverlayView(parent);
            return true;
        }

        private static bool EnsureLineView()
        {
            Transform parent = GetFlightHudCanvasTransform();
            if (parent == null)
                return false;

            int maxSeg = Mathf.Max(
                TerrainSilhouetteHudPlugin.HeightmapAzimuthSamples.Value,
                TerrainSilhouetteHudPlugin.WideSampleCount.Value) + 4;

            if (_lineView == null || _lineView.NeedsRebuild(parent))
            {
                _lineView?.Dispose();
                _lineView = new TerrainPolylineView(parent, "TerrainSilhouetteForward", maxSeg);
            }

            return true;
        }

        private static void HideAll()
        {
            HideOverlay();
            HideLine();
        }

        private static void HideOverlay()
        {
            if (_overlay != null)
                _overlay.SetVisible(false);
        }

        private static void HideLine()
        {
            if (_lineView != null)
                _lineView.SetVisible(false);
        }

        private static Color ResolveHudColor()
        {
            Color baseColor;
            if (TerrainSilhouetteHudPlugin.UseHudColor.Value)
            {
                baseColor = new Color(
                    PlayerSettings.hudColorR / 255f,
                    PlayerSettings.hudColorG / 255f,
                    PlayerSettings.hudColorB / 255f,
                    1f);
            }
            else if (!ColorUtility.TryParseHtmlString(TerrainSilhouetteHudPlugin.LineColorHex.Value, out baseColor))
            {
                baseColor = Color.green;
            }

            return baseColor;
        }

        private static Color ResolveWarningColor()
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(TerrainSilhouetteHudPlugin.WarningColorHex.Value, out c))
                c = new Color(1f, 0.2f, 0.1f, 1f);
            return c;
        }

        private static Color ResolveNearColor()
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(TerrainSilhouetteHudPlugin.NearColorHex.Value, out c))
                c = new Color(1f, 0.35f, 0.2f, 1f);
            return c;
        }
    }
}
