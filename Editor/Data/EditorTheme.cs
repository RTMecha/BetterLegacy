using System;
using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents a theme for the editor to use.
    /// </summary>
    public class EditorTheme : PAObject<EditorTheme>
    {
        public EditorTheme() { }

        #region Values

        /// <summary>
        /// Name of the editor theme.
        /// </summary>
        public string name;

        /// <summary>
        /// Dictionary of colors in the editor theme.
        /// </summary>
        public Dictionary<ThemeGroup, Color> ColorGroups { get; set; }

        #endregion

        #region Functions

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

        /// <summary>
        /// Gets a layer's color.
        /// </summary>
        /// <param name="layer">Layer number.</param>
        /// <returns>Returns a color based on the layer.</returns>
        public Color GetLayerColor(int layer) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetLayerThemeGroup(layer), Color.white);

        /// <summary>
        /// Gets a color that represents a tab.
        /// </summary>
        /// <param name="tab">Tab number.</param>
        /// <returns>Returns a color based on a tab.</returns>
        public Color GetTabThemeColor(int layer) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetTabThemeGroup(layer), Color.white);

        /// <summary>
        /// Gets an event's color.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        /// <returns>Returns a color based on an event.</returns>
        public Color GetEventColor(int type) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetEventColorThemeGroup(type), Color.white);

        /// <summary>
        /// Gets an event keyframe's color.
        /// </summary>
        /// <param name="type">Type of the event keyframe.</param>
        /// <returns>Returns a color based on an event.</returns>
        public Color GetEventKeyframeColor(int type) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetEventColorKeyframeThemeGroup(type), Color.white);

        /// <summary>
        /// Gets an event editor's color.
        /// </summary>
        /// <param name="type">Type of the event editor.</param>
        /// <returns>Returns a color based on an event editor.</returns>
        public Color GetEventColorEditorColor(int type) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetEventColorEditorThemeGroup(type), Color.white);

        /// <summary>
        /// Gets an object keyframe's color.
        /// </summary>
        /// <param name="type">Type of the object keyframe.</param>
        /// <returns>Returns a color based on an object keyframe.</returns>
        public Color GetObjectKeyframeColor(int type) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetObjectKeyframeThemeGroup(type), Color.white);

        /// <summary>
        /// Gets a notification types' color.
        /// </summary>
        /// <param name="type">Type of the notification.</param>
        /// <returns>Returns a color based on a notification type.</returns>
        public Color GetNotificationColor(EditorManager.NotificationType type) => ColorGroups.GetValueOrDefault(EditorThemeManager.GetNotificationThemeGroup(type), Color.white);

        #endregion
    }
}
