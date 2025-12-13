using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;
using Crosstales.FB;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Elements;

using Version = BetterLegacy.Core.Data.Version;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Provides some extra editor functionality via right clicking an element in the editor.
    /// </summary>
    public class EditorContextMenu : BaseManager<EditorContextMenu, EditorManagerSettings>
    {
        #region Values

        /// <summary>
        /// The default width the context menu should have.
        /// </summary>
        public const float DEFAULT_CONTEXT_MENU_WIDTH = 300f;

        Transform parent;
        GameObject contextMenu;
        RectTransform contextMenuLayout;

        #endregion

        #region Functions

        public override void OnInit()
        {
            try
            {
                parent = EditorManager.inst.dialogs.parent.parent;

                contextMenu = Creator.NewUIObject("Context Menu", parent, parent.childCount - 1);
                RectValues.Default.AnchorMax(0f, 0f).AnchorMin(0f, 0f).Pivot(0f, 1f).SizeDelta(126f, 300f).AssignToRectTransform(contextMenu.transform.AsRT());
                var contextMenuImage = contextMenu.AddComponent<Image>();

                var contextMenuLayout = Creator.NewUIObject("Context Menu Layout", contextMenu.transform);
                RectValues.Default.AnchorMax(1f, 0.5f).AnchorMin(0f, 0.5f).SizeDelta(0f, 0f).AssignToRectTransform(contextMenuLayout.transform.AsRT());
                this.contextMenuLayout = contextMenuLayout.transform.AsRT();

                var contextMenuLayoutVLG = contextMenuLayout.AddComponent<VerticalLayoutGroup>();
                contextMenuLayoutVLG.childControlHeight = false;
                contextMenuLayoutVLG.childForceExpandHeight = false;
                contextMenuLayoutVLG.spacing = 4f;
                contextMenuLayoutVLG.padding = new RectOffset(8, 8, 8, 8);

                var contentSizeFitter = contextMenuLayout.AddComponent<ContentSizeFitter>();
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

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

        /// <summary>
        /// Adds the editor context menu to an object.
        /// </summary>
        /// <param name="gameObject">Unity game object to add a context menu to.</param>
        /// <param name="editorElements">The context menus' functions.</param>
        public static void AddContextMenu(GameObject gameObject, params EditorElement[] editorElements) => AddContextMenu(gameObject, null, editorElements);

        /// <summary>
        /// Adds the editor context menu to an object.
        /// </summary>
        /// <param name="gameObject">Unity game object to add a context menu to.</param>
        /// <param name="leftClick">Function to run when the user left clicks.</param>
        /// <param name="editorElements">The context menus' functions.</param>
        public static void AddContextMenu(GameObject gameObject, Action leftClick, params EditorElement[] editorElements)
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
                            inst.ShowContextMenu(editorElements);
                            break;
                        }
                }
            };
        }

        /// <summary>
        /// Adds the editor context menu to an object.
        /// </summary>
        /// <param name="gameObject">Unity game object to add a context menu to.</param>
        /// <param name="editorElements">The context menus' functions.</param>
        public static void AddContextMenu(GameObject gameObject, List<EditorElement> editorElements) => AddContextMenu(gameObject, null, editorElements);

        /// <summary>
        /// Adds the editor context menu to an object.
        /// </summary>
        /// <param name="gameObject">Unity game object to add a context menu to.</param>
        /// <param name="leftClick">Function to run when the user left clicks.</param>
        /// <param name="editorElements">The context menus' functions.</param>
        public static void AddContextMenu(GameObject gameObject, Action leftClick, List<EditorElement> editorElements)
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
                            inst?.ShowContextMenu(editorElements);
                            break;
                        }
                }
            };
        }

        /// <summary>
        /// Adds the editor context menu to an object.
        /// </summary>
        /// <param name="gameObject">Unity game object to add a context menu to.</param>
        /// <param name="getEditorElements">The context menus' functions.</param>
        public static void AddContextMenu(GameObject gameObject, Func<List<EditorElement>> getEditorElements) => AddContextMenu(gameObject, null, getEditorElements);

        /// <summary>
        /// Adds the editor context menu to an object.
        /// </summary>
        /// <param name="gameObject">Unity game object to add a context menu to.</param>
        /// <param name="leftClick">Function to run when the user left clicks.</param>
        /// <param name="getEditorElements">The context menus' functions.</param>
        public static void AddContextMenu(GameObject gameObject, Action leftClick, Func<List<EditorElement>> getEditorElements)
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
                            inst?.ShowContextMenu(getEditorElements?.Invoke() ?? new List<EditorElement>());
                            break;
                        }
                }
            };
        }

        /// <summary>
        /// Shows the editor context menu.
        /// </summary>
        /// <param name="editorElementGroup">The context menus' functions.</param>
        public void ShowContextMenu(EditorElementGroup editorElementGroup) => ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH, editorElementGroup);

        public void ShowContextMenu(List<EditorElementGroup> editorElementGroups) => ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH, editorElementGroups);

        public void ShowContextMenu(params EditorElementGroup[] editorElementGroups) => ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH, editorElementGroups);

        public void ShowContextMenu(float width, EditorElementGroup editorElementGroup) => ShowContextMenu(width, editorElementGroup.Elements);

        public void ShowContextMenu(float width, List<EditorElementGroup> editorElementGroups) => ShowContextMenu(width, editorElementGroups.ToArray());

        public void ShowContextMenu(float width, params EditorElementGroup[] editorElementGroups)
        {
            var list = new List<EditorElement>();
            for (int i = 0; i < editorElementGroups.Length; i++)
            {
                var editorElementGroup = editorElementGroups[i];
                if (editorElementGroup.ShouldGenerate)
                    list.AddRange(editorElementGroup.Elements);
            }
            if (!list.IsEmpty())
                ShowContextMenu(width, list);
        }

        /// <summary>
        /// Shows the editor context menu.
        /// </summary>
        /// <param name="buttonFunctions">The context menus' functions.</param>
        public void ShowContextMenu(List<EditorElement> editorElements) => ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH, editorElements.ToArray());

        /// <summary>
        /// Shows the editor context menu.
        /// </summary>
        /// <param name="buttonFunctions">The context menus' functions.</param>
        public void ShowContextMenu(params EditorElement[] editorElements) => ShowContextMenu(DEFAULT_CONTEXT_MENU_WIDTH, editorElements);

        /// <summary>
        /// Shows the editor context menu.
        /// </summary>
        /// <param name="width">Width of the context menu.</param>
        /// <param name="editorElements">The context menus' functions.</param>
        public void ShowContextMenu(float width, List<EditorElement> editorElements) => ShowContextMenu(width, editorElements.ToArray());

        /// <summary>
        /// Shows the editor context menu.
        /// </summary>
        /// <param name="width">Width of the context menu.</param>
        /// <param name="editorElements">The context menus' functions.</param>
        public void ShowContextMenu(float width, params EditorElement[] editorElements)
        {
            contextMenu.SetActive(true);
            CoreHelper.DestroyChildren(contextMenuLayout);
            for (int i = 0; i < editorElements.Length; i++)
            {
                var element = editorElements[i];
                if (!element.ShouldGenerate)
                    continue;

                element.Init(EditorElement.InitSettings.Default.Parent(contextMenuLayout).OnClick(pointerEventData => contextMenu.SetActive(false)));
            }

            // rebuild layout so it's correct
            LayoutRebuilder.ForceRebuildLayoutImmediate(contextMenuLayout.transform.AsRT());
            LayoutRebuilder.ForceRebuildLayoutImmediate(contextMenu.transform.AsRT());

            var pos = Input.mousePosition * CoreHelper.ScreenScaleInverse;
            pos.x = Mathf.Clamp(pos.x, float.NegativeInfinity, (Screen.width * CoreHelper.ScreenScaleInverse) - width);
            pos.y = Mathf.Clamp(pos.y, contextMenuLayout.transform.AsRT().sizeDelta.y, float.PositiveInfinity);
            contextMenu.transform.AsRT().anchoredPosition = pos;
            contextMenu.transform.AsRT().sizeDelta = new Vector2(width, contextMenuLayout.transform.AsRT().sizeDelta.y);
        }

        public static List<EditorElement> GetMoveIndexFunctions<T>(List<T> list, int index, Action onMove = null) => new List<EditorElement>
        {
            new ButtonElement("Move Up", () =>
            {
                if (index <= 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move item up since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                list.Move(index, index - 1);
                onMove?.Invoke();
            }),
            new ButtonElement("Move Down", () =>
            {
                if (index >= list.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move item down since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                list.Move(index, index + 1);
                onMove?.Invoke();
            }),
            new ButtonElement("Move to Start", () =>
            {
                if (index == 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move item to the start since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                list.Move(index, 0);
                onMove?.Invoke();
            }),
            new ButtonElement("Move to End", () =>
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
        
        public static List<EditorElement> GetMoveIndexFunctions<T>(List<T> list, Func<int> getIndex, Action onMove = null) => new List<EditorElement>
        {
            new ButtonElement("Move Up", () =>
            {
                var index = getIndex();
                if (index == -1)
                {
                    EditorManager.inst.DisplayNotification("Item does not exist in the list.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                if (index <= 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move item up since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                list.Move(index, index - 1);
                onMove?.Invoke();
            }),
            new ButtonElement("Move Down", () =>
            {
                var index = getIndex();
                if (index == -1)
                {
                    EditorManager.inst.DisplayNotification("Item does not exist in the list.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                if (index >= list.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move item down since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                list.Move(index, index + 1);
                onMove?.Invoke();
            }),
            new ButtonElement("Move to Start", () =>
            {
                var index = getIndex();
                if (index == -1)
                {
                    EditorManager.inst.DisplayNotification("Item does not exist in the list.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                if (index == 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move item to the start since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                list.Move(index, 0);
                onMove?.Invoke();
            }),
            new ButtonElement("Move to End", () =>
            {
                var index = getIndex();
                if (index == -1)
                {
                    EditorManager.inst.DisplayNotification("Item does not exist in the list.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                if (index == list.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move item to the end since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                list.Move(index, list.Count - 1);
                onMove?.Invoke();
            }),
        };

        public static List<EditorElement> GetIndexerFunctions<T>(int currentIndex, List<T> list) where T : PAObjectBase, IEditable => new List<EditorElement>
        {
            new ButtonElement("Select Previous", () =>
            {
                if (currentIndex <= 0)
                {
                    EditorManager.inst.DisplayNotification($"There are no previous objects to select.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                var prevObject = list[currentIndex - 1];

                if (!prevObject)
                    return;

                var timelineObject = EditorTimeline.inst.GetTimelineObject(prevObject);

                if (timelineObject)
                    EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
            }),
            new ButtonElement("Select Previous", () =>
            {
                if (currentIndex >= list.Count - 1)
                {
                    EditorManager.inst.DisplayNotification($"There are no previous objects to select.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                var nextObject = list[currentIndex + 1];

                if (!nextObject)
                    return;

                var timelineObject = EditorTimeline.inst.GetTimelineObject(nextObject);

                if (timelineObject)
                    EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
            }),
            new SpacerElement(),
            new ButtonElement("Select First", () =>
            {
                if (list.IsEmpty())
                {
                    EditorManager.inst.DisplayNotification($"There are no objects!", 3f, EditorManager.NotificationType.Warning);
                    return;
                }

                var prevObject = list.First();

                if (!prevObject)
                    return;

                var timelineObject = EditorTimeline.inst.GetTimelineObject(prevObject);

                if (timelineObject)
                    EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
            }),
            new ButtonElement("Select Last", () =>
            {
                if (list.IsEmpty())
                {
                    EditorManager.inst.DisplayNotification($"There are no objects!", 3f, EditorManager.NotificationType.Warning);
                    return;
                }

                var nextObject = list.Last();

                if (!nextObject)
                    return;

                var timelineObject = EditorTimeline.inst.GetTimelineObject(nextObject);

                if (timelineObject)
                    EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
            })
        };

        public static List<EditorElement> GetObjectVersionFunctions(IUploadable uploadable, Action update)
        {
            var buttonFunctions = new List<EditorElement>
            {
                new ButtonElement("Format 1.0.0", () =>
                {
                    uploadable.ObjectVersion = "1.0.0";
                    update?.Invoke();
                }),
                new ButtonElement("Format 1.0", () =>
                {
                    uploadable.ObjectVersion = "1.0";
                    update?.Invoke();
                }),
                new ButtonElement("Format 1", () =>
                {
                    uploadable.ObjectVersion = "1";
                    update?.Invoke();
                }),
            };

            var origVersion = uploadable.ObjectVersion;
            if (string.IsNullOrEmpty(origVersion))
                return buttonFunctions;

            buttonFunctions.Add(new SpacerElement());

            if (Version.TryParse(origVersion, out Version version))
            {
                buttonFunctions.AddRange(new List<EditorElement>
                {
                    new ButtonElement("Increase Patch Number", () =>
                    {
                        version.Patch++;
                        uploadable.ObjectVersion = version.ToString();
                        update?.Invoke();
                    }),
                    new ButtonElement("Increase Minor Number", () =>
                    {
                        version.Patch = 0;
                        version.Minor++;
                        uploadable.ObjectVersion = version.ToString();
                        update?.Invoke();
                    }),
                    new ButtonElement("Increase Major Number", () =>
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

                buttonFunctions.AddRange(new List<EditorElement>
                {
                    new ButtonElement("Increase Minor Number", () =>
                    {
                        minor++;
                        uploadable.ObjectVersion = $"{major}.{minor}";
                        update?.Invoke();
                    }),
                    new ButtonElement("Increase Minor Number", () =>
                    {
                        major++;
                        uploadable.ObjectVersion = $"{major}.{minor}";
                        update?.Invoke();
                    })
                });
            }
            else if (int.TryParse(origVersion, out int num))
            {
                buttonFunctions.AddRange(new List<EditorElement>
                {
                    new ButtonElement("Increase Number", () =>
                    {
                        num++;
                        uploadable.ObjectVersion = num.ToString();
                        update?.Invoke();
                    }),
                });
            }

            return buttonFunctions;
        }

        public static List<EditorElement> GetEditorColorFunctions(InputField inputField, Func<string> getValue) => new List<EditorElement>
        {
            new ButtonElement("Edit Color", () => RTColorPicker.inst.Show(RTColors.HexToColor(getValue?.Invoke() ?? string.Empty),
                (col, hex) => inputField.SetTextWithoutNotify(hex),
                (col, hex) =>
                {
                    CoreHelper.Log($"Set timeline object color: {hex}");
                    // set the input field's text empty so it notices there was a change
                    inputField.SetTextWithoutNotify(string.Empty);
                    inputField.text = hex;
                },
                () => inputField.SetTextWithoutNotify(getValue?.Invoke() ?? string.Empty))),
            new ButtonElement("Clear", () => inputField.text = string.Empty),
            new SpacerElement(),
            new ButtonElement("VG Red", () => inputField.text = ObjectEditorData.RED),
            new ButtonElement("VG Red Green", () => inputField.text = ObjectEditorData.RED_GREEN),
            new ButtonElement("VG Green", () => inputField.text = ObjectEditorData.GREEN),
            new ButtonElement("VG Green Blue", () => inputField.text = ObjectEditorData.GREEN_BLUE),
            new ButtonElement("VG Blue", () => inputField.text = ObjectEditorData.BLUE),
            new ButtonElement("VG Blue Red", () => inputField.text = ObjectEditorData.RED_BLUE)
        };

        public static List<EditorElement> GetModifierSoundPathFunctions(Func<bool> getGlobal, Action<string> setPath) => new List<EditorElement>
        {
            new ButtonElement($"Use {RTEditor.SYSTEM_BROWSER}", () =>
            {
                var isGlobal = getGlobal?.Invoke() ?? false;
                var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH) ?
                                RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.RemoveEndSlash(RTFile.BasePath);

                if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH))
                {
                    EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                    return;
                }

                var result = Crosstales.FB.FileBrowser.OpenSingleFile("Select a sound to use!", directory, FileFormat.OGG.ToName(), FileFormat.WAV.ToName(), FileFormat.MP3.ToName());
                if (string.IsNullOrEmpty(result))
                    return;

                result = RTFile.ReplaceSlash(result);
                if (result.Contains(isGlobal ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath))))
                {
                    setPath?.Invoke(result.Remove(isGlobal ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath))));
                    RTEditor.inst.BrowserPopup.Close();
                    return;
                }

                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
            }),
            new ButtonElement($"Use {RTEditor.EDITOR_BROWSER}", () =>
            {
                RTEditor.inst.BrowserPopup.Open();

                var isGlobal = getGlobal?.Invoke() ?? false;
                var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH) ?
                                RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.RemoveEndSlash(RTFile.BasePath);

                if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH))
                {
                    EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                    return;
                }

                RTFileBrowser.inst.UpdateBrowserFile(directory, RTFile.AudioDotFormats, onSelectFile: _val =>
                {
                    _val = RTFile.ReplaceSlash(_val);
                    if (_val.Contains(isGlobal ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath))))
                    {
                        setPath?.Invoke(_val.Remove(isGlobal ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath))));
                        RTEditor.inst.BrowserPopup.Close();
                        return;
                    }

                    EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                });
            }),
            new SpacerElement(),
            new ButtonElement("Select Sound Asset", () => AssetEditor.inst.OpenPopup(setPath, null, true, false))
        };

        public static List<EditorElement> GetFolderPanelFunctions<T>(EditorPanel<T> editorPanel, Action onSetIcon, Action onOpenFolder, Action onFolderUpdate, Action paste) => new List<EditorElement>
        {
            new ButtonElement("Open folder", onOpenFolder),
            new ButtonElement("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.GetDirectory(editorPanel.Path), () => { onFolderUpdate?.Invoke(); RTEditor.inst.HideNameEditor(); })),
            new SpacerElement(),
            new ButtonElement("Paste", paste),
            new ButtonElement("Delete", () => RTEditor.inst.ShowWarningPopup("Are you <b>100%</b> sure you want to delete this folder? This <b>CANNOT</b> be undone! Always make sure you have backups.", () =>
            {
                RTFile.DeleteDirectory(editorPanel.Path);
                onFolderUpdate?.Invoke();
                EditorManager.inst.DisplayNotification("Deleted folder!", 2f, EditorManager.NotificationType.Success);
                RTEditor.inst.HideWarningPopup();
            }, RTEditor.inst.HideWarningPopup)),
            new SpacerElement(),
            new ButtonElement($"Select Icon ({RTEditor.SYSTEM_BROWSER})", () =>
            {
                string imageFile = FileBrowser.OpenSingleFile("Select an image!", RTEditor.inst.BasePath, new string[] { "png" });
                if (string.IsNullOrEmpty(imageFile))
                    return;

                RTFile.CopyFile(imageFile, RTFile.CombinePaths(editorPanel.Path, $"folder_icon{FileFormat.PNG.Dot()}"));
                onSetIcon?.Invoke();
            }),
            new ButtonElement($"Select Icon ({RTEditor.EDITOR_BROWSER})", () =>
            {
                RTEditor.inst.BrowserPopup.Open();
                RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot() }, imageFile =>
                {
                    if (string.IsNullOrEmpty(imageFile))
                        return;

                    RTEditor.inst.BrowserPopup.Close();

                    RTFile.CopyFile(imageFile, RTFile.CombinePaths(editorPanel.Path, $"folder_icon{FileFormat.PNG.Dot()}"));
                    onSetIcon?.Invoke();
                });
            }),
            new ButtonElement("Clear Icon", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to clear the folder icon? This will delete the icon file.", () =>
            {
                RTEditor.inst.HideWarningPopup();
                RTFile.DeleteFile(RTFile.CombinePaths(editorPanel.Path, $"folder_icon{FileFormat.PNG.Dot()}"));
                onSetIcon?.Invoke();
                EditorManager.inst.DisplayNotification("Deleted icon!", 1.5f, EditorManager.NotificationType.Success);
            }, RTEditor.inst.HideWarningPopup)),
            new SpacerElement(),
            new ButtonElement("Create Info File", () =>
            {
                var filePath = RTFile.CombinePaths(editorPanel.Path, $"folder_info{FileFormat.JSON.Dot()}");
                if (RTFile.FileExists(filePath))
                {
                    EditorManager.inst.DisplayNotification($"Info file already exists!", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                RTTextEditor.inst.SetEditor("This is the default description.", val => { }, "Create", () =>
                {
                    var jn = Parser.NewJSONObject();
                    jn["desc"] = RTTextEditor.inst.Text;
                    editorPanel.infoJN = jn;
                    RTFile.WriteToFile(filePath, jn.ToString());
                    editorPanel.RenderTooltip();
                    RTTextEditor.inst.Popup.Close();

                    EditorManager.inst.DisplayNotification("Created info file!", 1.5f, EditorManager.NotificationType.Success);
                });
            }),
            new ButtonElement("Edit Info File", () =>
            {
                var filePath = RTFile.CombinePaths(editorPanel.Path, $"folder_info{FileFormat.JSON.Dot()}");

                if (!RTFile.FileExists(filePath))
                    return;

                RTTextEditor.inst.SetEditor("This is the default description.", val => { }, "Done", () =>
                {
                    var jn =  Parser.NewJSONObject();
                    jn["desc"] = RTTextEditor.inst.Text;
                    editorPanel.infoJN = jn;
                    RTFile.WriteToFile(filePath, jn.ToString());
                    editorPanel.RenderTooltip();
                    RTTextEditor.inst.Popup.Close();
                });
            }),
            new ButtonElement("Update Info", () =>
            {
                editorPanel.infoJN = null;
                editorPanel.RenderTooltip();
                onSetIcon?.Invoke();
            }),
            new ButtonElement("Clear Info File", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete the info file?", () =>
            {
                RTFile.DeleteFile(RTFile.CombinePaths(editorPanel.Path, $"folder_info{FileFormat.JSON.Dot()}"));
                editorPanel.infoJN = null;
                editorPanel.RenderTooltip();
                RTEditor.inst.HideWarningPopup();
                EditorManager.inst.DisplayNotification("Deleted info file!", 1.5f, EditorManager.NotificationType.Success);
            }, RTEditor.inst.HideWarningPopup))
        };

        public static List<EditorElement> GetObjectTimeFunctions(Func<float> getObjectTime, Action<float> setTime) => new List<EditorElement>
        {
            new ButtonElement("Set to Timeline Cursor", () => setTime?.Invoke(AudioManager.inst.CurrentAudioSource.time)),
            new ButtonElement("Snap to BPM", () => setTime?.Invoke(RTEditor.SnapToBPM(getObjectTime?.Invoke() ?? AudioManager.inst.CurrentAudioSource.time)))
        };

        public void Test(bool singleTest, bool groupTest)
        {
            // editor element groups can be used to generate a specific group of elements.
            ShowContextMenu(
                new EditorElementGroup(null,
                    new ButtonElement("Test", () => { }),
                    new ButtonElement("Test Generate", () => { }, shouldGenerate: () => singleTest)),
                new EditorElementGroup(() => groupTest,
                    new SpacerElement(),
                    new ButtonElement("Test Group", () => { })));
        }

        #endregion
    }
}
