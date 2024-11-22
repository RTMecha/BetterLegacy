using UnityEngine.UI;

using SimpleJSON;
using UnityEngine;
using BetterLegacy.Core.Data;
using BetterLegacy.Core;

namespace BetterLegacy.Menus.UI.Layouts
{
    public class MenuVerticalLayout : MenuHorizontalOrVerticalLayout
    {
        public VerticalLayoutGroup verticalLayout;

        public static MenuVerticalLayout Parse(JSONNode jn) => new MenuVerticalLayout
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
            regenerate = jn["regenerate"] == null || jn["regenerate"].AsBool,

            scrollable = jn["scrollable"].AsBool,
            minScroll = jn["min_scroll"] == null ? -100f : jn["min_scroll"].AsFloat,
            maxScroll = jn["max_scroll"] == null ? 100f : jn["max_scroll"].AsFloat,

            onScrollUpFuncJSON = jn["on_scroll_up_func"],
            onScrollDownFuncJSON = jn["on_scroll_down_func"],
        };

        public void Scroll(float value, bool additive)
        {
            var content = this.content.transform.AsRT();

            if (additive)
                value += content.anchoredPosition.y;

            value = Mathf.Clamp(value, minScroll, maxScroll);

            content.anchoredPosition = new Vector2(content.anchoredPosition.x, value);
        }
    }
}
