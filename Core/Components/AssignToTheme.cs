using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// Assigns a theme color slot to a graphic.
    /// </summary>
    public class AssignToTheme : MonoBehaviour
    {
        /// <summary>
        /// Graphic to assign a color to.
        /// </summary>
        public Graphic Graphic { get; set; }
        /// <summary>
        /// Index of the theme color slot.
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// What to use from a theme.
        /// </summary>
        public ThemeSource Source { get; set; } = ThemeSource.Objects;

        void Update()
        {
            if (!gameObject.activeInHierarchy || !Graphic.isActiveAndEnabled)
                return;

            Graphic.color = CoreHelper.CurrentBeatmapTheme.GetColor(Source, Index);
        }
    }
}
