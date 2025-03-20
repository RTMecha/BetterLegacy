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
        public Type ThemeType { get; set; } = Type.Objects;
        public enum Type
        {
            GUI,
            Background,
            Player,
            PlayerTail,
            Objects,
            BackgroundObjects,
            Effects
        }

        void Update()
        {
            if (!gameObject.activeInHierarchy || !Graphic.isActiveAndEnabled)
                return;

            Graphic.color = ThemeType switch
            {
                Type.GUI => CoreHelper.CurrentBeatmapTheme.guiColor,
                Type.PlayerTail => CoreHelper.CurrentBeatmapTheme.guiAccentColor,
                Type.Background => CoreHelper.CurrentBeatmapTheme.backgroundColor,
                Type.Player => CoreHelper.CurrentBeatmapTheme.GetPlayerColor(Index),
                Type.Objects => CoreHelper.CurrentBeatmapTheme.GetObjColor(Index),
                Type.BackgroundObjects => CoreHelper.CurrentBeatmapTheme.GetBGColor(Index),
                Type.Effects => CoreHelper.CurrentBeatmapTheme.GetFXColor(Index),
                _ => Color.white,
            };
        }
    }
}
