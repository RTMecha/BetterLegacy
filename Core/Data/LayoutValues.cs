using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

namespace BetterLegacy.Core.Data
{
    public abstract class LayoutValues : Exists
    {
        /// <summary>
        /// Alignment of the layout group.
        /// </summary>
        public TextAnchor childAlignment;

        /// <summary>
        /// Padding of the layout group.
        /// </summary>
        public RectOffset padding;

        public static LayoutValues Parse(JSONNode jn)
        {
            if (jn["type"] != null)
            {
                switch (jn["type"].Value.ToLower())
                {
                    case "grid": {
                            var layout = GridLayoutValues.Default;
                            layout.ReadJSON(jn);
                            return layout;
                        }
                    case "horizontal": 
                    case "vertical": {
                            var layout = HorizontalOrVerticalLayoutValues.Default;
                            layout.ReadJSON(jn);
                            return layout;
                        }
                }
            }

            var l = GridLayoutValues.Default;
            l.ReadJSON(jn);
            return l;
        }

        public virtual void ReadJSON(JSONNode jn)
        {
            if (jn["child_alignment"] != null)
                childAlignment = (TextAnchor)jn["child_alignment"].AsInt;
            if (jn["padding"] != null)
                padding = new RectOffset
                {
                    top = jn["padding"]["top"].AsInt,
                    bottom = jn["padding"]["bottom"].AsInt,
                    left = jn["padding"]["left"].AsInt,
                    right = jn["padding"]["right"].AsInt,
                };
        }

        public void AssignPadding(LayoutGroup layout)
        {
            if (!layout || padding == null)
                return;

            layout.padding.top = padding.top;
            layout.padding.bottom = padding.bottom;
            layout.padding.left = padding.left;
            layout.padding.right = padding.right;
        }
    }

    public class GridLayoutValues : LayoutValues
    {
        public static GridLayoutValues Default => new GridLayoutValues
        {
            cellSize = new Vector2(100f, 100f),
            spacing = Vector2.zero,
            constraintCount = 2,
            constraint = GridLayoutGroup.Constraint.Flexible,
            startCorner = GridLayoutGroup.Corner.UpperLeft,
            startAxis = GridLayoutGroup.Axis.Horizontal,
            childAlignment = TextAnchor.UpperLeft,
        };

        /// <summary>
        /// Size of each cell.
        /// </summary>
        public Vector2 cellSize;

        /// <summary>
        /// Spacing between each cell.
        /// </summary>
        public Vector2 spacing;

        /// <summary>
        /// Controls the amount of row / columns.
        /// </summary>
        public int constraintCount;

        /// <summary>
        /// Row / column behavior.
        /// </summary>
        public GridLayoutGroup.Constraint constraint;

        /// <summary>
        /// Start corner.
        /// </summary>
        public GridLayoutGroup.Corner startCorner;

        /// <summary>
        /// Start axis.
        /// </summary>
        public GridLayoutGroup.Axis startAxis;

        /// <summary>
        /// Parses and applies layout values from JSON.
        /// </summary>
        /// <param name="jn">JSON to read.</param>
        public override void ReadJSON(JSONNode jn)
        {
            if (jn["cell_size"] != null)
                cellSize = Parser.TryParse(jn["cell_size"], new Vector2(100f, 100f));
            if (jn["spacing"] != null)
                spacing = Parser.TryParse(jn["spacing"], Vector2.zero);
            if (jn["constraint_count"] != null)
                constraintCount = jn["constraint_count"].AsInt;
            if (jn["constraint"] != null)
                constraint = (GridLayoutGroup.Constraint)jn["constraint"].AsInt;
            if (jn["start_corner"] != null)
                startCorner = (GridLayoutGroup.Corner)jn["start_corner"].AsInt;
            if (jn["start_axis"] != null)
                startAxis = (GridLayoutGroup.Axis)jn["start_axis"].AsInt;

            base.ReadJSON(jn);
        }

        /// <summary>
        /// Assigns the current Layout Values to a <see cref="GridLayoutGroup"/>.
        /// </summary>
        /// <param name="layout">Layout to assign to.</param>
        public void AssignToLayout(GridLayoutGroup layout)
        {
            if (!layout)
                return;

            layout.cellSize = cellSize;
            layout.spacing = spacing;
            layout.constraintCount = constraintCount;
            layout.constraint = constraint;
            layout.startCorner = startCorner;
            layout.startAxis = startAxis;
            layout.childAlignment = childAlignment;

            AssignPadding(layout);
        }
    }

    public class HorizontalOrVerticalLayoutValues : LayoutValues
    {
        public static HorizontalOrVerticalLayoutValues Default => new HorizontalOrVerticalLayoutValues
        {
            childControlHeight = true,
            childControlWidth = true,
            childForceExpandHeight = true,
            childForceExpandWidth = true,
            childScaleHeight = true,
            childScaleWidth = true,
            spacing = 0f,
            childAlignment = TextAnchor.UpperLeft,
        };

        /// <summary>
        /// If the layouts' children control the height of the layout.
        /// </summary>
        public bool childControlHeight;

        /// <summary>
        /// If the layouts' children control the width of the layout.
        /// </summary>
        public bool childControlWidth;

        /// <summary>
        /// If the layouts' children force expand the height of the layout.
        /// </summary>
        public bool childForceExpandHeight;

        /// <summary>
        /// If the layouts' children force expand the width of the layout.
        /// </summary>
        public bool childForceExpandWidth;

        /// <summary>
        /// If the height of the layouts' children scale with the layout.
        /// </summary>
        public bool childScaleHeight;

        /// <summary>
        /// If the width of the layouts' children scale with the layout.
        /// </summary>
        public bool childScaleWidth;

        /// <summary>
        /// Spacing between each child.
        /// </summary>
        public float spacing;

        /// <summary>
        /// Parses and applies layout values from JSON.
        /// </summary>
        /// <param name="jn">JSON to read.</param>
        public override void ReadJSON(JSONNode jn)
        {
            if (jn["control_height"] != null)
                childControlHeight = jn["control_height"].AsBool;
            if (jn["control_width"] != null)
                childControlWidth = jn["control_width"].AsBool;
            if (jn["force_expand_height"] != null)
                childForceExpandHeight = jn["force_expand_height"].AsBool;
            if (jn["force_expand_width"] != null)
                childForceExpandWidth = jn["force_expand_width"].AsBool;
            if (jn["scale_height"] != null)
                childScaleHeight = jn["scale_height"].AsBool;
            if (jn["scale_width"] != null)
                childScaleWidth = jn["scale_width"].AsBool;
            if (jn["spacing"] != null)
                spacing = jn["spacing"].AsFloat;

            base.ReadJSON(jn);
        }

        /// <summary>
        /// Assigns the current Layout Values to a <see cref="HorizontalOrVerticalLayoutGroup"/>.
        /// </summary>
        /// <param name="layout">Layout to assign to.</param>
        public void AssignToLayout(HorizontalOrVerticalLayoutGroup layout)
        {
            if (!layout)
                return;

            layout.childControlHeight = childControlHeight;
            layout.childControlWidth = childControlWidth;
            layout.childForceExpandHeight = childForceExpandHeight;
            layout.childForceExpandWidth = childForceExpandWidth;
            layout.childScaleHeight = childScaleHeight;
            layout.childScaleWidth = childScaleWidth;
            layout.spacing = spacing;
            layout.childAlignment = childAlignment;

            AssignPadding(layout);
        }
    }
}
