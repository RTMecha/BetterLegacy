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
                #region File

                NewSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/file/new{FileFormat.PNG.Dot()}"));
                OpenSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/file/open{FileFormat.PNG.Dot()}"));
                DocumentSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/file/document{FileFormat.PNG.Dot()}"));
                ListSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/file/list{FileFormat.PNG.Dot()}"));

                #endregion

                #region Operations

                DownArrow = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/down{FileFormat.PNG.Dot()}"));
                LeftArrow = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/left{FileFormat.PNG.Dot()}"));
                RightArrow = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/right{FileFormat.PNG.Dot()}"));
                UpArrow = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/up{FileFormat.PNG.Dot()}"));

                AddSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/add{FileFormat.PNG.Dot()}"));
                EditSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/edit{FileFormat.PNG.Dot()}"));
                CopySprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/copy{FileFormat.PNG.Dot()}"));
                CloseSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/close{FileFormat.PNG.Dot()}"));
                DropperSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/dropper{FileFormat.PNG.Dot()}"));
                SearchSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/search{FileFormat.PNG.Dot()}"));
                ReloadSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/refresh{FileFormat.PNG.Dot()}"));
                CheckmarkSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/checkmark{FileFormat.PNG.Dot()}"));
                PauseSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/pause{FileFormat.PNG.Dot()}"));
                PlaySprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/play{FileFormat.PNG.Dot()}"));
                FlagStartSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/flag_start{FileFormat.PNG.Dot()}"));
                FlagEndSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/flag_end{FileFormat.PNG.Dot()}"));

                #endregion

                #region Generic

                PALogo = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/pa_logo{FileFormat.PNG.Dot()}"));

                PlayerSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/player{FileFormat.PNG.Dot()}"));

                LinkSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/link{FileFormat.PNG.Dot()}"));
                SoundSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/sound{FileFormat.PNG.Dot()}"));

                CircleSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/circle{FileFormat.PNG.Dot()}"));

                QuestionSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/question{FileFormat.PNG.Dot()}"));
                ExclaimSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/exclaim{FileFormat.PNG.Dot()}"));

                #endregion
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

        public static Sprite DocumentSprite { get; set; }
        public static Sprite ListSprite { get; set; }

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
