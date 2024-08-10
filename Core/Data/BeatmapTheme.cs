using BetterLegacy.Core.Helpers;
using LSFunctions;
using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseBeatmapTheme = DataManager.BeatmapTheme;

namespace BetterLegacy.Core.Data
{
    public class BeatmapTheme : BaseBeatmapTheme
    {
        public string filePath;

        public Color guiAccentColor = Color.white;

        public List<Color> effectColors = new List<Color>();

        public string VGID { get; set; }

        #region Methods

        public static BeatmapTheme DeepCopy(BeatmapTheme orig, bool _copyID = false)
        {
            var beatmapTheme = new BeatmapTheme();
            beatmapTheme.name = orig.name;
            beatmapTheme.filePath = orig.filePath;

            beatmapTheme.playerColors = orig.playerColors.Clone();
            beatmapTheme.objectColors = orig.objectColors.Clone();
            beatmapTheme.backgroundColors = orig.backgroundColors.Clone();
            beatmapTheme.effectColors = orig.effectColors.Clone();

            beatmapTheme.guiAccentColor = orig.guiAccentColor;
            beatmapTheme.guiColor = orig.guiColor;
            beatmapTheme.backgroundColor = orig.backgroundColor;

            beatmapTheme.expanded = orig.expanded;
            if (_copyID)
                beatmapTheme.id = orig.id;

            var lastObjColor = beatmapTheme.objectColors.Last();
            while (beatmapTheme.objectColors.Count < 18)
                beatmapTheme.objectColors.Add(lastObjColor);

            var lastBGColor = beatmapTheme.backgroundColors.Last();
            while (beatmapTheme.backgroundColors.Count < 9)
                beatmapTheme.backgroundColors.Add(lastBGColor);

            var lastFXColor = beatmapTheme.effectColors.Last();
            while (beatmapTheme.effectColors.Count < 18)
                beatmapTheme.effectColors.Add(lastFXColor);

            return beatmapTheme;
        }

        public static BeatmapTheme ParseVG(JSONNode jn)
        {
            var beatmapTheme = new BeatmapTheme();

            beatmapTheme.id = LSText.randomNumString(IDLength);
            beatmapTheme.VGID = jn["id"] != null ? jn["id"] : "";

            beatmapTheme.name = jn["name"] != null ? jn["name"] : "name your themes!";

            beatmapTheme.guiColor = jn["base_gui"] != null ? LSColors.HexToColor(jn["base_gui"]) : LSColors.gray800;

            beatmapTheme.guiAccentColor = jn["base_gui_accent"] != null ? LSColors.HexToColor(jn["base_gui_accent"]) : beatmapTheme.guiColor;

            beatmapTheme.backgroundColor = jn["base_bg"] != null ? LSColors.HexToColor(jn["base_bg"]) : LSColors.gray100;

            beatmapTheme.playerColors = jn["pla"] != null ? SetColors(jn["pla"], 4, "Player Hex code does not exist for some reason") : new List<Color>
            {
                LSColors.HexToColorAlpha("E57373FF"),
                LSColors.HexToColorAlpha("64B5F6FF"),
                LSColors.HexToColorAlpha("81C784FF"),
                LSColors.HexToColorAlpha("FFB74DFF"),
            };

            beatmapTheme.objectColors = jn["obj"] != null ? SetColors(jn["obj"], 18) : new List<Color>
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

            beatmapTheme.backgroundColors = jn["bg"] != null ? SetColors(jn["bg"], 9, "BG Hex code does not exist for some reason") : new List<Color>
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

            beatmapTheme.effectColors = jn["fx"] != null ? SetColors(jn["fx"], 18) : beatmapTheme.objectColors.Clone();

            return beatmapTheme;
        }

