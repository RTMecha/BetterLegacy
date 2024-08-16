using BetterLegacy.Core;
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
                cellSize = jn["cell_size"].AsVector2(),
                spacing = jn["spacing"].AsVector2(),

                constraintCount = jn["constraint_count"].AsInt,
                constraint = (GridLayoutGroup.Constraint)jn["constraint"].AsInt,
                startCorner = (GridLayoutGroup.Corner)jn["start_corner"].AsInt,
                startAxis = (GridLayoutGroup.Axis)jn["start_axis"].AsInt,

                childAlignment = (TextAnchor)jn["align"].AsInt,

                rectJSON = jn["rect"],
            };

            return gridLayout;
        }
    }
}
