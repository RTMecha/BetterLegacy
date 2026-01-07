using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

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
            Popup.onRender = () =>
            {
                if (AssetPack.TryReadFromFile("editor/ui/popups/pinned_editor_layers_popup.json", out string uiFile))
                {
                    var jn = JSON.Parse(uiFile);
                    RectValues.TryParse(jn["base"]["rect"], RectValues.Default.SizeDelta(600f, 450f)).AssignToRectTransform(Popup.GameObject.transform.AsRT());
                    RectValues.TryParse(jn["top_panel"]["rect"], RectValues.FullAnchored.AnchorMin(0, 1).Pivot(0f, 0f).SizeDelta(32f, 32f)).AssignToRectTransform(Popup.TopPanel);
                    RectValues.TryParse(jn["search"]["rect"], new RectValues(Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 32f))).AssignToRectTransform(Popup.GameObject.transform.Find("search-box").AsRT());
                    RectValues.TryParse(jn["scrollbar"]["rect"], new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(0f, 0.5f), new Vector2(32f, 0f))).AssignToRectTransform(Popup.GameObject.transform.Find("Scrollbar").AsRT());

                    var layoutValues = LayoutValues.Parse(jn["layout"]);
                    if (layoutValues is GridLayoutValues gridLayoutValues)
                        gridLayoutValues.AssignToLayout(Popup.Grid ? Popup.Grid : Popup.GameObject.transform.Find("mask/content").GetComponent<GridLayoutGroup>());

                    if (jn["title"] != null)
                    {
                        Popup.title = jn["title"]["text"] != null ? jn["title"]["text"] : "Pinned Editor Layers";

                        var title = Popup.Title;
                        RectValues.TryParse(jn["title"]["rect"], RectValues.FullAnchored.AnchoredPosition(2f, 0f).SizeDelta(-12f, -8f)).AssignToRectTransform(title.rectTransform);
                        title.alignment = jn["title"]["alignment"] != null ? (TextAnchor)jn["title"]["alignment"].AsInt : TextAnchor.MiddleLeft;
                        title.fontSize = jn["title"]["font_size"] != null ? jn["title"]["font_size"].AsInt : 20;
                        title.fontStyle = (FontStyle)jn["title"]["font_style"].AsInt;
                        title.horizontalOverflow = jn["title"]["horizontal_overflow"] != null ? (HorizontalWrapMode)jn["title"]["horizontal_overflow"].AsInt : HorizontalWrapMode.Wrap;
                        title.verticalOverflow = jn["title"]["vertical_overflow"] != null ? (VerticalWrapMode)jn["title"]["vertical_overflow"].AsInt : VerticalWrapMode.Overflow;
                    }

                    if (jn["anim"] != null)
                        Popup.ReadAnimationJSON(jn["anim"]);

                    if (jn["drag_mode"] != null && Popup.Dragger)
                        Popup.Dragger.mode = (DraggableUI.DragMode)jn["drag_mode"].AsInt;
                }
            };

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

                var buttonFunctions = new List<EditorElement>
                {
                    new ButtonElement("Go to Layer", () => EditorTimeline.inst.SetLayer(pinnedEditorLayer.layer, pinnedEditorLayer.layerType)),
                    new ButtonElement("Set to Current Layer", () =>
                    {
                        pinnedEditorLayer.layer = EditorTimeline.inst.Layer;
                        pinnedEditorLayer.layerType = EditorTimeline.inst.layerType;
                        EditorTimeline.inst.RenderLayerInput(pinnedEditorLayer.layer, pinnedEditorLayer.layerType);
                        RenderDialog();
                        RenderPopup();
                    }),
                    new ButtonElement("Edit", () =>
                    {
                        CurrentPinnedEditorLayer = pinnedEditorLayer;
                        if (!Dialog.IsCurrent)
                            Dialog.Open();
                        RenderDialog();
                    }),
                    new ButtonElement("Delete", () =>
                    {
                        RTEditor.inst.editorInfo.pinnedEditorLayers.RemoveAt(index);
                        CurrentPinnedEditorLayer = null;
                        if (Dialog.IsCurrent)
                            Dialog.Close();
                        RenderPopup();
                    }),
                    new ButtonElement("Clear All", () =>
                    {
                        RTEditor.inst.editorInfo.pinnedEditorLayers.Clear();
                        CurrentPinnedEditorLayer = null;
                        if (Dialog.IsCurrent)
                            Dialog.Close();
                        RenderPopup();
                    }),
                    new SpacerElement(),
                    new ButtonElement("Copy", () =>
                    {
                        copiedPinnedEditorLayers.Clear();
                        copiedPinnedEditorLayers.Add(pinnedEditorLayer);
                    }),
                    new ButtonElement("Copy All", () =>
                    {
                        copiedPinnedEditorLayers.Clear();
                        copiedPinnedEditorLayers.AddRange(RTEditor.inst.editorInfo.pinnedEditorLayers);
                    }),
                    new ButtonElement("Paste", () =>
                    {
                        RTEditor.inst.editorInfo.pinnedEditorLayers.AddRange(copiedPinnedEditorLayers);
                        RenderPopup();
                    }),
                    new SpacerElement(),
                };
                buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(RTEditor.inst.editorInfo.pinnedEditorLayers, index, RenderPopup));
                EditorContextMenu.AddContextMenu(gameObject, buttonFunctions);

                var text = gameObject.transform.GetChild(0).GetComponent<Text>();
                text.text = $"[{EditorTimeline.GetLayerString(pinnedEditorLayer.layer)}, {pinnedEditorLayer.layerType}] - {pinnedEditorLayer.name}";

                var color = pinnedEditorLayer.overrideColor ? pinnedEditorLayer.color : EditorTimeline.GetLayerColor(pinnedEditorLayer.layer, pinnedEditorLayer.layerType);

                var image = gameObject.transform.Find("Image").GetComponent<Image>();
                image.color = color;

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform, "Delete");
                UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(280f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));
                var deleteStorage = delete.GetComponent<DeleteButtonStorage>();
                deleteStorage.OnClick.NewListener(() =>
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
                EditorThemeManager.ApplyDeleteButton(deleteStorage);

                TooltipHelper.AddHoverTooltip(gameObject, $"<#{RTColors.ColorToHexOptional(color)}>{pinnedEditorLayer.name}</color>", pinnedEditorLayer.description, clear: true);
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
            Dialog.DescriptionField.SetTextWithoutNotify(CurrentPinnedEditorLayer.description);
            Dialog.DescriptionField.onValueChanged.NewListener(_val =>
            {
                CurrentPinnedEditorLayer.description = _val;

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
                EditorContextMenu.inst.ShowContextMenu(EditorContextMenu.GetEditorColorFunctions(Dialog.ColorField, () => currentHexColor));
            };
        }

        #endregion
    }
}
