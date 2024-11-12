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

        public static MenuGridLayout Parse(JSONNode jn)
        {
            var gridLayout = new MenuGridLayout
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
            };

            return gridLayout;
        }
    }
}
