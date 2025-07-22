using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core
{
    /// <summary>
    /// Color handler class.
    /// </summary>
    public static class RTColors
    {
        /// <summary>
        /// Hex ToString format.
        /// </summary>
        public const string X2 = "X2";

        public const string WHITE_HEX_CODE = "FFFFFF";
        public const string BLACK_HEX_CODE = "000000";

        public static Color defaultBloomColor = Color.white;
        public static Color defaultVignetteColor = Color.black;
        public static Color defaultGradientColor1 = new Color(0f, 0.8f, 0.56f, 0.5f);
        public static Color defaultGradientColor2 = new Color(0.81f, 0.37f, 1f, 0.5f);
        public static Color defaultDangerColor = new Color(0.66f, 0f, 0f);
        public static Color errorColor = LSColors.pink500;

        /// <summary>
        /// Converts a float to a hex value.
        /// </summary>
        /// <param name="num">Number to convert.</param>
        /// <returns>Returns a hex code from the number provided.</returns>
        public static string FloatToHex(float num) => num.ToString(X2);

        /// <summary>
        /// Converts all color channels (including alpha) to a hex value.
        /// </summary>
        /// <param name="color">Color to convert.</param>
        /// <returns>Returns a hex code from the color provided.</returns>
        public static string ColorToHex(Color32 color) => color.r.ToString(X2) + color.g.ToString(X2) + color.b.ToString(X2) + color.a.ToString(X2);

        /// <summary>
        /// Converts R, G and B color channels to a hex value. If the alpha channel is not full, then add that to the hex value.
        /// </summary>
        /// <param name="color">Color to convert.</param>
        /// <returns>Returns a hex code from the color provided.</returns>
        public static string ColorToHexOptional(Color32 color)
        {
            var result = color.r.ToString(X2) + color.g.ToString(X2) + color.b.ToString(X2);
            var a = color.a.ToString(X2);
            if (a != "FF")
                result += a;
            return result;
        }

        /// <summary>
        /// Parses a float number from a hex code.
        /// </summary>
        /// <param name="hex">Hex code to parse.</param>
        /// <returns>Returns the parsed float.</returns>
        public static float HexToFloat(string hex)
        {
            byte b = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            return b / 255f;
        }

        /// <summary>
        /// Parses a color from a hex code.
        /// </summary>
        /// <param name="hex">Hex code to parse.</param>
        /// <returns>Returns the parsed color.</returns>
        public static Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return LSColors.transparent;

            hex = hex.Remove("#");

            if (hex.Length < 6)
                return LSColors.pink500;

            var r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            var a = hex.Length > 7 ? byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber) : byte.MaxValue;
            return new Color32(r, g, b, a);
        }

        /// <summary>
        /// Changes the opacity of a color.
        /// </summary>
        /// <param name="col">Color to change the opacity of.</param>
        /// <param name="opacity">Opacity to set.</param>
        /// <returns>Returns the faded color.</returns>
        public static Color FadeColor(Color col, float opacity) => new Color(col.r, col.g, col.b, opacity);

        /// <summary>
        /// Changes the hue, saturation and value of a color.
        /// </summary>
        /// <param name="color">Color to change.</param>
        /// <param name="hue">Hue offset.</param>
        /// <param name="sat">Saturation offset.</param>
        /// <param name="val">Value offset.</param>
        /// <returns>Returns a changed color based on the hue / sat / val offset values.</returns>
        public static Color ChangeColorHSV(Color color, float hue, float sat, float val)
        {
            LSColors.ColorToHSV(color, out double num, out double saturation, out double value);
            return LSColors.ColorFromHSV(num + hue, saturation + sat, value + val);
        }

        /// <summary>
        /// Inverts a color.
        /// </summary>
        /// <param name="color">Color to invert.</param>
        /// <returns>Returns an inverted color.</returns>
        public static Color InvertColor(Color color) => InvertColorHue(InvertColorValue(color));

        /// <summary>
        /// Inverts a colors' hue.
        /// </summary>
        /// <param name="color">Color to invert.</param>
        /// <returns>Returns an inverted color.</returns>
        public static Color InvertColorHue(Color color)
        {
            LSColors.ColorToHSV(color, out double hue, out double saturation, out double value);
            return LSColors.ColorFromHSV(hue - 180.0, saturation, value);
        }

        /// <summary>
        /// Inverts a colors' value.
        /// </summary>
        /// <param name="color">Color to invert.</param>
        /// <returns>Returns an inverted color.</returns>
        public static Color InvertColorValue(Color color)
        {
            LSColors.ColorToHSV(color, out double hue, out double sat, out double val);
            return LSColors.ColorFromHSV(hue, sat, val < 0.5 ? -val + 1 : -(val - 1));
        }

        /// <summary>
        /// Gets a custom player object color.
        /// </summary>
        /// <param name="playerIndex">Index reference.</param>
        /// <param name="col"></param>
        /// <param name="alpha"></param>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static Color GetPlayerColor(int playerIndex, int col, float alpha, string hex)
        {
            var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

            return FadeColor(col >= 0 && col < 4 ? beatmapTheme.playerColors[col] : col == 4 ? beatmapTheme.guiColor : col > 4 && col < 23 ? beatmapTheme.objectColors[col - 5] :
                col == 23 ? beatmapTheme.playerColors[playerIndex % 4] : col == 24 ? LSColors.HexToColor(hex) : col == 25 ? beatmapTheme.guiAccentColor : LSColors.pink500, alpha);
        }

        /// <summary>
        /// Creates and fills a color list.
        /// </summary>
        /// <param name="count">Amount to fill.</param>
        /// <returns></returns>
        public static List<Color> NewColorList(int count)
        {
            var list = new List<Color>();
            while (list.Count < count)
                list.Add(LSColors.pink500);
            return list;
        }

        /// <summary>
        /// Mixes all colors in a list.
        /// </summary>
        /// <param name="colors">Colors to mix.</param>
        /// <returns>Returns the mixed color.</returns>
        public static Color MixColors(List<Color> colors)
        {
            var invertedColorSum = Color.black;
            foreach (var color in colors)
                invertedColorSum += Color.white - color;

            return Color.white - invertedColorSum / colors.Count;
        }

        /// <summary>
        /// Mixes all colors in a collection.
        /// </summary>
        /// <param name="colors">Colors to mix.</param>
        /// <returns>Returns the mixed color.</returns>
        public static Color MixColors(IEnumerable<Color> colors)
        {
            var invertedColorSum = Color.black;
            foreach (var color in colors)
                invertedColorSum += Color.white - color;

            return Color.white - invertedColorSum / colors.Count();
        }

        /// <summary>
        /// Mixes all colors in a collection.
        /// </summary>
        /// <param name="colors">Colors to mix.</param>
        /// <returns>Returns the mixed color.</returns>
        public static Color MixColors(params Color[] colors)
        {
            var invertedColorSum = Color.black;
            foreach (var color in colors)
                invertedColorSum += Color.white - color;

            return Color.white - invertedColorSum / colors.Length;
        }

        /// <summary>
        /// Checks if two colors are in the range of each other.
        /// </summary>
        /// <param name="a">First color to compare.</param>
        /// <param name="b">Second color to compare.</param>
        /// <param name="range">Range to check.</param>
        /// <param name="alpha">If opacity should be accounted for.</param>
        /// <returns>Returns true if both colors in range of each other, otherwise returns false.</returns>
        public static bool ColorMatch(Color a, Color b, float range, bool alpha = false)
            => alpha ? a.r < b.r + range && a.r > b.r - range && a.g < b.g + range && a.g > b.g - range && a.b < b.b + range && a.b > b.b - range && a.a < b.a + range && a.a > b.a - range :
                a.r < b.r + range && a.r > b.r - range && a.g < b.g + range && a.g > b.g - range && a.b < b.b + range && a.b > b.b - range;
    }
}
