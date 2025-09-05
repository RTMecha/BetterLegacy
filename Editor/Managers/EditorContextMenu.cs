using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;

using Version = BetterLegacy.Core.Data.Version;

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
        /// Adds the editor context menu to an object.
        /// </summary>
        /// <param name="gameObject">Unity game object to add a context menu to.</param>
        /// <param name="buttonFunctions">The context menus' functions.</param>
        public void AddContextMenu(GameObject gameObject, params ButtonFunction[] buttonFunctions) => AddContextMenu(gameObject, null, buttonFunctions);

        /// <summary>
        /// Adds the editor context menu to an object.
        /// </summary>
        /// <param name="gameObject">Unity game object to add a context menu to.</param>
        /// <param name="leftClick">Function to run when the user left clicks.</param>
        /// <param name="buttonFunctions">The context menus' functions.</param>
        public void AddContextMenu(GameObject gameObject, Action leftClick, params ButtonFunction[] buttonFunctions)
        {
            if (!gameObject)
                return;

            gameObject.GetOrAddComponent<ContextClickable>().onClick = pointerEventData =>
            {
                switch (pointerEventData.button)
                {
                    case PointerEventData.InputButton.Left: {
                            leftClick?.Invoke();
                            break;
                        }
                    case PointerEventData.InputButton.Right: {
                            ShowContextMenu(buttonFunctions);
                            break;
                        }
                }
            };
        }

        /// <summary>
        /// Adds the editor context menu to an object.
        /// </summary>
        /// <param name="gameObject">Unity game object to add a context menu to.</param>
        /// <param name="buttonFunctions">The context menus' functions.</param>
        public void AddContextMenu(GameObject gameObject, List<ButtonFunction> buttonFunctions) => AddContextMenu(gameObject, null, buttonFunctions);

        /// <summary>
        /// Adds the editor context menu to an object.
        /// </summary>
        /// <param name="gameObject">Unity game object to add a context menu to.</param>
        /// <param name="leftClick">Function to run when the user left clicks.</param>
        /// <param name="buttonFunctions">The context menus' functions.</param>
        public void AddContextMenu(GameObject gameObject, Action leftClick, List<ButtonFunction> buttonFunctions)
        {
            if (!gameObject)
                return;

            gameObject.GetOrAddComponent<ContextClickable>().onClick = pointerEventData =>
            {
                switch (pointerEventData.button)
                {
                    case PointerEventData.InputButton.Left: {
                            leftClick?.Invoke();
                            break;
                        }
                    case PointerEventData.InputButton.Right: {
                            ShowContextMenu(buttonFunctions);
                            break;
                        }
                }
            };
        }

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

        public static List<ButtonFunction> GetMoveIndexFunctions<T>(List<T> list, int index, Action onMove = null) => new List<ButtonFunction>
        {
            new ButtonFunction("Move Up", () =>
            {
                if (index <= 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move item up since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                list.Move(index, index - 1);
                onMove?.Invoke();
            }),
            new ButtonFunction("Move Down", () =>
            {
                if (index >= list.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move item down since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                list.Move(index, index + 1);
                onMove?.Invoke();
            }),
            new ButtonFunction("Move to Start", () =>
            {
                if (index == 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move item to the start since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                list.Move(index, 0);
                onMove?.Invoke();
            }),
            new ButtonFunction("Move to End", () =>
            {
                if (index == list.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move item to the end since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                list.Move(index, list.Count - 1);
                onMove?.Invoke();
            }),
        };

        public static List<ButtonFunction> GetObjectVersionFunctions(IUploadable uploadable, Action update)
        {
            var buttonFunctions = new List<ButtonFunction>
            {
                new ButtonFunction("Format 1.0.0", () =>
                {
                    uploadable.ObjectVersion = "1.0.0";
                    update?.Invoke();
                }),
                new ButtonFunction("Format 1.0", () =>
                {
                    uploadable.ObjectVersion = "1.0";
                    update?.Invoke();
                }),
                new ButtonFunction("Format 1", () =>
                {
                    uploadable.ObjectVersion = "1";
                    update?.Invoke();
                }),
            };

            var origVersion = uploadable.ObjectVersion;
            if (string.IsNullOrEmpty(origVersion))
                return buttonFunctions;

            buttonFunctions.Add(new ButtonFunction(true));

            if (Version.TryParse(origVersion, out Version version))
            {
                buttonFunctions.AddRange(new List<ButtonFunction>
                {
                    new ButtonFunction("Increase Patch Number", () =>
                    {
                        version.Patch++;
                        uploadable.ObjectVersion = version.ToString();
                        update?.Invoke();
                    }),
                    new ButtonFunction("Increase Minor Number", () =>
                    {
                        version.Patch = 0;
                        version.Minor++;
                        uploadable.ObjectVersion = version.ToString();
                        update?.Invoke();
                    }),
                    new ButtonFunction("Increase Major Number", () =>
                    {
                        version.Patch = 0;
                        version.Minor = 0;
                        version.Major++;
                        uploadable.ObjectVersion = version.ToString();
                        update?.Invoke();
                    }),
                });
            }
            else if (RTString.RegexMatch(origVersion, new Regex(@"([0-9]+).([0-9]+)"), out Match match))
            {
                var major = int.Parse(match.Groups[1].ToString());
                var minor = int.Parse(match.Groups[2].ToString());

                buttonFunctions.AddRange(new List<ButtonFunction>
                {
                    new ButtonFunction("Increase Minor Number", () =>
                    {
                        minor++;
                        uploadable.ObjectVersion = $"{major}.{minor}";
                        update?.Invoke();
                    }),
                    new ButtonFunction("Increase Minor Number", () =>
                    {
                        major++;
                        uploadable.ObjectVersion = $"{major}.{minor}";
                        update?.Invoke();
                    })
                });
            }
            else if (int.TryParse(origVersion, out int num))
            {
                buttonFunctions.AddRange(new List<ButtonFunction>
                {
                    new ButtonFunction("Increase Number", () =>
                    {
                        num++;
                        uploadable.ObjectVersion = num.ToString();
                        update?.Invoke();
                    }),
                });
            }

            return buttonFunctions;
        }

        #endregion
    }
}
