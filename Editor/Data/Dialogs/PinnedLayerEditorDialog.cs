
using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PinnedLayerEditorDialog : EditorDialog
    {
        public PinnedLayerEditorDialog() : base() { }

        public RectTransform Content { get; set; }

        public InputFieldStorage LayerField { get; set; }
        
        public InputFieldStorage LayerRangeField { get; set; }

        public Dropdown LayerTypeDropdown { get; set; }

        public ToggleButtonStorage AllLayerTypesToggle { get; set; }

        public InputField NameField { get; set; }

        public InputField DescriptionField { get; set; }

        public ToggleButtonStorage ColorOverrideToggle { get; set; }

        public InputField ColorField { get; set; }

        public override void Init()
        {
            if (init)
                return;

            var editorDialog = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "PinnedLayerEditorDialog");
            var editorDialogStorage = editorDialog.GetComponent<EditorDialogStorage>();
            editorDialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            EditorHelper.AddEditorDialog(PINNED_EDITOR_LAYER_DIALOG, editorDialog);

            InitDialog(PINNED_EDITOR_LAYER_DIALOG);

            base.Init();

            #region Generation

            editorDialogStorage.topPanel.color = RTColors.HexToColor("FE4545");
            editorDialogStorage.title.text = "- Pinned Layer Editor -";
            editorDialogStorage.title.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            EditorThemeManager.ApplyGraphic(editorDialog.GetComponent<Image>(), ThemeGroup.Background_1);

            CoreHelper.Delete(GameObject.transform.Find("spacer"));
            CoreHelper.Delete(GameObject.transform.Find("Text"));

            var main = Creator.NewUIObject("Main", editorDialog.transform);
            main.transform.AsRT().sizeDelta = new Vector2(765f, 696f);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(main.transform, "Scroll View");
            RectValues.Default.SizeDelta(745f, 696f).AssignToRectTransform(scrollView.transform.AsRT());
            Content = scrollView.transform.Find("Viewport/Content").AsRT();

            #region Layer

            new LabelsElement("Layer").Init(EditorElement.InitSettings.Default.Parent(Content));

            var layer = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Content, "Layer");
            RectValues.Default.AnchoredPosition(162.5f, 16f).AnchorMax(0f, 0f).AnchorMin(0f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(layer.transform.AsRT());
            LayerField = layer.GetComponent<InputFieldStorage>();
            CoreHelper.Delete(LayerField.leftGreaterButton);
            CoreHelper.Delete(LayerField.rightGreaterButton);
            EditorThemeManager.ApplyInputField(LayerField);

            new LabelsElement("Layer Range").Init(EditorElement.InitSettings.Default.Parent(Content));

            var layerRange = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Content, "Layer Range");
            RectValues.Default.AnchoredPosition(162.5f, 16f).AnchorMax(0f, 0f).AnchorMin(0f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(layerRange.transform.AsRT());
            LayerRangeField = layerRange.GetComponent<InputFieldStorage>();
            CoreHelper.Delete(LayerRangeField.leftGreaterButton);
            CoreHelper.Delete(LayerRangeField.rightGreaterButton);
            EditorThemeManager.ApplyInputField(LayerRangeField);

            new LabelsElement("Layer Type").Init(EditorElement.InitSettings.Default.Parent(Content));

            var layerType = EditorPrefabHolder.Instance.Dropdown.Duplicate(Content, "Layer Type");
            RectValues.Default.AnchoredPosition(112.5f, 16f).AnchorMax(0f, 0f).AnchorMin(0f, 0f).SizeDelta(198f, 32f).AssignToRectTransform(layerType.transform.AsRT());
            LayerTypeDropdown = layerType.GetComponent<Dropdown>();
            LayerTypeDropdown.options = CoreHelper.ToOptionData<EditorTimeline.LayerType>();
            EditorThemeManager.ApplyDropdown(LayerTypeDropdown);

            var allLayerTypes = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Content, "layer type toggle");
            RectValues.Default.AnchoredPosition(162.5f, 16f).AnchorMax(0f, 0f).AnchorMin(0f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(allLayerTypes.transform.AsRT());
            AllLayerTypesToggle = allLayerTypes.GetComponent<ToggleButtonStorage>();
            AllLayerTypesToggle.label.text = "All Layer Types";
            EditorThemeManager.ApplyToggle(AllLayerTypesToggle);

            new SpacerElement(false).Init(EditorElement.InitSettings.Default.Parent(Content));

            #endregion

            #region Name

            new LabelsElement("Name").Init(EditorElement.InitSettings.Default.Parent(Content));

            var name = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(Content);
            name.transform.localScale = Vector3.one;
            RectValues.Default.SizeDelta(740f, 32f).AssignToRectTransform(name.transform.AsRT());

            NameField = name.GetComponent<InputField>();
            NameField.lineType = InputField.LineType.MultiLineNewline;
            NameField.GetPlaceholderText().text = "Set name...";
            NameField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
            EditorThemeManager.ApplyInputField(NameField);

            new SpacerElement(false).Init(EditorElement.InitSettings.Default.Parent(Content));

            #endregion

            #region Description

            new LabelsElement("Description").Init(EditorElement.InitSettings.Default.Parent(Content));

            var description = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(Content);
            description.transform.localScale = Vector3.one;
            RectValues.Default.SizeDelta(740f, 200f).AssignToRectTransform(description.transform.AsRT());

            DescriptionField = description.GetComponent<InputField>();
            DescriptionField.lineType = InputField.LineType.MultiLineNewline;
            DescriptionField.GetPlaceholderText().text = "Set description...";
            DescriptionField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
            EditorThemeManager.ApplyInputField(DescriptionField);

            new SpacerElement(new Vector2(0f, 32f), false).Init(EditorElement.InitSettings.Default.Parent(Content));

            #endregion

            #region Color

            new LabelsElement("Color").Init(EditorElement.InitSettings.Default.Parent(Content));

            var toggle = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Content, "color toggle");
            RectValues.Default.AnchoredPosition(162.5f, 16f).AnchorMax(0f, 0f).AnchorMin(0f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(toggle.transform.AsRT());
            ColorOverrideToggle = toggle.GetComponent<ToggleButtonStorage>();
            ColorOverrideToggle.label.text = "Override";
            EditorThemeManager.ApplyToggle(ColorOverrideToggle);

            var color = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(Content, "color");
            color.transform.localScale = Vector3.one;
            RectValues.Default.SizeDelta(740f, 32f).AssignToRectTransform(color.transform.AsRT());

            ColorField = color.GetComponent<InputField>();
            ColorField.lineType = InputField.LineType.MultiLineNewline;
            ColorField.GetPlaceholderText().text = "Set color...";
            ColorField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
            EditorThemeManager.ApplyInputField(ColorField);

            new SpacerElement(false).Init(EditorElement.InitSettings.Default.Parent(Content));

            #endregion

            #endregion
        }
    }
}
