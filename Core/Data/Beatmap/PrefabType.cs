using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// A <see cref="Prefab"/> group. Displays an icon and color for the Prefab references.
    /// </summary>
    public class PrefabType : PAObject<PrefabType>, IPacket
    {
        #region Constructors

        public PrefabType() => id = GetNumberID();

        public PrefabType(string name, Color color) : this()
        {
            this.name = name;
            this.color = color;
        }

        #endregion

        #region Values

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

        /// <summary>
        /// If the Prefab Type is a default one.
        /// </summary>
        public bool isDefault;

        const string BOMBS = "7171177971767435";
        const string BULLETS = "7177271762800247";
        const string BEAMS = "9453916730542677";
        const string SPINNERS = "1644373336900132";
        const string PULSES = "4045878933744147";
        const string CHARACTERS = "4965490066612888";
        const string CHARACTER_PARTS = "9570474999546241";
        const string PROPS = "5992953518858497";
        const string STATIC = "0689518742909570";
        const string MISC_1 = "0236192376944164";
        const string MISC_2 = "8073495542187348";
        const string MISC_3 = "5347376169954072";
        const string MISC_4 = "4727401743041957";

        /// <summary>
        /// Converts LS Prefab Type Index to ID.
        /// </summary>
        public static Dictionary<int, string> LSIndexToID { get; } = new Dictionary<int, string>()
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

        /// <summary>
        /// Converts VG Prefab Type Index to ID.
        /// </summary>
        public static Dictionary<int, string> VGIndexToID { get; } = new Dictionary<int, string>()
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

        /// <summary>
        /// Converts LS Prefab Type ID to Index.
        /// </summary>
        public static Dictionary<string, int> LSIDToIndex { get; } = new Dictionary<string, int>()
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

        /// <summary>
        /// Converts VG Prefab Type ID to Index.
        /// </summary>
        public static Dictionary<string, int> VGIDToIndex { get; } = new Dictionary<string, int>()
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

        #region Methods

        /// <summary>
        /// Assigns the color of the Prefab Type.
        /// </summary>
        /// <param name="hex">Hex color to assign.</param>
        public void AssignColor(string hex) => color = hex.Length == 8 ? LSColors.HexToColorAlpha(hex) : hex.Length == 6 ? LSColors.HexToColor(hex) : LSColors.pink500;

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
            jn["id"] = id;
            jn["name"] = name;
            jn["color"] = RTColors.ColorToHex(color);

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

        public void ReadPacket(NetworkReader reader)
        {
            id = reader.ReadString();
            name = reader.ReadString();
            color = reader.ReadColor();
            icon = reader.ReadSprite();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(id);
            writer.Write(name);
            writer.Write(color);
            writer.Write(icon);
        }

        public override string ToString() => name;

        #endregion
    }
}
