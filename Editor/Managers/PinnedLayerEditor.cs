using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

using UnityRandom = UnityEngine.Random;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages pinned editor layers.
    /// </summary>
    public class PinnedLayerEditor : BaseManager<PinnedLayerEditor, EditorManagerSettings>
    {
        #region Values

        /// <summary>
        /// The Pinned Editor Layers content popup.
        /// </summary>
        public ContentPopup Popup { get; set; }

        /// <summary>
        /// The Pinned Editor Layer Dialog.
        /// </summary>
        public PinnedLayerEditorDialog Dialog { get; set; }

        /// <summary>
        /// The currently selected pinned editor layer.
        /// </summary>
        public PinnedEditorLayer CurrentPinnedEditorLayer { get; set; }

        /// <summary>
        /// List of copied pinned editor layers.
        /// </summary>
        public List<PinnedEditorLayer> copiedPinnedEditorLayers = new List<PinnedEditorLayer>();

        #endregion

        #region Functions

        public override void OnInit()
        {
            Popup = RTEditor.inst.GeneratePopup(
                name: EditorPopup.PINNED_EDITOR_LAYER_POPUP,
                title: "Pinned Editor Layers",
                defaultPosition: Vector2.zero,
                size: new Vector2(600f, 450f),
                refreshSearch: _val => RenderPopup(),
                placeholderText: "Search pinned editor layers...");

            Dialog = new PinnedLayerEditorDialog();
            Dialog.Init();

            EditorHelper.AddEditorDropdown("View Pinned Layers", string.Empty, EditorHelper.VIEW_DROPDOWN, EditorSprites.SearchSprite, () =>
            {
                Popup.Open();
                RenderPopup();
            });
        }

        /// <summary>
        /// Pins the currently viewed editor layer.
        /// </summary>
        public void PinCurrentEditorLayer()
        {
            var pinnedEditorLayer = new PinnedEditorLayer(EditorTimeline.inst.Layer, EditorTimeline.inst.layerType)
            {
                name = "Pinned Editor Layer",
                overrideColor = true,
                color = new Color(UnityRandom.Range(0.5f, 1f), UnityRandom.Range(0.5f, 1f), UnityRandom.Range(0.5f, 1f)),
            };
            RTEditor.inst.editorInfo.pinnedEditorLayers.Add(pinnedEditorLayer);
            CurrentPinnedEditorLayer = pinnedEditorLayer;
            Dialog.Open();
            RenderDialog();
            EditorTimeline.inst.RenderLayerInput(EditorTimeline.inst.Layer, EditorTimeline.inst.layerType);
        }

        /// <summary>
        /// Renders the Pinned Editor Layers popup.
        /// </summary>
        public void RenderPopup()
        {
            Popup.ClearContent();

            var add = EditorPrefabHolder.Instance.CreateAddButton(Popup.Content);
            add.Text = "Pin Current Editor Layer";
            add.OnClick.NewListener(() =>
            {
                PinCurrentEditorLayer();
                RenderPopup();
            });

            int num = 0;
            foreach (var pinnedEditorLayer in RTEditor.inst.editorInfo.pinnedEditorLayers)
            {
                int index = num;
                if (!RTString.SearchString(Popup.SearchTerm, pinnedEditorLayer.name))
                {
                    num++;
                    continue;
                }

                var gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(Popup.Content, pinnedEditorLayer.name);
                var button = gameObject.GetComponent<Button>();
                button.onClick.NewListener(() =>
                {
                    CurrentPinnedEditorLayer = pinnedEditorLayer;
                    if (!Dialog.IsCurrent)
                        Dialog.Open();
                    RenderDialog();
                    EditorTimeline.inst.SetLayer(pinnedEditorLayer.layer, pinnedEditorLayer.layerType);
                });

                var contextMenu = gameObject.AddComponent<ContextClickable>();
                contextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Go to Layer", () => EditorTimeline.inst.SetLayer(pinnedEditorLayer.layer, pinnedEditorLayer.layerType)),
                        new ButtonFunction("Set to Current Layer", () =>
                        {
                            pinnedEditorLayer.layer = EditorTimeline.inst.Layer;
                            pinnedEditorLayer.layerType = EditorTimeline.inst.layerType;
                            EditorTimeline.inst.RenderLayerInput(pinnedEditorLayer.layer, pinnedEditorLayer.layerType);
                            RenderDialog();
                            RenderPopup();
                        }),
                        new ButtonFunction("Edit", () =>
                        {
                            CurrentPinnedEditorLayer = pinnedEditorLayer;
                            if (!Dialog.IsCurrent)
                                Dialog.Open();
                            RenderDialog();
                        }),
                        new ButtonFunction("Delete", () =>
                        {
                            RTEditor.inst.editorInfo.pinnedEditorLayers.RemoveAt(index);
                            CurrentPinnedEditorLayer = null;
                            if (Dialog.IsCurrent)
                                Dialog.Close();
                            RenderPopup();
                        }),
                        new ButtonFunction("Clear All", () =>
                        {
                            RTEditor.inst.editorInfo.pinnedEditorLayers.Clear();
                            CurrentPinnedEditorLayer = null;
                            if (Dialog.IsCurrent)
                                Dialog.Close();
                            RenderPopup();
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Copy", () =>
                        {
                            copiedPinnedEditorLayers.Clear();
                            copiedPinnedEditorLayers.Add(pinnedEditorLayer);
                        }),
                        new ButtonFunction("Copy All", () =>
                        {
                            copiedPinnedEditorLayers.Clear();
                            copiedPinnedEditorLayers.AddRange(RTEditor.inst.editorInfo.pinnedEditorLayers);
                        }),
                        new ButtonFunction("Paste", () =>
                        {
                            RTEditor.inst.editorInfo.pinnedEditorLayers.AddRange(copiedPinnedEditorLayers);
                            RenderPopup();
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Move Up", () =>
                        {
                            if (index <= 0)
                            {
                                EditorManager.inst.DisplayNotification("Could not move the pinned editor layer up since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            RTEditor.inst.editorInfo.pinnedEditorLayers.Move(index, index - 1);
                            RenderPopup();
                        }),
                        new ButtonFunction("Move Down", () =>
                        {
                            if (index >= RTEditor.inst.editorInfo.pinnedEditorLayers.Count - 1)
                            {
                                EditorManager.inst.DisplayNotification("Could not move the pinned editor layer down since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            RTEditor.inst.editorInfo.pinnedEditorLayers.Move(index, index + 1);
                            RenderPopup();
                        }),
                        new ButtonFunction("Move to Start", () =>
                        {
                            if (index <= 0)
                            {
                                EditorManager.inst.DisplayNotification("Could not move the pinned editor layer up since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            RTEditor.inst.editorInfo.pinnedEditorLayers.Move(index, 0);
                            RenderPopup();
                        }),
                        new ButtonFunction("Move to End", () =>
                        {
                            if (index >= RTEditor.inst.editorInfo.pinnedEditorLayers.Count - 1)
                            {
                                EditorManager.inst.DisplayNotification("Could not move the pinned editor layer down since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            RTEditor.inst.editorInfo.pinnedEditorLayers.Move(index, RTEditor.inst.editorInfo.pinnedEditorLayers.Count - 1);
                            RenderPopup();
                        })
                        );
                };

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();
                text.text = $"[{EditorTimeline.GetLayerString(pinnedEditorLayer.layer)}, {pinnedEditorLayer.layerType}] - {pinnedEditorLayer.name}";

                var color = pinnedEditorLayer.overrideColor ? pinnedEditorLayer.color : EditorTimeline.GetLayerColor(pinnedEditorLayer.layer, pinnedEditorLayer.layerType);

                var image = gameObject.transform.Find("Image").GetComponent<Image>();
                image.color = color;

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform, "Delete");
                UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(280f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));
                var deleteStorage = delete.GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.NewListener(() =>
                {
                    RTEditor.inst.editorInfo.pinnedEditorLayers.RemoveAt(index);
                    CurrentPinnedEditorLayer = null;
                    if (Dialog.IsCurrent)
                        Dialog.Close();
                    RenderPopup();
                });

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(text);
                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                TooltipHelper.AddHoverTooltip(gameObject, $"<#{RTColors.ColorToHexOptional(color)}>{pinnedEditorLayer.name}</color>", pinnedEditorLayer.desc, clear: true);
                num++;
            }
        }

        /// <summary>
        /// Renders the Pinned Editor Layer dialog.
        /// </summary>
        public void RenderDialog()
        {
            if (!CurrentPinnedEditorLayer)
                return;

            Dialog.LayerField.inputField.SetTextWithoutNotify(EditorTimeline.GetLayerString(CurrentPinnedEditorLayer.layer));
            Dialog.LayerField.inputField.onValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    CurrentPinnedEditorLayer.layer = RTMath.Clamp(num - 1, 0, int.MaxValue);

                    if (Popup.IsOpen)
                        RenderPopup();

                    EditorTimeline.inst.RenderLayerInput(EditorTimeline.inst.Layer, EditorTimeline.inst.layerType);
                }
            });
            Dialog.LayerField.inputField.onEndEdit.NewListener(_val =>
            {
                if (RTMath.TryParse(_val, CurrentPinnedEditorLayer.layer, out float num))
                {
                    CurrentPinnedEditorLayer.layer = RTMath.Clamp((int)num - 1, 0, int.MaxValue);

                    if (Popup.IsOpen)
                        RenderPopup();

                    EditorTimeline.inst.RenderLayerInput(EditorTimeline.inst.Layer, EditorTimeline.inst.layerType);
                }
            });
            Dialog.LayerField.middleButton.onClick.NewListener(() =>
            {
                CurrentPinnedEditorLayer.layer = EditorTimeline.inst.Layer;
                CurrentPinnedEditorLayer.layerType = EditorTimeline.inst.layerType;
                RenderDialog();
            });
            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.LayerField, min: 1, max: int.MaxValue);
            TriggerHelper.AddEventTriggers(Dialog.LayerField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.LayerField.inputField, min: 1, max: int.MaxValue));

            Dialog.LayerTypeDropdown.SetValueWithoutNotify((int)CurrentPinnedEditorLayer.layerType);
            Dialog.LayerTypeDropdown.onValueChanged.NewListener(_val =>
            {
                CurrentPinnedEditorLayer.layerType = (EditorTimeline.LayerType)_val;

                if (Popup.IsOpen)
                    RenderPopup();
                EditorTimeline.inst.RenderLayerInput(EditorTimeline.inst.Layer, EditorTimeline.inst.layerType);
            });

            Dialog.NameField.SetTextWithoutNotify(CurrentPinnedEditorLayer.name);
            Dialog.NameField.onValueChanged.NewListener(_val =>
            {
                CurrentPinnedEditorLayer.name = _val;

                if (Popup.IsOpen)
                    RenderPopup();
            });
            Dialog.DescriptionField.SetTextWithoutNotify(CurrentPinnedEditorLayer.desc);
            Dialog.DescriptionField.onValueChanged.NewListener(_val =>
            {
                CurrentPinnedEditorLayer.desc = _val;

                if (Popup.IsOpen)
                    RenderPopup();
            });
            Dialog.ColorField.SetTextWithoutNotify(RTColors.ColorToHexOptional(CurrentPinnedEditorLayer.color));
            Dialog.ColorField.onValueChanged.NewListener(_val =>
            {
                CurrentPinnedEditorLayer.color = RTColors.HexToColor(_val);

                if (Popup.IsOpen)
                    RenderPopup();
                EditorTimeline.inst.RenderLayerInput(EditorTimeline.inst.Layer, EditorTimeline.inst.layerType);
            });
            Dialog.ColorOverrideToggle.toggle.SetIsOnWithoutNotify(CurrentPinnedEditorLayer.overrideColor);
            Dialog.ColorOverrideToggle.toggle.onValueChanged.NewListener(_val =>
            {
                CurrentPinnedEditorLayer.overrideColor = _val;

                if (Popup.IsOpen)
                    RenderPopup();
                EditorTimeline.inst.RenderLayerInput(EditorTimeline.inst.Layer, EditorTimeline.inst.layerType);
            });
            var colorContextMenu = Dialog.ColorField.gameObject.GetOrAddComponent<ContextClickable>();
            colorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                var currentHexColor = Dialog.ColorField.text;
                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Edit Color", () =>
                    {
                        RTColorPicker.inst.Show(RTColors.HexToColor(currentHexColor),
                            (col, hex) =>
                            {
                                Dialog.ColorField.SetTextWithoutNotify(hex);
                            },
                            (col, hex) =>
                            {
                                // set the input field's text empty so it notices there was a change
                                Dialog.ColorField.SetTextWithoutNotify(string.Empty);
                                Dialog.ColorField.text = hex;
                            }, () =>
                            {
                                Dialog.ColorField.SetTextWithoutNotify(currentHexColor);
                            });
                    }),
                    new ButtonFunction("Clear", () =>
                    {
                        Dialog.ColorField.text = string.Empty;
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("VG Red", () =>
                    {
                        Dialog.ColorField.text = ObjectEditorData.RED;
                    }),
                    new ButtonFunction("VG Red Green", () =>
                    {
                        Dialog.ColorField.text = ObjectEditorData.RED_GREEN;
                    }),
                    new ButtonFunction("VG Green", () =>
                    {
                        Dialog.ColorField.text = ObjectEditorData.GREEN;
                    }),
                    new ButtonFunction("VG Green Blue", () =>
                    {
                        Dialog.ColorField.text = ObjectEditorData.GREEN_BLUE;
                    }),
                    new ButtonFunction("VG Blue", () =>
                    {
                        Dialog.ColorField.text = ObjectEditorData.BLUE;
                    }),
                    new ButtonFunction("VG Blue Red", () =>
                    {
                        Dialog.ColorField.text = ObjectEditorData.RED_BLUE;
                    }));
            };
        }

        #endregion
    }
}
