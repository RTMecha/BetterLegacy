using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Network;

namespace BetterLegacy.Menus.UI.Layouts
{
    /// <summary>
    /// Represents a layout that is a grid.
    /// </summary>
    public class MenuGridLayout : MenuLayoutBase
    {
        /// <summary>
        /// Unity component reference.
        /// </summary>
        public GridLayoutGroup gridLayout;

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
        /// Minimum horizontal scroll.
        /// </summary>
        public float minHorizontalScroll = -100f;

        /// <summary>
        /// Maximum horizontal scroll.
        /// </summary>
        public float maxHorizontalScroll = 100f;

        /// <summary>
        /// Minimum vertical scroll.
        /// </summary>
        public float minVerticalScroll = -100f;

        /// <summary>
        /// Maximum vertical scroll.
        /// </summary>
        public float maxVerticalScroll = 100f;

        public override void ReadPacket(NetworkReader reader)
        {
            base.ReadPacket(reader);
            cellSize = reader.ReadVector2();
            spacing = reader.ReadVector2();
            constraintCount = reader.ReadInt32();
            constraint = (GridLayoutGroup.Constraint)reader.ReadByte();
            startCorner = (GridLayoutGroup.Corner)reader.ReadByte();
            minHorizontalScroll = reader.ReadSingle();
            maxHorizontalScroll = reader.ReadSingle();
            minVerticalScroll = reader.ReadSingle();
            maxVerticalScroll = reader.ReadSingle();
        }

        public override void WritePacket(NetworkWriter writer)
        {
            base.WritePacket(writer);
            writer.Write(cellSize);
            writer.Write(spacing);
            writer.Write(constraintCount);
            writer.Write((byte)constraint);
            writer.Write((byte)startCorner);
            writer.Write(minHorizontalScroll);
            writer.Write(maxHorizontalScroll);
            writer.Write(minVerticalScroll);
            writer.Write(maxVerticalScroll);
        }

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

            onScrollUpFuncJSON = jn["on_scroll_up_func"],
            onScrollDownFuncJSON = jn["on_scroll_down_func"],
        };

        /// <summary>
        /// Scrolls the layout by a set amount.
        /// </summary>
        /// <param name="x">Value to scroll on the X axis.</param>
        /// <param name="y">Value to scroll on the Y axis.</param>
        /// <param name="xAdditive">If the scroll X should be additive.</param>
        /// <param name="yAdditive">If the scroll Y should be additive.</param>
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
