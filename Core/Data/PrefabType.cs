using BetterLegacy.Core.Helpers;
using LSFunctions;
using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;
using BasePrefabType = DataManager.PrefabType;

namespace BetterLegacy.Core.Data
{
    public class PrefabType : BasePrefabType
    {
        public PrefabType(string name, Color color)
        {
            Name = name;
            Color = color;
            id = LSText.randomNumString(16);
        }

        public string id;

        public Sprite icon;

        public string filePath;

        public bool isDefault;

        public static Dictionary<int, string> prefabTypeLSIndexToID = new Dictionary<int, string>()
        {
            { 0, "7171177971767435" }, // Bombs
            { 1, "7177271762800247" }, // Bullets
            { 2, "9453916730542677" }, // Beams
            { 3, "1644373336900132" }, // Spinners
            { 4, "4045878933744147" }, // Pulses
            { 5, "4965490066612888" }, // Characters
            { 6, "0236192376944164" }, // Misc 1
            { 7, "8073495542187348" }, // Misc 2
            { 8, "5347376169954072" }, // Misc 3
            { 9, "4727401743041957" }, // Misc 4
        };

        public static Dictionary<int, string> prefabTypeVGIndexToID = new Dictionary<int, string>()
        {
            { 0, "4965490066612888" }, // Characters
            { 1, "9570474999546241" }, // Character Parts
            { 2, "5992953518858497" }, // Props
            { 3, "7177271762800247" }, // Bullets
            { 4, "4045878933744147" }, // Pulses
            { 5, "7171177971767435" }, // Bombs
            { 6, "1644373336900132" }, // Spinners
            { 7, "9453916730542677" }, // Beams
            { 8, "0689518742909570" }, // Static
            { 9, "0236192376944164" }, // Misc 1
            { 10, "8073495542187348" }, // Misc 2
            { 11, "5347376169954072" }, // Misc 3
        };

        public static Dictionary<string, int> prefabTypeLSIDToIndex = new Dictionary<string, int>()
        {
            { "7171177971767435", 0 }, // Bombs
            { "7177271762800247", 1 }, // Bullets
            { "9453916730542677", 2 }, // Beams
            { "1644373336900132", 3 }, // Spinners
            { "4045878933744147", 4 }, // Pulses
            { "4965490066612888", 5 }, // Characters
            { "0236192376944164", 6 }, // Misc 1
            { "8073495542187348", 7 }, // Misc 2
            { "5347376169954072", 8 }, // Misc 3
            { "4727401743041957", 9 }, // Misc 4
        };

        public static Dictionary<string, int> prefabTypeVGIDToIndex = new Dictionary<string, int>()
        {
            { "4965490066612888", 0 }, // Characters
            { "9570474999546241", 1 }, // Character Parts
            { "5992953518858497", 2 }, // Props
            { "7177271762800247", 3 }, // Bullets
            { "4045878933744147", 4 }, // Pulses
            { "7171177971767435", 5 }, // Bombs
            { "1644373336900132", 6 }, // Spinners
            { "9453916730542677", 7 }, // Beams
            { "0689518742909570", 8 }, // Static
            { "0236192376944164", 9 }, // Misc 1
            { "8073495542187348", 10 }, // Misc 2
            { "5347376169954072", 11 }, // Misc 3
        };

        public void AssignColor(string _val) => Color = _val.Length == 8 ? LSColors.HexToColorAlpha(_val) : _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;

        public static PrefabType DeepCopy(PrefabType og, bool newID = true) => new PrefabType(og.Name, og.Color)
        {
            id = newID ? LSText.randomNumString(16) : og.id,
            icon = og.icon,
        };

        public static PrefabType Parse(JSONNode jn, bool isDefault = false)
        {
            var prefabType = new PrefabType(jn["name"], LSColors.HexToColorAlpha(jn["color"]));
            prefabType.isDefault = isDefault;
            prefabType.id = jn["id"] ?? LSText.randomNumString(16);
            try
            {
                prefabType.icon = jn["icon"] == null ? null : SpriteHelper.StringToSprite(jn["icon"]);
            }
            catch
            {

            }

            return prefabType;
        }
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["name"] = Name;
            jn["color"] = CoreHelper.ColorToHex(Color);
            jn["id"] = id;

            try
            {
                if (icon && icon.texture)
                    jn["icon"] = SpriteHelper.SpriteToString(icon);
            }
            catch
            {

            }

            return jn;
        }

        static PrefabType invalidType;
        public static PrefabType InvalidType
        {
            get
            {
                if (!invalidType)
                    invalidType = new PrefabType("invalid", Color.red) { icon = LegacyPlugin.AtanPlaceholder };
                return invalidType;
            }
        }


        #region Operators

        public static implicit operator bool(PrefabType exists) => exists != null;

        public override string ToString() => Name;

        #endregion
    }
}
