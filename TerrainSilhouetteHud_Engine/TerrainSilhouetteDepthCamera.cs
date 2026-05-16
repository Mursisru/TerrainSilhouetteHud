using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TerrainSilhouetteHud_Engine
{
    /// <summary>
    /// Off-screen camera synced to the cockpit view; renders only terrain layers into an RT.
    /// </summary>
    internal sealed class TerrainSilhouetteDepthCamera
    {
        private GameObject _root;
        private Camera _cam;
        private RenderTexture _terrainRt;
        private RenderTexture _edgeRt;
        private int _rtW;
        private int _rtH;

        internal Camera Camera => _cam;
        internal RenderTexture TerrainRt => _terrainRt;
        internal RenderTexture EdgeRt => _edgeRt;

        internal void Ensure(Camera mainTemplate, float resolutionScale, int depthBits)
        {
            int w = Mathf.Max(64, (int)(Screen.width * resolutionScale));
            int h = Mathf.Max(64, (int)(Screen.height * resolutionScale));
            if (_cam != null && _terrainRt != null && (_rtW != w || _rtH != h))
                ReleaseTextures();

            if (_root == null)
            {
                _root = new GameObject("TerrainSilhouetteHud_WorldCamera");
                Object.DontDestroyOnLoad(_root);
                _cam = _root.AddComponent<Camera>();
                ConfigureCamera(_cam, mainTemplate);

                var uacd = _cam.GetUniversalAdditionalCameraData();
                uacd.renderType = CameraRenderType.Base;
            }

            if (_terrainRt == null || _rtW != w || _rtH != h)
            {
                ReleaseTextures();
                _rtW = w;
                _rtH = h;
                int depth = Mathf.Clamp(depthBits, 16, 32);
                _terrainRt = new RenderTexture(w, h, depth, RenderTextureFormat.ARGB32)
                {
                    name = "TerrainSilhouette_Source",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _terrainRt.Create();

                _edgeRt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
                {
                    name = "TerrainSilhouette_Edge",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _edgeRt.Create();

                _cam.targetTexture = _terrainRt;
            }
        }

        internal void SyncFrom(Camera main)
        {
            if (_cam == null || main == null)
                return;

            Transform t = _cam.transform;
            t.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            _cam.fieldOfView = main.fieldOfView;
            _cam.nearClipPlane = main.nearClipPlane;
            _cam.farClipPlane = main.farClipPlane;
            _cam.aspect = main.aspect;
            _cam.orthographic = main.orthographic;
            if (main.orthographic)
                _cam.orthographicSize = main.orthographicSize;

            _cam.cullingMask = TerrainSilhouetteHudPlugin.TerrainLayerMask.Value;
        }

        internal void Render()
        {
            if (_cam == null || _terrainRt == null)
                return;

            _cam.Render();
        }

        internal void Teardown()
        {
            if (_cam != null)
                _cam.targetTexture = null;
            ReleaseTextures();
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _cam = null;
            }
        }

        private void ReleaseTextures()
        {
            if (_terrainRt != null)
            {
                _terrainRt.Release();
                Object.Destroy(_terrainRt);
                _terrainRt = null;
            }

            if (_edgeRt != null)
            {
                _edgeRt.Release();
                Object.Destroy(_edgeRt);
                _edgeRt = null;
            }

            _rtW = 0;
            _rtH = 0;
        }

        private static void ConfigureCamera(Camera cam, Camera mainTemplate)
        {
            cam.enabled = false;
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.depth = -120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.forceIntoRenderTexture = true;
            cam.depthTextureMode = DepthTextureMode.Depth;

            if (mainTemplate != null)
            {
                cam.nearClipPlane = Mathf.Max(0.01f, mainTemplate.nearClipPlane);
                cam.farClipPlane = Mathf.Max(cam.nearClipPlane + 1f, mainTemplate.farClipPlane);
                cam.useOcclusionCulling = mainTemplate.useOcclusionCulling;
            }
            else
            {
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 50000f;
                cam.useOcclusionCulling = true;
            }
        }
    }
}
