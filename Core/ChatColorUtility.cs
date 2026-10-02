using UnityEngine;
namespace BetterLegacy.Core
{
    public static class ChatColorUtility
    {
        public static Color MakeReadable(Color color, Color background, float minContrastRatio = 4.5f)
        {
            var backgroundLuminance = RelativeLuminance(background);
            var requiredLuminance = minContrastRatio * (backgroundLuminance + 0.05f) - 0.05f;
            if (RelativeLuminance(color) >= requiredLuminance)
                return color;
            RgbToHsl(color.r, color.g, color.b, out float h, out float s, out float l);
            var white = new Color(1f, 1f, 1f, color.a);
            if (RelativeLuminance(white) < requiredLuminance)
                return white;
            var lo = l;
            var hi = 1f;
            for (int i = 0; i < 24; i++)
            {
                var mid = (lo + hi) * 0.5f;
                HslToRgb(h, s, mid, out float r, out float g, out float b);
                if (RelativeLuminance(new Color(r, g, b, color.a)) >= requiredLuminance)
                    hi = mid;
                else
                    lo = mid;
            }
            HslToRgb(h, s, hi, out float outR, out float outG, out float outB);
            return new Color(outR, outG, outB, color.a);
        }
        public static float RelativeLuminance(Color color) =>
            0.2126f * SrgbToLinear(color.r) +
            0.7152f * SrgbToLinear(color.g) +
            0.0722f * SrgbToLinear(color.b);
        static float SrgbToLinear(float c)
        {
            c = Mathf.Clamp01(c);
            return c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
        }
        static void RgbToHsl(float r, float g, float b, out float h, out float s, out float l)
        {
            var max = Mathf.Max(r, Mathf.Max(g, b));
            var min = Mathf.Min(r, Mathf.Min(g, b));
            var delta = max - min;
            l = (max + min) * 0.5f;
            if (delta <= 0.000001f)
            {
                h = 0f;
                s = 0f;
                return;
            }
            s = l > 0.5f ? delta / (2f - max - min) : delta / (max + min);
            if (max == r)
                h = (g - b) / delta + (g < b ? 6f : 0f);
            else if (max == g)
                h = (b - r) / delta + 2f;
            else
                h = (r - g) / delta + 4f;
            h /= 6f;
        }
        static void HslToRgb(float h, float s, float l, out float r, out float g, out float b)
        {
            if (s <= 0.000001f)
            {
                r = g = b = l;
                return;
            }
            var q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            var p = 2f * l - q;
            r = HueToRgb(p, q, h + 1f / 3f);
            g = HueToRgb(p, q, h);
            b = HueToRgb(p, q, h - 1f / 3f);
        }
        static float HueToRgb(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }
    }
}