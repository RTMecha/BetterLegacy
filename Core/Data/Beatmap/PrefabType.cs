using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents the type of a prefab.
    /// </summary>
    public class PrefabType : PAObject<PrefabType>
    {
        public PrefabType() => id = GetNumberID();

        public PrefabType(string name, Color color) : this()
        {
            this.name = name;
            this.color = color;
        }

        #region Values

        /// <summary>
        /// Name of the prefab type.
        /// </summary>
        public string name;

        /// <summary>
        /// Color to apply to prefabs using this type.
        /// </summary>
        public Color color;

        /// <summary>
        /// Icon to apply to prefabs using this type.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Path to the prefab type's file.
        /// </summary>
        public string filePath;

        #endregion

        #region Defaults

        static PrefabType invalidType;

        /// <summary>
        /// The invalid prefab type if no prefab types were found.
        /// </summary>
        public static PrefabType InvalidType
        {
            get
            {
                if (!invalidType)
                    invalidType = new PrefabType("invalid", Color.red) { icon = LegacyPlugin.AtanPlaceholder };
                return invalidType;
            }
        }

        public bool isDefault;

        public const string BOMBS = "7171177971767435";
        public const string BULLETS = "7177271762800247";
        public const string BEAMS = "9453916730542677";
        public const string SPINNERS = "1644373336900132";
        public const string PULSES = "4045878933744147";
        public const string CHARACTERS = "4965490066612888";
        public const string CHARACTER_PARTS = "9570474999546241";
        public const string PROPS = "5992953518858497";
        public const string STATIC = "0689518742909570";
        public const string MISC_1 = "0236192376944164";
        public const string MISC_2 = "8073495542187348";
        public const string MISC_3 = "5347376169954072";
        public const string MISC_4 = "4727401743041957";

        public static Dictionary<int, string> prefabTypeLSIndexToID = new Dictionary<int, string>()
        {
            { 0, BOMBS }, // Bombs
            { 1, BULLETS }, // Bullets
            { 2, BEAMS }, // Beams
            { 3, SPINNERS }, // Spinners
            { 4, PULSES }, // Pulses
            { 5, CHARACTERS }, // Characters
            { 6, MISC_1 }, // Misc 1
            { 7, MISC_2 }, // Misc 2
            { 8, MISC_3 }, // Misc 3
            { 9, MISC_4 }, // Misc 4
        };

        public static Dictionary<int, string> prefabTypeVGIndexToID = new Dictionary<int, string>()
        {
            { 0, CHARACTERS }, // Characters
            { 1, CHARACTER_PARTS }, // Character Parts
            { 2, PROPS }, // Props
            { 3, BULLETS }, // Bullets
            { 4, PULSES }, // Pulses
            { 5, BOMBS }, // Bombs
            { 6, SPINNERS }, // Spinners
            { 7, BEAMS }, // Beams
            { 8, STATIC }, // Static
            { 9, MISC_1 }, // Misc 1
            { 10, MISC_2 }, // Misc 2
            { 11, MISC_3 }, // Misc 3
        };

        public static Dictionary<string, int> prefabTypeLSIDToIndex = new Dictionary<string, int>()
        {
            { BOMBS, 0 }, // Bombs
            { BULLETS, 1 }, // Bullets
            { BEAMS, 2 }, // Beams
            { SPINNERS, 3 }, // Spinners
            { PULSES, 4 }, // Pulses
            { CHARACTERS, 5 }, // Characters
            { MISC_1, 6 }, // Misc 1
            { MISC_2, 7 }, // Misc 2
            { MISC_3, 8 }, // Misc 3
            { MISC_4, 9 }, // Misc 4
        };

        public static Dictionary<string, int> prefabTypeVGIDToIndex = new Dictionary<string, int>()
        {
            { CHARACTERS, 0 }, // Characters
            { CHARACTER_PARTS, 1 }, // Character Parts
            { PROPS, 2 }, // Props
            { BULLETS, 3 }, // Bullets
            { PULSES, 4 }, // Pulses
            { BOMBS, 5 }, // Bombs
            { SPINNERS, 6 }, // Spinners
            { BEAMS, 7 }, // Beams
            { STATIC, 8 }, // Static
            { MISC_1, 9 }, // Misc 1
            { MISC_2, 10 }, // Misc 2
            { MISC_3, 11 }, // Misc 3
        };

        #endregion

        #region Methods

        public void AssignColor(string _val) => color = _val.Length == 8 ? LSColors.HexToColorAlpha(_val) : _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;

        public override void CopyData(PrefabType orig, bool newID = true)
        {
            id = newID ? GetNumberID() : orig.id;
            name = orig.name;
            color = orig.color;
            icon = orig.icon;
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? GetNumberID();
            name = jn["name"];
            color = LSColors.HexToColorAlpha(jn["color"]);
            try
            {
                icon = jn["icon"] == null ? null : SpriteHelper.StringToSprite(jn["icon"]);
            }
            catch
            {

            }
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();
            jn["name"] = name;
            jn["color"] = RTColors.ColorToHex(color);
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

        #endregion

        #region Operators

        public static implicit operator bool(PrefabType exists) => exists != null;

        public override string ToString() => name;

        #endregion
    }
}