        public static BeatmapTheme Parse(JSONNode jn)
        {
            var beatmapTheme = new BeatmapTheme();

            beatmapTheme.id = jn["id"] != null ? jn["id"] : DataManager.inst.AllThemes.Count.ToString();

            beatmapTheme.name = jn["name"] != null ? jn["name"] : "name your themes!";

            beatmapTheme.guiColor = jn["gui"] != null ? ((string)jn["gui"]).Length == 8 ? LSColors.HexToColorAlpha(jn["gui"]) : LSColors.HexToColor(jn["gui"]) : LSColors.gray800;

            beatmapTheme.guiAccentColor = jn["gui_ex"] != null ? ((string)jn["gui_ex"]).Length == 8 ? LSColors.HexToColor(jn["gui_ex"]) : LSColors.HexToColorAlpha(jn["gui_ex"]) : beatmapTheme.guiColor;

            beatmapTheme.backgroundColor = jn["bg"] != null ? LSColors.HexToColor(jn["bg"]) : LSColors.gray100;

            beatmapTheme.playerColors = jn["players"] != null ? SetColors(jn["players"], 4, "Player Hex code does not exist for some reason") : new List<Color>
                {
                    LSColors.HexToColorAlpha("E57373FF"),
                    LSColors.HexToColorAlpha("64B5F6FF"),
                    LSColors.HexToColorAlpha("81C784FF"),
                    LSColors.HexToColorAlpha("FFB74DFF"),
                };

            beatmapTheme.objectColors = jn["objs"] != null ? SetColors(jn["objs"], 18) : new List<Color>
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

            beatmapTheme.backgroundColors = jn["bgs"] != null ? SetColors(jn["bgs"], 9, "BG Hex code does not exist for some reason") : new List<Color>
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

            beatmapTheme.effectColors = jn["fx"] != null ? SetColors(jn["fx"], 18) : beatmapTheme.objectColors.Clone();

            return beatmapTheme;
        }

        public JSONNode ToJSONVG()
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

        public JSONNode ToJSON()
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

        public static List<Color> SetColors(JSONNode jn, int count, string errorMsg = "", bool alpha = true)
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

        public static int IDLength => 7;
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

        public new void ClearBeatmap()
        {
            playerColors.Clear();
            objectColors.Clear();
            backgroundColors.Clear();
            id = LSText.randomNumString(IDLength);
            name = DefaultName;
            backgroundColor = new Color(DefaultBGColor.r, DefaultBGColor.g, DefaultBGColor.b, DefaultBGColor.a);
            guiColor = new Color(DefaultGUIColor.r, DefaultGUIColor.g, DefaultGUIColor.b, DefaultGUIColor.a);
            guiAccentColor = new Color(DefaultTailColor.r, DefaultTailColor.g, DefaultTailColor.b, DefaultTailColor.a);

            playerColors.AddRange(DefaultPlayerColors.Select(x => new Color(x.r, x.g, x.b, x.a)));
            objectColors.AddRange(DefaultObjectColors.Select(x => new Color(x.r, x.g, x.b, x.a)));
            backgroundColors.AddRange(DefaulBackgroundColors.Select(x => new Color(x.r, x.g, x.b, x.a)));
            effectColors.AddRange(DefaultEffectColors.Select(x => new Color(x.r, x.g, x.b, x.a)));

            //guiColor = LSColors.white;
            //guiAccentColor = LSColors.white;
            //backgroundColor = LSColors.gray900;
            //playerColors.Add(LSColors.HexToColor("E57373"));
            //playerColors.Add(LSColors.HexToColor("64B5F6"));
            //playerColors.Add(LSColors.HexToColor("81C784"));
            //playerColors.Add(LSColors.HexToColor("FFB74D"));

            //objectColors.Add(LSColors.gray100);
            //objectColors.Add(LSColors.gray200);
            //objectColors.Add(LSColors.gray300);
            //objectColors.Add(LSColors.gray400);
            //objectColors.Add(LSColors.gray500);
            //objectColors.Add(LSColors.gray600);
            //objectColors.Add(LSColors.gray700);
            //objectColors.Add(LSColors.gray800);
            //objectColors.Add(LSColors.gray900);
            //objectColors.Add(LSColors.gray100);
            //objectColors.Add(LSColors.gray200);
            //objectColors.Add(LSColors.gray300);
            //objectColors.Add(LSColors.gray400);
            //objectColors.Add(LSColors.gray500);
            //objectColors.Add(LSColors.gray600);
            //objectColors.Add(LSColors.gray700);
            //objectColors.Add(LSColors.gray800);
            //objectColors.Add(LSColors.gray900);

            //effectColors.Add(LSColors.gray100);
            //effectColors.Add(LSColors.gray200);
            //effectColors.Add(LSColors.gray300);
            //effectColors.Add(LSColors.gray400);
            //effectColors.Add(LSColors.gray500);
            //effectColors.Add(LSColors.gray600);
            //effectColors.Add(LSColors.gray700);
            //effectColors.Add(LSColors.gray800);
            //effectColors.Add(LSColors.gray900);
            //effectColors.Add(LSColors.gray100);
            //effectColors.Add(LSColors.gray200);
            //effectColors.Add(LSColors.gray300);
            //effectColors.Add(LSColors.gray400);
            //effectColors.Add(LSColors.gray500);
            //effectColors.Add(LSColors.gray600);
            //effectColors.Add(LSColors.gray700);
            //effectColors.Add(LSColors.gray800);
            //effectColors.Add(LSColors.gray900);

            //backgroundColors.Add(LSColors.pink100);
            //backgroundColors.Add(LSColors.pink200);
            //backgroundColors.Add(LSColors.pink300);
            //backgroundColors.Add(LSColors.pink400);
            //backgroundColors.Add(LSColors.pink500);
            //backgroundColors.Add(LSColors.pink600);
            //backgroundColors.Add(LSColors.pink700);
            //backgroundColors.Add(LSColors.pink800);
            //backgroundColors.Add(LSColors.pink900);
        }

