using UnityEngine;
using UnityEngine.UI;

namespace TerrainSilhouetteHud_Engine.Hud
{
    /// <summary>
    /// Full-screen HUD overlay on the FlightHud canvas (semi-transparent edge texture).
    /// </summary>
    internal sealed class TerrainSilhouetteOverlayView
    {
        private readonly GameObject _root;
        private readonly RawImage _image;
        private readonly RectTransform _rect;

        internal TerrainSilhouetteOverlayView(Transform flightHudCanvasTransform)
        {
            _root = new GameObject("TerrainSilhouetteOverlay");
            _root.transform.SetParent(flightHudCanvasTransform, false);
            _root.transform.SetAsLastSibling();

            _rect = _root.AddComponent<RectTransform>();
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
            _rect.pivot = new Vector2(0.5f, 0.5f);

            _image = _root.AddComponent<RawImage>();
            _image.raycastTarget = false;
            _image.color = Color.white;
        }

        internal bool NeedsRebuild(Transform flightHudCanvasTransform)
        {
            if (flightHudCanvasTransform == null || _root == null)
                return true;
            return _root.transform.parent != flightHudCanvasTransform;
        }

        internal void SetVisible(bool visible)
        {
            if (_root != null && _root.activeSelf != visible)
                _root.SetActive(visible);
        }

        internal void SetTexture(Texture texture, float overlayAlpha)
        {
            _image.texture = texture;
            Color c = _image.color;
            c.a = Mathf.Clamp01(overlayAlpha);
            _image.color = c;
        }

        internal void Dispose()
        {
            if (_root != null)
                Object.Destroy(_root);
        }
    }
}
