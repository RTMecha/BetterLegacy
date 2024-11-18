using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Menus.UI.Layouts
{
    public class MenuGridLayout : MenuLayoutBase
    {
        public GridLayoutGroup gridLayout;
        public Vector2 cellSize;
        public Vector2 spacing;

        public int constraintCount;
        public GridLayoutGroup.Constraint constraint;
        public GridLayoutGroup.Corner startCorner;
        public GridLayoutGroup.Axis startAxis;

        public float minHorizontalScroll = -100f;
        public float maxHorizontalScroll = 100f;
        
        public float minVerticalScroll = -100f;
        public float maxVerticalScroll = 100f;

        public static MenuGridLayout Parse(JSONNode jn) => new MenuGridLayout
        {
            name = jn["name"],
            cellSize = Parser.TryParse(jn["cell_size"], new Vector2(100f, 100f)),
            spacing = Parser.TryParse(jn["spacing"], Vector2.zero),

            constraintCount = jn["constraint_count"].AsInt,
            constraint = (GridLayoutGroup.Constraint)jn["constraint"].AsInt,
            startCorner = (GridLayoutGroup.Corner)jn["start_corner"].AsInt,
            startAxis = (GridLayoutGroup.Axis)jn["start_axis"].AsInt,

            childAlignment = (TextAnchor)jn["align"].AsInt,

            rect = RectValues.TryParse(jn["rect"], RectValues.Default),
            mask = jn["mask"].AsBool,
            regenerate = jn["regenerate"] == null || jn["regenerate"].AsBool,

            scrollable = jn["scrollable"].AsBool,
            minHorizontalScroll = jn["min_horizontal_scroll"] == null ? -100f : jn["min_horizontal_scroll"].AsFloat,
            maxHorizontalScroll = jn["max_horizontal_scroll"] == null ? 100f : jn["max_horizontal_scroll"].AsFloat,
            minVerticalScroll = jn["min_vertical_scroll"] == null ? -100f : jn["min_vertical_scroll"].AsFloat,
            maxVerticalScroll = jn["max_vertical_scroll"] == null ? 100f : jn["max_vertical_scroll"].AsFloat,
        };

        public void Scroll(float x, float y, bool xAdditive, bool yAdditive)
        {
            var content = this.content.transform.AsRT();
            if (xAdditive)
                x += content.anchoredPosition.x;
            if (yAdditive)
                y += content.anchoredPosition.y;

            x = Mathf.Clamp(x, minHorizontalScroll, maxHorizontalScroll);
            y = Mathf.Clamp(y, minVerticalScroll, maxVerticalScroll);

            content.anchoredPosition = new Vector2(x, y);
        }
    }
}
