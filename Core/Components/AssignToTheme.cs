﻿using BetterLegacy.Core.Helpers;
using UnityEngine;
using UnityEngine.UI;

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

            switch (ThemeType)
            {
                case Type.GUI:
                    {
                        Graphic.color = CoreHelper.CurrentBeatmapTheme.guiColor;
                        break;
                    }
                case Type.PlayerTail:
                    {
                        Graphic.color = CoreHelper.CurrentBeatmapTheme.guiAccentColor;
                        break;
                    }
                case Type.Background:
                    {
                        Graphic.color = CoreHelper.CurrentBeatmapTheme.backgroundColor;
                        break;
                    }
                case Type.Player:
                    {
                        Graphic.color = CoreHelper.CurrentBeatmapTheme.GetPlayerColor(Index);
                        break;
                    }
                case Type.Objects:
                    {
                        Graphic.color = CoreHelper.CurrentBeatmapTheme.GetObjColor(Index);
                        break;
                    }
                case Type.BackgroundObjects:
                    {
                        Graphic.color = CoreHelper.CurrentBeatmapTheme.GetBGColor(Index);
                        break;
                    }
                case Type.Effects:
                    {
                        Graphic.color = CoreHelper.CurrentBeatmapTheme.GetFXColor(Index);
                        break;
                    }
            }
        }
    }
}
