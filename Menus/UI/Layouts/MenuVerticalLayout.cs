using UnityEngine.UI;

using SimpleJSON;
using UnityEngine;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Menus.UI.Layouts
{
    public class MenuVerticalLayout : MenuHorizontalOrVerticalLayout
    {
        public VerticalLayoutGroup verticalLayout;

        public static MenuVerticalLayout Parse(JSONNode jn)
        {
            var verticalLayout = new MenuVerticalLayout
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

                rect = RectValues.TryParse(jn["rect"], RectValues.Default),
                mask = jn["mask"].AsBool,
            };

            return verticalLayout;
        }
    }
}
