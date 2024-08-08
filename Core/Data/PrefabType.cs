using BetterLegacy.Core.Helpers;
using SimpleJSON;
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
        }

        public string id;

        public int index;

        public Sprite icon;

        public static PrefabType Parse(JSONNode jn) => new PrefabType(jn["name"], LSFunctions.LSColors.HexToColorAlpha(jn["color"]));
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["name"] = Name;
            jn["color"] = CoreHelper.ColorToHex(Color);
            jn["index"] = index.ToString();

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

        public override string ToString() => Name;

        #endregion
    }
}
