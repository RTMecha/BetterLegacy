using System;

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
                PALogo = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_pa_logo{FileFormat.PNG.Dot()}"));

                DownArrow = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_down{FileFormat.PNG.Dot()}"));
                LeftArrow = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_left{FileFormat.PNG.Dot()}"));
                RightArrow = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_right{FileFormat.PNG.Dot()}"));
                UpArrow = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_up{FileFormat.PNG.Dot()}"));

                AddSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_add{FileFormat.PNG.Dot()}"));
                EditSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_edit{FileFormat.PNG.Dot()}"));
                CopySprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_copy{FileFormat.PNG.Dot()}"));
                CloseSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_close{FileFormat.PNG.Dot()}"));
                DropperSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_dropper{FileFormat.PNG.Dot()}"));
                SearchSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_search{FileFormat.PNG.Dot()}"));
                ReloadSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_refresh-white{FileFormat.PNG.Dot()}"));
                CheckmarkSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_checkmark{FileFormat.PNG.Dot()}"));
                PauseSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_pause{FileFormat.PNG.Dot()}"));
                PlaySprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_play{FileFormat.PNG.Dot()}"));
                PlayerSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_player{FileFormat.PNG.Dot()}"));

                NewSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_new{FileFormat.PNG.Dot()}"));
                OpenSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_open{FileFormat.PNG.Dot()}"));

                LinkSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_link{FileFormat.PNG.Dot()}"));
                SoundSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_sound{FileFormat.PNG.Dot()}"));

                FlagStartSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_flag_start{FileFormat.PNG.Dot()}"));
                FlagEndSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_flag_end{FileFormat.PNG.Dot()}"));

                CircleSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_circle{FileFormat.PNG.Dot()}"));

                QuestionSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_question{FileFormat.PNG.Dot()}"));
                ExclaimSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_exclaim{FileFormat.PNG.Dot()}"));
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to load sprites. Do they not exist or are they corrupted?\nException: {ex}");
            }
        }

        #region Sprites

        public static Sprite PALogo { get; set; }

        #region Directions

        public static Sprite DownArrow { get; set; }
        public static Sprite LeftArrow { get; set; }
        public static Sprite RightArrow { get; set; }
        public static Sprite UpArrow { get; set; }

        #endregion

        public static Sprite AddSprite { get; set; }

        public static Sprite EditSprite { get; set; }

        public static Sprite CopySprite { get; set; }

        public static Sprite CloseSprite { get; set; }

        public static Sprite DropperSprite { get; set; }

        public static Sprite SearchSprite { get; set; }

        public static Sprite ReloadSprite { get; set; }

        public static Sprite CheckmarkSprite { get; set; }

        public static Sprite PauseSprite { get; set; }

        public static Sprite PlaySprite { get; set; }

        public static Sprite PlayerSprite { get; set; }

        public static Sprite NewSprite { get; set; }

        public static Sprite OpenSprite { get; set; }

        public static Sprite LinkSprite { get; set; }
        public static Sprite SoundSprite { get; set; }

        public static Sprite DottedLineSprite { get; set; }

        public static Sprite FlagStartSprite { get; set; }

        public static Sprite FlagEndSprite { get; set; }

        public static Sprite CircleSprite { get; set; }

        public static Sprite QuestionSprite { get; set; }
        public static Sprite ExclaimSprite { get; set; }

        #endregion
    }
}