        public void Lerp(BeatmapTheme _start, BeatmapTheme _end, float _val)
        {
            guiColor = Color.Lerp(_start.guiColor, _end.guiColor, _val);
            guiAccentColor = Color.Lerp(_start.guiAccentColor, _end.guiAccentColor, _val);
            backgroundColor = Color.Lerp(_start.backgroundColor, _end.backgroundColor, _val);
            for (int i = 0; i < 4; i++)
            {
                if (_start.playerColors[i] != null && _end.playerColors[i] != null)
                {
                    playerColors[i] = Color.Lerp(_start.GetPlayerColor(i), _end.GetPlayerColor(i), _val);
                }
            }

            int maxObj = 9;
            if (_start.objectColors.Count > 9 && _end.objectColors.Count > 9)
            {
                maxObj = 18;
            }

            for (int j = 0; j < maxObj; j++)
            {
                if (_start.objectColors[j] != null && _end.objectColors[j] != null)
                {
                    objectColors[j] = Color.Lerp(_start.GetObjColor(j), _end.GetObjColor(j), _val);
                }
            }

            for (int k = 0; k < 9; k++)
            {
                if (_start.backgroundColors[k] != null && _end.backgroundColors[k] != null)
                {
                    backgroundColors[k] = Color.Lerp(_start.GetBGColor(k), _end.GetBGColor(k), _val);
                }
            }

            for (int k = 0; k < 18; k++)
            {
                if (_start.effectColors[k] != null && _end.effectColors[k] != null)
                {
                    effectColors[k] = Color.Lerp(_start.GetFXColor(k), _end.GetFXColor(k), _val);
                }
            }
        }

        public Color GetFXColor(int _val) => effectColors[Mathf.Clamp(_val, 0, effectColors.Count - 1)];

        #endregion

        #region Operators

        public static implicit operator bool(BeatmapTheme exists) => exists != null;

        public override bool Equals(object obj) => obj is BeatmapTheme && id == (obj as BeatmapTheme).id;

        public override string ToString() => $"{id}: {name}";

        #endregion
    }
}
