using System;
using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    public class EditorTheme : PAObject<EditorTheme>
    {
        public EditorTheme() { }

        public string name;

        public Dictionary<ThemeGroup, Color> ColorGroups { get; set; }

        public Color GetLayerColor(int layer) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetLayerThemeGroup(layer), Color.white);
        
        public Color GetTabThemeColor(int layer) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetTabThemeGroup(layer), Color.white);

        public Color GetEventColor(int type) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetEventColorThemeGroup(type), Color.white);
        
        public Color GetEventKeyframeColor(int type) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetEventColorKeyframeThemeGroup(type), Color.white);
        
        public Color GetEventColorEditorColor(int type) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetEventColorEditorThemeGroup(type), Color.white);

        public Color GetObjectKeyframeColor(int type) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetObjectKeyframeThemeGroup(type), Color.white);

        public Color GetNotificationColor(EditorManager.NotificationType type) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetNotificationThemeGroup(type), Color.white);

        public override void CopyData(EditorTheme orig, bool newID = true)
        {
            name = orig.name;
            ColorGroups = new Dictionary<ThemeGroup, Color>(orig.ColorGroups);
        }

        public override void ReadJSON(JSONNode jn)
        {
            var type = typeof(ThemeGroup);

            var colorGroups = new Dictionary<ThemeGroup, Color>();
            for (int i = 0; i < jn["groups"].Count; i++)
            {
                var colorJN = jn["groups"][i]["color"];
                string name = jn["groups"][i]["name"];
                if (Enum.TryParse(name, out ThemeGroup group))
                    colorGroups[group] = colorJN.IsObject ? new Color(colorJN["r"].AsFloat, colorJN["g"].AsFloat, colorJN["b"].AsFloat, colorJN["a"].AsFloat) : RTColors.HexToColor(colorJN);
            }

            var themeGroups = Enum.GetNames(type);
            for (int i = 0; i < themeGroups.Length; i++)
            {
                var themeGroup = Parser.TryParse(themeGroups[i], true, ThemeGroup.Null);
                colorGroups.TryAdd(themeGroup, Color.black);
            }

            name = jn["name"];
            ColorGroups = colorGroups;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["name"] = name;

            int num = 0;
            foreach (var colorGroup in ColorGroups)
            {
                jn["groups"][num]["name"] = colorGroup.Key.ToString();
                jn["groups"][num]["color"] = RTColors.ColorToHexOptional(colorGroup.Value);
                num++;
            }

            return jn;
        }
    }
}
