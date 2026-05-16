using UnityEngine;

namespace TerrainSilhouetteHud_Engine.Hud
{
    internal static class TerrainPolylineClipper
    {
        internal static int ClipHorizontalBand(
            Vector2[] source,
            int sourceCount,
            float minX,
            float maxX,
            Vector2[] dest,
            float maxGapPixels)
        {
            if (source == null || dest == null || sourceCount < 2 || maxX <= minX)
                return 0;

            int outCount = 0;
            int maxDest = dest.Length;
            float maxGap = Mathf.Max(4f, maxGapPixels);

            for (int i = 0; i < sourceCount - 1; i++)
            {
                Vector2 a = source[i];
                Vector2 b = source[i + 1];
                if (!ClipSegmentToBand(ref a, ref b, minX, maxX))
                    continue;

                if (outCount > 0 && Vector2.Distance(dest[outCount - 1], a) > maxGap)
                    outCount = 0;

                if (outCount >= maxDest - 1)
                    break;

                if (outCount == 0 || (dest[outCount - 1] - a).sqrMagnitude > 0.25f)
                    dest[outCount++] = a;

                if (outCount >= maxDest)
                    break;

                dest[outCount++] = b;
            }

            return outCount >= 2 ? outCount : 0;
        }

        internal static bool TryClipSegmentToBand(ref Vector2 a, ref Vector2 b, float minX, float maxX)
        {
            return ClipSegmentToBand(ref a, ref b, minX, maxX);
        }

        private static bool ClipSegmentToBand(ref Vector2 a, ref Vector2 b, float minX, float maxX)
        {
            if (a.x < minX && b.x < minX)
                return false;
            if (a.x > maxX && b.x > maxX)
                return false;

            a = ClipPointToBand(a, b, minX, maxX);
            b = ClipPointToBand(b, a, minX, maxX);
            return (b - a).sqrMagnitude > 0.25f;
        }

        private static Vector2 ClipPointToBand(Vector2 p, Vector2 other, float minX, float maxX)
        {
            if (p.x >= minX && p.x <= maxX)
                return p;

            float dx = other.x - p.x;
            if (Mathf.Abs(dx) < 1e-4f)
                return new Vector2(Mathf.Clamp(p.x, minX, maxX), p.y);

            float targetX = p.x < minX ? minX : maxX;
            float t = Mathf.Clamp01((targetX - p.x) / dx);
            return Vector2.Lerp(p, other, t);
        }
    }
}
