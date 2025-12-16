
using SimpleJSON;

namespace BetterLegacy.Menus.UI.Layouts
{
    /// <summary>
    /// Represents a layout that can be horizontal or vertical.
    /// </summary>
    public abstract class MenuHorizontalOrVerticalLayout : MenuLayoutBase
    {
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
        /// Minimum scroll amount.
        /// </summary>
        public float minScroll = -100f;

        /// <summary>
        /// Maximum scroll amount.
        /// </summary>
        public float maxScroll = 100f;

        public override void Read(JSONNode jn)
        {
            base.Read(jn);

            childControlHeight = jn["control_height"].AsBool;
            childControlWidth = jn["control_width"].AsBool;
            childForceExpandHeight = jn["force_expand_height"].AsBool;
            childForceExpandWidth = jn["force_expand_width"].AsBool;
            childScaleHeight = jn["scale_height"].AsBool;
            childScaleWidth = jn["scale_width"].AsBool;
            spacing = jn["spacing"].AsFloat;

            minScroll = jn["min_scroll"] == null ? -100f : jn["min_scroll"].AsFloat;
            maxScroll = jn["max_scroll"] == null ? 100f : jn["max_scroll"].AsFloat;
        }

        /// <summary>
        /// Scrolls the layout by a set amount.
        /// </summary>
        /// <param name="value">Value to scroll.</param>
        /// <param name="additive">If the scroll should be additive.</param>
        public abstract void Scroll(float value, bool additive);
    }
}
