using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using LSFunctions;
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

        public static PrefabType DeepCopy(PrefabType og, bool newID = true) => new PrefabType(og.Name, og.Color)
        {
            id = newID ? LSText.randomNumString(16) : og.id,
            icon = og.icon,
        };

        public static PrefabType Parse(JSONNode jn)
        {
            var prefabType = new PrefabType(jn["name"], LSColors.HexToColorAlpha(jn["color"]));

            //try
            //{
            //    prefabType.icon = jn["icon"] == null ? null : SpriteManager.StringToSprite(jn["icon"]);
            //}
            //catch
            //{

            //}

            return prefabType;
        }
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["name"] = Name;
            jn["color"] = CoreHelper.ColorToHex(Color);
            jn["index"] = index.ToString();

            //try
            //{
            //    if (icon && icon.texture)
            //        jn["icon"] = SpriteManager.SpriteToString(icon);
            //}
            //catch
            //{

            //}

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
