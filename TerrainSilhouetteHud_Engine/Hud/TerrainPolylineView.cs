using UnityEngine;
using UnityEngine.UI;

namespace TerrainSilhouetteHud_Engine.Hud
{
    internal sealed class TerrainPolylineView
    {
        private static Sprite _whiteSprite;

        private readonly GameObject _root;
        private readonly Image[] _segments;
        private readonly RectTransform[] _rects;

        internal TerrainPolylineView(Transform flightHudCanvasTransform, string rootName, int maxSegments)
        {
            int cap = Mathf.Max(8, maxSegments);
            _root = new GameObject(rootName);
            _root.transform.SetParent(flightHudCanvasTransform, false);

            _segments = new Image[cap];
            _rects = new RectTransform[cap];
            for (int i = 0; i < cap; i++)
            {
                var go = new GameObject("Seg" + i);
                go.transform.SetParent(_root.transform, false);
                var img = go.AddComponent<Image>();
                img.raycastTarget = false;
                img.sprite = GetWhiteSprite();
                img.type = Image.Type.Simple;
                _segments[i] = img;
                _rects[i] = img.rectTransform;
                _rects[i].anchorMin = new Vector2(0.5f, 0.5f);
                _rects[i].anchorMax = new Vector2(0.5f, 0.5f);
                _rects[i].pivot = new Vector2(0.5f, 0.5f);
                go.SetActive(false);
            }
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

        internal void SetPolyline(
            Vector2[] points,
            int count,
            Color color,
            float thicknessPixels,
            float maxGapPixels,
            float clipMinX = float.NegativeInfinity,
            float clipMaxX = float.PositiveInfinity)
        {
            int segIndex = 0;
            float thickness = Mathf.Max(0.5f, thicknessPixels);
            float maxGap = Mathf.Max(4f, maxGapPixels);
            bool clipX = clipMaxX > clipMinX + 1f;

            for (int i = 0; i < _segments.Length; i++)
                _segments[i].gameObject.SetActive(false);

            if (points == null || count < 2)
                return;

            for (int i = 0; i < count - 1 && segIndex < _segments.Length; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                if (clipX && !TerrainPolylineClipper.TryClipSegmentToBand(ref a, ref b, clipMinX, clipMaxX))
                    continue;

                Vector2 d = b - a;
                float len = d.magnitude;
                if (len < 1f || len > maxGap)
                    continue;

                Image img = _segments[segIndex];
                RectTransform rect = _rects[segIndex];
                img.color = color;
                rect.sizeDelta = new Vector2(len, thickness);
                rect.position = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0f);
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                rect.localEulerAngles = new Vector3(0f, 0f, angle);
                img.gameObject.SetActive(true);
                segIndex++;
            }
        }

        internal void Dispose()
        {
            if (_root != null)
                Object.Destroy(_root);
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            Texture2D tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return _whiteSprite;
        }
    }
}
