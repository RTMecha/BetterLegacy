﻿using System;

using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Editor
{
    /// <summary>
    /// Library of sprites to use in the editor.
    /// </summary>
    public class EditorSprites
    {
        /// <summary>
        /// Initializes default sprites.
        /// </summary>
        public static void Init()
        {
            try
            {
                AddSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_add{FileFormat.PNG.Dot()}"));
                EditSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_edit{FileFormat.PNG.Dot()}"));
                CloseSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_close{FileFormat.PNG.Dot()}"));
                DropperSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_dropper{FileFormat.PNG.Dot()}"));
                SearchSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_search{FileFormat.PNG.Dot()}"));
                ReloadSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_refresh-white{FileFormat.PNG.Dot()}"));
                CheckmarkSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_checkmark{FileFormat.PNG.Dot()}"));
                PlayerSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_player{FileFormat.PNG.Dot()}"));

                NewSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_new{FileFormat.PNG.Dot()}"));
                OpenSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_open{FileFormat.PNG.Dot()}"));

                LinkSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_link{FileFormat.PNG.Dot()}"));
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to load sprites. Do they not exist or are they corrupted?\nException: {ex}");
            }
        }

        #region Sprites

        public static Sprite AddSprite { get; set; }

        public static Sprite EditSprite { get; set; }

        public static Sprite CloseSprite { get; set; }

        public static Sprite DropperSprite { get; set; }

        public static Sprite SearchSprite { get; set; }

        public static Sprite ReloadSprite { get; set; }

        public static Sprite CheckmarkSprite { get; set; }

        public static Sprite PlayerSprite { get; set; }

        public static Sprite NewSprite { get; set; }

        public static Sprite OpenSprite { get; set; }

        public static Sprite LinkSprite { get; set; }

        public static Sprite DottedLineSprite { get; set; }

        #endregion
    }
}
