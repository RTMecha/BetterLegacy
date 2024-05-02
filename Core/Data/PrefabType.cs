using BetterLegacy.Core.Helpers;
using SimpleJSON;
using UnityEngine;
using BasePrefabType = DataManager.PrefabType;

namespace BetterLegacy.Core.Data
{
    public class PrefabType : BasePrefabType
    {
        PrefabType()
        {

        }

        public PrefabType(string name, Color color)
        {
            Name = name;
            Color = color;
        }

        public int Index { get; set; }

        Sprite icon;
        public Sprite Icon
        {
            get => icon;
            set => icon = value;
        }

        public static PrefabType Parse(JSONNode jn) => new PrefabType(jn["name"], LSFunctions.LSColors.HexToColorAlpha(jn["color"]));
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["name"] = Name;
            jn["color"] = CoreHelper.ColorToHex(Color);
            jn["index"] = Index.ToString();

            return jn;
        }

        static PrefabType invalidType;
        public static PrefabType InvalidType
        {
            get
            {
                if (!invalidType)
                    invalidType = new PrefabType("invalid", Color.red);
                return invalidType;
            }
        }


        #region Operators

        public static implicit operator bool(PrefabType exists) => exists != null;

        public override bool Equals(object obj) => obj is PrefabType && Name == (obj as PrefabType).Name;

        public override string ToString() => Name;

        #endregion
    }
}
