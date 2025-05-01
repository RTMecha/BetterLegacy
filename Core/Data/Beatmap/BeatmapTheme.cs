using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class BeatmapTheme : PAObject<BeatmapTheme>
    {
        public BeatmapTheme() => id = string.Empty;

        #region Values

        public string VGID { get; set; }

        public string name = string.Empty;

        public bool isDefault;

        public string filePath;

        public Color backgroundColor = LSColors.gray100;

        public Color guiColor = LSColors.gray900;

        public Color guiAccentColor = Color.white;

        public List<Color> effectColors = new List<Color>();

        public List<Color> playerColors = new List<Color>();

        public List<Color> objectColors = new List<Color>();

        public List<Color> backgroundColors = new List<Color>();

        public ThemePanel themePanel;

        #endregion

        #region Consts

        public const int ID_LENGTH = 7;
        public const string PLAYER_1_COLOR = "E57373";
        public const string PLAYER_2_COLOR = "64B5F6";
        public const string PLAYER_3_COLOR = "81C784";
        public const string PLAYER_4_COLOR = "FFB74D";

        #endregion

        #region Default Properties

        public static string DefaultName { get; set; } = "New Theme";

        public static Color DefaultBGColor { get; set; } = LSColors.gray900;
        public static Color DefaultGUIColor { get; set; } = LSColors.white;
        public static Color DefaultTailColor { get; set; } = LSColors.white;

        public static List<Color> DefaultPlayerColors { get; set; } = new List<Color>
        {
            LSColors.HexToColor("E57373"),
            LSColors.HexToColor("64B5F6"),
            LSColors.HexToColor("81C784"),
            LSColors.HexToColor("FFB74D"),
        };
        public static List<Color> DefaultObjectColors { get; set; } = new List<Color>
        {
            LSColors.gray100,
            LSColors.gray200,
            LSColors.gray300,
            LSColors.gray400,
            LSColors.gray500,
            LSColors.gray600,
            LSColors.gray700,
            LSColors.gray800,
            LSColors.gray900,
            LSColors.gray100,
            LSColors.gray200,
            LSColors.gray300,
            LSColors.gray400,
            LSColors.gray500,
            LSColors.gray600,
            LSColors.gray700,
            LSColors.gray800,
            LSColors.gray900,
        };
        public static List<Color> DefaulBackgroundColors { get; set; } = new List<Color>
        {
            LSColors.pink100,
            LSColors.pink200,
            LSColors.pink300,
            LSColors.pink400,
            LSColors.pink500,
            LSColors.pink600,
            LSColors.pink700,
            LSColors.pink800,
            LSColors.pink900,
            LSColors.pink100,
            LSColors.pink200,
            LSColors.pink300,
            LSColors.pink400,
            LSColors.pink500,
            LSColors.pink600,
            LSColors.pink700,
            LSColors.pink800,
            LSColors.pink900,
        };
        public static List<Color> DefaultEffectColors { get; set; } = new List<Color>
        {
            LSColors.gray100,
            LSColors.gray200,
            LSColors.gray300,
            LSColors.gray400,
            LSColors.gray500,
            LSColors.gray600,
            LSColors.gray700,
            LSColors.gray800,
            LSColors.gray900,
            LSColors.gray100,
            LSColors.gray200,
            LSColors.gray300,
            LSColors.gray400,
            LSColors.gray500,
            LSColors.gray600,
            LSColors.gray700,
            LSColors.gray800,
            LSColors.gray900,
        };

        #endregion

        #region Methods

        public override void CopyData(BeatmapTheme orig, bool newID = true)
        {
            name = orig.name;
            filePath = orig.filePath;

            playerColors = orig.playerColors.Clone();
            objectColors = orig.objectColors.Clone();
            backgroundColors = orig.backgroundColors.Clone();
            effectColors = orig.effectColors.Clone();

            guiAccentColor = orig.guiAccentColor;
            guiColor = orig.guiColor;
            backgroundColor = orig.backgroundColor;

            if (!newID)
                id = orig.id;

            var lastObjColor = objectColors.Last();
            while (objectColors.Count < 18)
                objectColors.Add(lastObjColor);

            var lastBGColor = backgroundColors.Last();
            while (backgroundColors.Count < 9)
                backgroundColors.Add(lastBGColor);

            var lastFXColor = effectColors.Last();
            while (effectColors.Count < 18)
                effectColors.Add(lastFXColor);
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            id = LSText.randomNumString(ID_LENGTH);
            VGID = jn["id"] ?? "";

            name = jn["name"] ?? "name your themes!";

            guiColor = jn["base_gui"] != null ? LSColors.HexToColor(jn["base_gui"]) : LSColors.gray800;

            guiAccentColor = jn["base_gui_accent"] != null ? LSColors.HexToColor(jn["base_gui_accent"]) : guiColor;

            backgroundColor = jn["base_bg"] != null ? LSColors.HexToColor(jn["base_bg"]) : LSColors.gray100;

            playerColors = jn["pla"] != null ? SetColors(jn["pla"], 4, "Player Hex code does not exist for some reason") : new List<Color>
            {
                LSColors.HexToColorAlpha("E57373FF"),
                LSColors.HexToColorAlpha("64B5F6FF"),
                LSColors.HexToColorAlpha("81C784FF"),
                LSColors.HexToColorAlpha("FFB74DFF"),
            };

            objectColors = jn["obj"] != null ? SetColors(jn["obj"], 18) : new List<Color>
            {
                LSColors.pink100,
                LSColors.pink200,
                LSColors.pink300,
                LSColors.pink400,
                LSColors.pink500,
                LSColors.pink600,
                LSColors.pink700,
                LSColors.pink800,
                LSColors.pink900,
                LSColors.pink100,
                LSColors.pink200,
                LSColors.pink300,
                LSColors.pink400,
                LSColors.pink500,
                LSColors.pink600,
                LSColors.pink700,
                LSColors.pink800,
                LSColors.pink900,
            };

            backgroundColors = jn["bg"] != null ? SetColors(jn["bg"], 9, "BG Hex code does not exist for some reason") : new List<Color>
            {
                LSColors.gray100,
                LSColors.gray200,
                LSColors.gray300,
                LSColors.gray400,
                LSColors.gray500,
                LSColors.gray600,
                LSColors.gray700,
                LSColors.gray800,
                LSColors.gray900,
            };

            effectColors = jn["fx"] != null ? SetColors(jn["fx"], 18) : objectColors.Clone();
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? ThemeManager.inst.ThemeCount.ToString();

            name = jn["name"] ?? "name your themes!";

            guiColor = jn["gui"] != null ? ((string)jn["gui"]).Length == 8 ? LSColors.HexToColorAlpha(jn["gui"]) : LSColors.HexToColor(jn["gui"]) : LSColors.gray800;

            guiAccentColor = jn["gui_ex"] != null ? ((string)jn["gui_ex"]).Length == 8 ? LSColors.HexToColor(jn["gui_ex"]) : LSColors.HexToColorAlpha(jn["gui_ex"]) : guiColor;

            backgroundColor = jn["bg"] != null ? LSColors.HexToColor(jn["bg"]) : LSColors.gray100;

            playerColors = jn["players"] != null ? SetColors(jn["players"], 4, "Player Hex code does not exist for some reason") : new List<Color>
                {
                    LSColors.HexToColorAlpha("E57373FF"),
                    LSColors.HexToColorAlpha("64B5F6FF"),
                    LSColors.HexToColorAlpha("81C784FF"),
                    LSColors.HexToColorAlpha("FFB74DFF"),
                };

            objectColors = jn["objs"] != null ? SetColors(jn["objs"], 18) : new List<Color>
            {
                LSColors.pink100,
                LSColors.pink200,
                LSColors.pink300,
                LSColors.pink400,
                LSColors.pink500,
                LSColors.pink600,
                LSColors.pink700,
                LSColors.pink800,
                LSColors.pink900,
                LSColors.pink100,
                LSColors.pink200,
                LSColors.pink300,
                LSColors.pink400,
                LSColors.pink500,
                LSColors.pink600,
                LSColors.pink700,
                LSColors.pink800,
                LSColors.pink900,
            };

            backgroundColors = jn["bgs"] != null ? SetColors(jn["bgs"], 9, "BG Hex code does not exist for some reason") : new List<Color>
                {
                    LSColors.gray100,
                    LSColors.gray200,
                    LSColors.gray300,
                    LSColors.gray400,
                    LSColors.gray500,
                    LSColors.gray600,
                    LSColors.gray700,
                    LSColors.gray800,
                    LSColors.gray900,
                };

            effectColors = jn["fx"] != null ? SetColors(jn["fx"], 18) : objectColors.Clone();
        }

        public override JSONNode ToJSONVG()
        {
            var jn = JSON.Parse("{}");

            jn["name"] = name;
            if (!string.IsNullOrEmpty(VGID))
                jn["id"] = VGID;
            jn["base_gui_accent"] = LSColors.ColorToHex(guiAccentColor);
            jn["base_gui"] = LSColors.ColorToHex(guiColor);
            jn["base_bg"] = LSColors.ColorToHex(backgroundColor);

            for (int i = 0; i < 4; i++)
                jn["pla"][i] = LSColors.ColorToHex(playerColors[i]);

            for (int i = 0; i < 9; i++)
                jn["obj"][i] = LSColors.ColorToHex(objectColors[i]);

            for (int i = 0; i < 9; i++)
                jn["bg"][i] = LSColors.ColorToHex(backgroundColors[i]);

            for (int i = 0; i < 9; i++)
                jn["fx"][i] = LSColors.ColorToHex(effectColors[i]);

            return jn;
        }

        public override JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["id"] = id;
            jn["name"] = name;
            jn["gui_ex"] = GameData.SaveOpacityToThemes ? CoreHelper.ColorToHex(guiAccentColor) : LSColors.ColorToHex(guiAccentColor);
            jn["gui"] = GameData.SaveOpacityToThemes ? CoreHelper.ColorToHex(guiColor) : LSColors.ColorToHex(guiColor);
            jn["bg"] = LSColors.ColorToHex(backgroundColor);

            for (int i = 0; i < playerColors.Count; i++)
                jn["players"][i] = GameData.SaveOpacityToThemes ? CoreHelper.ColorToHex(playerColors[i]) : LSColors.ColorToHex(playerColors[i]);

            for (int i = 0; i < objectColors.Count; i++)
                jn["objs"][i] = GameData.SaveOpacityToThemes ? CoreHelper.ColorToHex(objectColors[i]) : LSColors.ColorToHex(objectColors[i]);

            for (int i = 0; i < backgroundColors.Count; i++)
                jn["bgs"][i] = LSColors.ColorToHex(backgroundColors[i]);

            if (effectColors != null)
                for (int i = 0; i < effectColors.Count; i++)
                    jn["fx"][i] = GameData.SaveOpacityToThemes ? CoreHelper.ColorToHex(effectColors[i]) : LSColors.ColorToHex(effectColors[i]);

            return jn;
        }

        static List<Color> SetColors(JSONNode jn, int count, string errorMsg = "", bool alpha = true)
        {
            var colors = new List<Color>();

            Color lastColor = LSColors.pink500;
            for (int i = 0; i < jn.Count; i++)
            {
                var hex = jn[i];
                lastColor = hex != null ? alpha && ((string)hex).Length == 8 ? LSColors.HexToColorAlpha(hex) : LSColors.HexToColor(hex) : LSColors.pink500;
                if (hex == null && !string.IsNullOrEmpty(errorMsg))
                    Debug.LogError(errorMsg);

                colors.Add(lastColor);
            }

            while (colors.Count < count)
                colors.Add(lastColor);

            return colors;
        }

        /// <summary>
        /// Resets the theme to the default values.
        /// </summary>
        public void Reset()
        {
            // clear
            playerColors.Clear();
            objectColors.Clear();
            backgroundColors.Clear();

            // reset
            id = LSText.randomNumString(ID_LENGTH);
            name = DefaultName;
            backgroundColor = new Color(DefaultBGColor.r, DefaultBGColor.g, DefaultBGColor.b, DefaultBGColor.a);
            guiColor = new Color(DefaultGUIColor.r, DefaultGUIColor.g, DefaultGUIColor.b, DefaultGUIColor.a);
            guiAccentColor = new Color(DefaultTailColor.r, DefaultTailColor.g, DefaultTailColor.b, DefaultTailColor.a);

            playerColors.AddRange(DefaultPlayerColors.Select(x => new Color(x.r, x.g, x.b, x.a)));
            objectColors.AddRange(DefaultObjectColors.Select(x => new Color(x.r, x.g, x.b, x.a)));
            backgroundColors.AddRange(DefaulBackgroundColors.Select(x => new Color(x.r, x.g, x.b, x.a)));
            effectColors.AddRange(DefaultEffectColors.Select(x => new Color(x.r, x.g, x.b, x.a)));
        }

        /// <summary>
        /// Lerps between two themes.
        /// </summary>
        /// <param name="prev">Previous theme.</param>
        /// <param name="next">Next theme.</param>
        /// <param name="t">Time scale.</param>
        public void Lerp(BeatmapTheme prev, BeatmapTheme next, float t)
        {
            guiColor = Color.Lerp(prev.guiColor, next.guiColor, t);
            guiAccentColor = Color.Lerp(prev.guiAccentColor, next.guiAccentColor, t);
            backgroundColor = Color.Lerp(prev.backgroundColor, next.backgroundColor, t);

            for (int i = 0; i < 4; i++)
                playerColors[i] = Color.Lerp(prev.GetPlayerColor(i), next.GetPlayerColor(i), t);

            int maxObj = 9;
            if (prev.objectColors.Count > 9 && next.objectColors.Count > 9)
                maxObj = 18;

            for (int j = 0; j < maxObj; j++)
                objectColors[j] = Color.Lerp(prev.GetObjColor(j), next.GetObjColor(j), t);

            for (int k = 0; k < 9; k++)
                backgroundColors[k] = Color.Lerp(prev.GetBGColor(k), next.GetBGColor(k), t);

            for (int k = 0; k < 18; k++)
                effectColors[k] = Color.Lerp(prev.GetFXColor(k), next.GetFXColor(k), t);
        }

        /// <summary>
        /// Gets a player color from the theme.
        /// </summary>
        /// <param name="index">Index of the color.</param>
        /// <returns>Returns a color from the player color list.</returns>
        public Color GetPlayerColor(int index) => playerColors[Mathf.Clamp(index, 0, playerColors.Count - 1)];

        /// <summary>
        /// Gets a object color from the theme.
        /// </summary>
        /// <param name="index">Index of the color.</param>
        /// <returns>Returns a color from the object color list.</returns>
        public Color GetObjColor(int index) => objectColors[Mathf.Clamp(index, 0, objectColors.Count - 1)];

        /// <summary>
        /// Gets a background color from the theme.
        /// </summary>
        /// <param name="index">Index of the color.</param>
        /// <returns>Returns a color from the background color list.</returns>
        public Color GetBGColor(int index) => backgroundColors[Mathf.Clamp(index, 0, backgroundColors.Count - 1)];

        /// <summary>
        /// Gets an effect color from the theme.
        /// </summary>
        /// <param name="index">Index of the color.</param>
        /// <returns>Returns a color from the effect color list.</returns>
        public Color GetFXColor(int index) => effectColors[Mathf.Clamp(index, 0, effectColors.Count - 1)];

        #endregion

        #region Operators

        public override bool Equals(object obj) => obj is BeatmapTheme beatmapTheme && id == beatmapTheme.id;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => $"{id}: {name}";

        #endregion
    }
}
