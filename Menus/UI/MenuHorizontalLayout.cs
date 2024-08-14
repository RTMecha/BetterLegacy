using UnityEngine.UI;

using SimpleJSON;
using UnityEngine;

namespace BetterLegacy.Menus.UI
{
    public class MenuHorizontalLayout : MenuHorizontalOrVerticalLayout
    {
        public HorizontalLayoutGroup horizontalLayout;

        public static MenuHorizontalLayout Parse(JSONNode jn)
        {
            var horizontalLayout = new MenuHorizontalLayout
            {
                name = jn["name"],
                childControlHeight = jn["control_height"].AsBool,
                childControlWidth = jn["control_width"].AsBool,
                childForceExpandHeight = jn["force_expand_height"].AsBool,
                childForceExpandWidth = jn["force_expand_width"].AsBool,
                childScaleHeight = jn["scale_height"].AsBool,
                childScaleWidth = jn["scale_width"].AsBool,
                spacing = jn["spacing"].AsFloat,
                childAlignment = (TextAnchor)jn["align"].AsInt,

                rectJSON = jn["rect"],
            };

            return horizontalLayout;
        }
    }
}
