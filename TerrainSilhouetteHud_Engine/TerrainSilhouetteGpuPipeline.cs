using UnityEngine;

namespace TerrainSilhouetteHud_Engine
{
    internal sealed class TerrainSilhouetteGpuPipeline
    {
        private readonly TerrainSilhouetteDepthCamera _depthCam = new TerrainSilhouetteDepthCamera();
        private Material _edgeMaterial;
        private Material _depthVisMaterial;
        private bool _loggedFallback;

        internal RenderTexture OutputTexture => _depthCam.EdgeRt;

        internal bool IsReady => TerrainSilhouetteAssets.IsGpuReady;

        internal void EnsureResources(Camera main)
        {
            _depthCam.Ensure(
                main,
                TerrainSilhouetteHudPlugin.GpuResolutionScale.Value,
                TerrainSilhouetteHudPlugin.RtDepthBits.Value);

            if (_edgeMaterial == null && TerrainSilhouetteAssets.EdgeShader != null)
            {
                _edgeMaterial = new Material(TerrainSilhouetteAssets.EdgeShader)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
            }

            if (_depthVisMaterial == null)
            {
                Shader depthVis = Shader.Find("Hidden/Universal Render Pipeline/CopyDepth")
                    ?? Shader.Find("Hidden/ConvertTexture");
                if (depthVis != null)
                {
                    _depthVisMaterial = new Material(depthVis)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                }
            }
        }

        internal void RenderFrame(Camera main, Color hudColor, Color nearColor)
        {
            if (main == null)
                return;

            _depthCam.SyncFrom(main);
            _depthCam.Render();

            RenderTexture src = _depthCam.TerrainRt;
            RenderTexture dst = _depthCam.EdgeRt;
            if (src == null || dst == null)
                return;

            if (_edgeMaterial != null)
            {
                _edgeMaterial.SetColor("_HudColor", hudColor);
                _edgeMaterial.SetColor("_NearColor", nearColor);
                _edgeMaterial.SetFloat("_EdgeThreshold", TerrainSilhouetteHudPlugin.EdgeThreshold.Value);
                _edgeMaterial.SetFloat("_EdgeStrength", TerrainSilhouetteHudPlugin.EdgeStrength.Value);
                _edgeMaterial.SetFloat("_NearDepthBias", TerrainSilhouetteHudPlugin.NearDepthBias.Value);
                _edgeMaterial.SetFloat("_NearDepthScale", TerrainSilhouetteHudPlugin.NearDepthScale.Value);

                if (_depthVisMaterial != null && TerrainSilhouetteHudPlugin.UseDepthTint.Value)
                {
                    RenderTexture depthColor = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.R8);
                    Graphics.Blit(src, depthColor, _depthVisMaterial);
                    Graphics.Blit(depthColor, dst, _edgeMaterial);
                    RenderTexture.ReleaseTemporary(depthColor);
                }
                else
                {
                    Graphics.Blit(src, dst, _edgeMaterial);
                }
            }
            else
            {
                if (!_loggedFallback)
                {
                    _loggedFallback = true;
                    TerrainSilhouetteHudPlugin.Logger?.LogWarning(
                        "GPU edge shader missing — showing dim terrain mask only. Build terrainsilhouette_shaders bundle.");
                }

                Graphics.Blit(src, dst);
            }
        }

        internal void Teardown()
        {
            if (_edgeMaterial != null)
            {
                Object.Destroy(_edgeMaterial);
                _edgeMaterial = null;
            }

            if (_depthVisMaterial != null)
            {
                Object.Destroy(_depthVisMaterial);
                _depthVisMaterial = null;
            }

            _depthCam.Teardown();
            _loggedFallback = false;
        }
    }
}
