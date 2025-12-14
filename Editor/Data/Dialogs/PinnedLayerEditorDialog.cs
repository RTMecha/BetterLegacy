using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PinnedLayerEditorDialog : EditorDialog
    {
        public PinnedLayerEditorDialog() : base() { }

        public InputFieldStorage LayerField { get; set; }

        public Dropdown LayerTypeDropdown { get; set; }

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

            var editorDialogSpacer = editorDialog.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            editorDialog.transform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 24f);

            var labelTypeBase = Creator.NewUIObject("Type Label", editorDialog.transform);

            labelTypeBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var labelType = editorDialog.transform.GetChild(2);
            labelType.SetParent(null);
            labelType.localPosition = Vector3.zero;
            labelType.localScale = Vector3.one;
            labelType.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelTypeText = labelType.GetComponent<Text>();
            labelTypeText.text = "Name";
            labelTypeText.alignment = TextAnchor.UpperLeft;

            #region Layer

            var labelLayerBase = Creator.NewUIObject("Layer Label", editorDialog.transform);
            labelLayerBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var labelLayer = labelType.gameObject.Duplicate(labelLayerBase.transform);
            labelLayer.transform.localPosition = Vector3.zero;
            labelLayer.transform.localScale = Vector3.one;
            labelLayer.transform.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelLayerText = labelLayer.GetComponent<Text>();
            labelLayerText.text = "Layer";

            var layerBase1 = Creator.NewUIObject("Layer", editorDialog.transform);
            layerBase1.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var layer = EditorPrefabHolder.Instance.NumberInputField.Duplicate(layerBase1.transform);
            RectValues.Default.AnchoredPosition(162.5f, 16f).AnchorMax(0f, 0f).AnchorMin(0f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(layer.transform.AsRT());
            LayerField = layer.GetComponent<InputFieldStorage>();
            CoreHelper.Delete(LayerField.leftGreaterButton);
            CoreHelper.Delete(LayerField.rightGreaterButton);

            var layerTypeBase1 = Creator.NewUIObject("Layer Type", editorDialog.transform);
            layerTypeBase1.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var layerType = EditorPrefabHolder.Instance.Dropdown.Duplicate(layerTypeBase1.transform);
            RectValues.Default.AnchoredPosition(112.5f, 16f).AnchorMax(0f, 0f).AnchorMin(0f, 0f).SizeDelta(198f, 32f).AssignToRectTransform(layerType.transform.AsRT());
            LayerTypeDropdown = layerType.GetComponent<Dropdown>();
            LayerTypeDropdown.options = CoreHelper.ToOptionData<EditorTimeline.LayerType>();

            RTEditor.GenerateSpacer("spacer", editorDialog.transform, new Vector2(765f, 4f));

            #endregion

            #region Name

            var labelNameBase = Creator.NewUIObject("Name Label", editorDialog.transform);
            labelNameBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var labelName = labelType.gameObject.Duplicate(labelNameBase.transform);
            labelName.transform.localPosition = Vector3.zero;
            labelName.transform.localScale = Vector3.one;
            labelName.transform.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelNameText = labelName.GetComponent<Text>();
            labelNameText.text = "Name";

            var nameTextBase1 = Creator.NewUIObject("Name", editorDialog.transform);
            nameTextBase1.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var name = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(nameTextBase1.transform);
            name.transform.localScale = Vector3.one;
            RectValues.Default.SizeDelta(740f, 32f).AssignToRectTransform(name.transform.AsRT());

            NameField = name.GetComponent<InputField>();
            NameField.lineType = InputField.LineType.MultiLineNewline;
            NameField.GetPlaceholderText().text = "Set name...";
            NameField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            RTEditor.GenerateSpacer("spacer", editorDialog.transform, new Vector2(765f, 4f));

            #endregion

            #region Description

            var labelDescriptionBase = Creator.NewUIObject("Description Label", editorDialog.transform);
            labelDescriptionBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var labelDescription = labelType.gameObject.Duplicate(labelDescriptionBase.transform);
            labelDescription.transform.localPosition = Vector3.zero;
            labelDescription.transform.localScale = Vector3.one;
            labelDescription.transform.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelDescriptionText = labelDescription.GetComponent<Text>();
            labelDescriptionText.text = "Description";
            labelDescriptionText.alignment = TextAnchor.UpperLeft;

            var descriptionTextBase1 = Creator.NewUIObject("Description", editorDialog.transform);
            descriptionTextBase1.transform.AsRT().sizeDelta = new Vector2(765f, 200f);

            var description = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(descriptionTextBase1.transform);
            description.transform.localScale = Vector3.one;
            RectValues.Default.SizeDelta(740f, 200f).AssignToRectTransform(description.transform.AsRT());

            DescriptionField = description.GetComponent<InputField>();
            DescriptionField.lineType = InputField.LineType.MultiLineNewline;
            DescriptionField.GetPlaceholderText().text = "Set description...";
            DescriptionField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            RTEditor.GenerateSpacer("spacer", editorDialog.transform, new Vector2(765f, 32f));

            #endregion

            #region Color

            var labelColorBase = Creator.NewUIObject("Color Label", editorDialog.transform);
            labelColorBase.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var labelColor = labelType.gameObject.Duplicate(labelColorBase.transform);
            labelColor.transform.localPosition = Vector3.zero;
            labelColor.transform.localScale = Vector3.one;
            labelColor.transform.AsRT().sizeDelta = new Vector2(725f, 32f);
            var labelColorText = labelColor.GetComponent<Text>();
            labelColorText.text = "Color";

            var colorOverrideBase1 = Creator.NewUIObject("Color", editorDialog.transform);
            colorOverrideBase1.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var toggle = EditorPrefabHolder.Instance.ToggleButton.Duplicate(colorOverrideBase1.transform, "toggle");
            RectValues.Default.AnchoredPosition(162.5f, 16f).AnchorMax(0f, 0f).AnchorMin(0f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(toggle.transform.AsRT());
            ColorOverrideToggle = toggle.GetComponent<ToggleButtonStorage>();
            ColorOverrideToggle.label.text = "Override";

            var colorTextBase1 = Creator.NewUIObject("Color", editorDialog.transform);
            colorTextBase1.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var color = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(colorTextBase1.transform);
            color.transform.localScale = Vector3.one;
            RectValues.Default.SizeDelta(740f, 32f).AssignToRectTransform(color.transform.AsRT());

            ColorField = color.GetComponent<InputField>();
            ColorField.lineType = InputField.LineType.MultiLineNewline;
            ColorField.GetPlaceholderText().text = "Set color...";
            ColorField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            RTEditor.GenerateSpacer("spacer", editorDialog.transform, new Vector2(765f, 4f));

            #endregion

            #region Editor Themes

            EditorThemeManager.ApplyGraphic(editorDialog.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorThemeManager.ApplyLightText(labelLayerText);
            EditorThemeManager.ApplyInputField(LayerField);
            EditorThemeManager.ApplyDropdown(LayerTypeDropdown);
            EditorThemeManager.ApplyLightText(labelNameText);
            EditorThemeManager.ApplyInputField(NameField);
            EditorThemeManager.ApplyLightText(labelDescriptionText);
            EditorThemeManager.ApplyInputField(DescriptionField);
            EditorThemeManager.ApplyLightText(labelColorText);
            EditorThemeManager.ApplyInputField(ColorField);
            EditorThemeManager.ApplyToggle(ColorOverrideToggle.toggle, graphic: ColorOverrideToggle.label);

            #endregion

            #endregion
        }
    }
}
