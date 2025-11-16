using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Elements
{
    public class PlayerModelPanel : EditorPanel<PlayerModel>
    {
        #region Values

        #region UI

        /// <summary>
        /// The icon of the level panel.
        /// </summary>
        public Image IconImage { get; set; }

        #endregion

        #region Data

        public override string DisplayName => Item.basePart.name;

        #endregion

        #region Asset Pack

        public static RectValues baseRect = new RectValues(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(584f, 32f));

        public static RectValues labelRect = RectValues.FullAnchored.AnchoredPosition(32f, 0f).SizeDelta(-12f, -8f);

        public static RectValues iconRect = RectValues.Default.AnchoredPosition(-276f, 0f).SizeDelta(26f, 26f);

        public static RectValues deleteRect = new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(32f, 0f));

        //public static string labelFormat = "{0}";

        public static TextAnchor labelAlignment = TextAnchor.MiddleLeft;

        public static HorizontalWrapMode labelHorizontalWrap = HorizontalWrapMode.Wrap;

        public static VerticalWrapMode labelVerticalWrap = VerticalWrapMode.Truncate;

        public static int labelFontSize = 20;

        #endregion

        #endregion

        public override void Init(PlayerModel item)
        {
            Item = item;
            //Path = item.path;
            item.editorPanel = this;

            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(EditorLevelManager.inst.LevelCollectionPopup.Content, $"Folder [{Name}]");
            baseRect.AssignToRectTransform(gameObject.transform.AsRT());
            GameObject = gameObject;
            var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();

            HoverFocus = gameObject.AddComponent<HoverUI>();
            HoverFocus.animatePos = false;
            HoverFocus.animateSca = true;

            var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
            Label = folderButtonStorage.label;
            Label.enabled = true;
            labelRect.AssignToRectTransform(Label.rectTransform);
            folderButtonStorage.OnClick.ClearAll();
            Button = folderButtonFunction;
            EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(folderButtonStorage.label);

            var iconBase = Creator.NewUIObject("icon base", gameObject.transform);
            var iconBaseImage = iconBase.AddComponent<Image>();
            iconBase.AddComponent<Mask>().showMaskGraphic = false;
            iconRect.AssignToRectTransform(iconBaseImage.rectTransform);
            EditorThemeManager.ApplyGraphic(iconBaseImage, ThemeGroup.Null, true);

            var icon = Creator.NewUIObject("icon", iconBase.transform);
            RectValues.FullAnchored.AssignToRectTransform(icon.transform.AsRT());

            var iconImage = icon.AddComponent<Image>();
            iconImage.sprite = EditorSprites.PlayerSprite;
            IconImage = iconImage;

            //SelectedUI = Creator.NewUIObject("selected", gameObject.transform);
            //SelectedUI.SetActive(false);
            //var selectedImage = SelectedUI.AddComponent<Image>();
            //selectedImage.color = LSColors.HexToColorAlpha("0088FF25");

            //RectValues.FullAnchored.AssignToRectTransform(selectedImage.rectTransform);

            Render();
        }

        public override void Render()
        {
            RenderLabel();
            RenderHover();
            RenderTooltip();
            UpdateFunction();
        }

        public override void RenderLabel(string text)
        {

        }

        public void UpdateFunction()
        {

        }
    }
}
