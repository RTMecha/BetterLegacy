using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Provides some extra editor functionality via right clicking an element in the editor.
    /// </summary>
    public class EditorContextMenu : MonoBehaviour
    {
        #region Init

        public static EditorContextMenu inst;

        public static void Init() => Creator.NewGameObject(nameof(EditorContextMenu), EditorManager.inst.transform.parent).AddComponent<EditorContextMenu>();

        void Awake()
        {
            inst = this;
            CreateContextMenu();
        }

        /// <summary>
        /// The default width the context menu should have.
        /// </summary>
        public const float DEFAULT_CONTEXT_MENU_WIDTH = 300f;

        GameObject contextMenu;
        RectTransform contextMenuLayout;

        #endregion

        #region Methods

        #region Internal

        void CreateContextMenu()
        {
            try
            {
                var parent = EditorManager.inst.dialogs.parent;

                contextMenu = Creator.NewUIObject("Context Menu", parent, parent.childCount - 2);
                RectValues.Default.AnchorMax(0f, 0f).AnchorMin(0f, 0f).Pivot(0f, 1f).SizeDelta(126f, 300f).AssignToRectTransform(contextMenu.transform.AsRT());
                var contextMenuImage = contextMenu.AddComponent<Image>();

                var contextMenuLayout = Creator.NewUIObject("Context Menu Layout", contextMenu.transform);
                RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(contextMenuLayout.transform.AsRT());
                this.contextMenuLayout = contextMenuLayout.transform.AsRT();

                var contextMenuLayoutVLG = contextMenuLayout.AddComponent<VerticalLayoutGroup>();
                contextMenuLayoutVLG.childControlHeight = false;
                contextMenuLayoutVLG.childForceExpandHeight = false;
                contextMenuLayoutVLG.spacing = 4f;

                var disable = contextMenu.AddComponent<Clickable>();
                disable.onExit = pointerEventData => contextMenu.SetActive(false);

                EditorThemeManager.AddGraphic(contextMenuImage, ThemeGroup.Background_2, true);
                contextMenu.SetActive(false);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        #endregion

        /// <summary>
        /// Shows the editor context menu.
        /// </summary>
        /// <param name="buttonFunctions">The context menus' functions.</param>
        public void ShowContextMenu(List<ButtonFunction> buttonFunctions) => ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH, buttonFunctions);

        /// <summary>
        /// Shows the editor context menu.
        /// </summary>
        /// <param name="buttonFunctions">The context menus' functions.</param>
        public void ShowContextMenu(params ButtonFunction[] buttonFunctions) => ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH, buttonFunctions);

        /// <summary>
        /// Shows the editor context menu.
        /// </summary>
        /// <param name="width">Width of the context menu.</param>
        /// <param name="buttonFunctions">The context menus' functions.</param>
        public void ShowContextMenu(float width, List<ButtonFunction> buttonFunctions) => ShowContextMenu(width, buttonFunctions.ToArray());

        /// <summary>
        /// Shows the editor context menu.
        /// </summary>
        /// <param name="width">Width of the context menu.</param>
        /// <param name="buttonFunctions">The context menus' functions.</param>
        public void ShowContextMenu(float width, params ButtonFunction[] buttonFunctions)
        {
            float height = 0f;
            contextMenu.SetActive(true);
            LSHelpers.DeleteChildren(contextMenuLayout);
            for (int i = 0; i < buttonFunctions.Length; i++)
            {
                var buttonFunction = buttonFunctions[i];

                if (buttonFunction.IsSpacer)
                {
                    var g = Creator.NewUIObject("sp", contextMenuLayout);
                    var image = g.AddComponent<Image>();
                    image.rectTransform.sizeDelta = new Vector2(0f, buttonFunction.SpacerSize);
                    EditorThemeManager.ApplyGraphic(image, ThemeGroup.Background_3);
                    height += 6f;
                    continue;
                }

                var gameObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(contextMenuLayout);
                var buttonStorage = gameObject.GetComponent<FunctionButtonStorage>();

                buttonStorage.button.onClick.ClearAll();
                buttonStorage.button.onClick.AddListener(() =>
                {
                    contextMenu.SetActive(false);
                    buttonFunction.Action?.Invoke();
                });
                buttonStorage.label.alignment = TextAnchor.MiddleLeft;
                buttonStorage.label.text = buttonFunction.Name;
                buttonStorage.label.rectTransform.sizeDelta = new Vector2(-12f, 0f);

                if (!string.IsNullOrEmpty(buttonFunction.TooltipGroup))
                    TooltipHelper.AssignTooltip(gameObject, buttonFunction.TooltipGroup);

                EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(buttonStorage.label, ThemeGroup.Function_2_Text);
                height += 37f;
            }

            var pos = Input.mousePosition * CoreHelper.ScreenScaleInverse;
            pos.x = Mathf.Clamp(pos.x, float.NegativeInfinity, (Screen.width * CoreHelper.ScreenScaleInverse) - width);
            pos.y = Mathf.Clamp(pos.y, height, float.PositiveInfinity);
            contextMenu.transform.AsRT().anchoredPosition = pos;
            contextMenu.transform.AsRT().sizeDelta = new Vector2(width, height);
        }

        #endregion
    }
}
