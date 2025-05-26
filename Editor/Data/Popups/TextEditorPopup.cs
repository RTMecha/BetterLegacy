using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Popups
{
    public class TextEditorPopup : EditorPopup
    {
        public TextEditorPopup() : base(TEXT_EDITOR) { }

        #region UI

        public InputField EditorField { get; set; }

        public FunctionButtonStorage UpdateButton { get; set; }

        public Text AutoUpdateLabel { get; set; }
        public Toggle AutoUpdateToggle { get; set; }

        #endregion

        public override void Init()
        {
            var textEditor = RTEditor.inst.SaveAsPopup.GameObject.Duplicate(RTEditor.inst.popups, "Text Editor");
            textEditor.transform.AsRT().anchoredPosition = Vector3.zero;

            var textEditorPopup = textEditor.transform.GetChild(0);
            textEditorPopup.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            textEditorPopup.transform.AsRT().sizeDelta = new Vector2(500f, 420f);

            var textEditorPopupPanel = textEditorPopup.Find("Panel");
            var textEditorPopupPanelTitle = textEditorPopupPanel.Find("Text").GetComponent<Text>();
            textEditorPopupPanelTitle.text = "Text Editor";

            var close = textEditorPopupPanel.Find("x").GetComponent<Button>();
            close.onClick.NewListener(Close);

            CoreHelper.Delete(textEditorPopup.Find("Level Name"));
            CoreHelper.Delete(textEditorPopup.Find("submit"));

            EditorField = textEditorPopup.Find("level-name").GetComponent<InputField>();
            EditorField.image.rectTransform.sizeDelta = new Vector2(492f, 344f);
            EditorField.textComponent.alignment = TextAnchor.UpperLeft;
            EditorField.GetPlaceholderText().alignment = TextAnchor.UpperLeft;
            EditorField.lineType = InputField.LineType.MultiLineNewline;
            EditorField.onValueChanged.ClearAll();
            EditorField.text = "";
            EditorField.characterLimit = 0;

            var overlay = Creator.NewUIObject("overlay", EditorField.transform);
            var overlayLayout = overlay.AddComponent<VerticalLayoutGroup>();
            overlayLayout.childControlHeight = false;
            overlayLayout.childForceExpandHeight = false;
            overlayLayout.spacing = 17f;
            RectValues.FullAnchored.AnchoredPosition(0f, -2f).AssignToRectTransform(overlay.transform.AsRT());

            for (int i = 0; i < 17; i++)
            {
                var a = Creator.NewUIObject($"line {i}", overlay.transform);
                a.transform.AsRT().sizeDelta = new Vector2(492f, 4f);
                var image = a.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.1f);
            }

            var updateLayout = Creator.NewUIObject("update", textEditorPopup);
            RectValues.Default.SizeDelta(492f, 20f).AssignToRectTransform(updateLayout.transform.AsRT());

            var label = EditorManager.inst.folderButtonPrefab.transform.GetChild(0).gameObject.Duplicate(updateLayout.transform, "label").GetComponent<Text>();
            RectValues.Default.AnchoredPosition(140f, 0f).SizeDelta(200f, 100f).AssignToRectTransform(label.rectTransform);
            AutoUpdateLabel = label;
            AutoUpdateLabel.alignment = TextAnchor.MiddleCenter;
            AutoUpdateLabel.text = "Auto-update";

            var toggle = EditorPrefabHolder.Instance.Toggle.Duplicate(updateLayout.transform, "auto").GetComponent<Toggle>();
            RectValues.Default.AnchoredPosition(226f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(toggle.transform.AsRT());

            AutoUpdateToggle = toggle;
            AutoUpdateToggle.onValueChanged.ClearAll();
            AutoUpdateToggle.isOn = RTTextEditor.inst.autoUpdate;
            AutoUpdateToggle.onValueChanged.AddListener(_val => RTTextEditor.inst.autoUpdate = _val);

            var update = EditorPrefabHolder.Instance.Function2Button.Duplicate(updateLayout.transform, "update").GetComponent<FunctionButtonStorage>();
            update.label.text = "Update";
            update.button.onClick.NewListener(RTTextEditor.inst.UpdateText);
            RectValues.Default.AnchoredPosition(-192f, 0f).SizeDelta(100f, 32f).AssignToRectTransform(update.transform.AsRT());
            UpdateButton = update;

            EditorThemeManager.AddGraphic(textEditorPopup.GetComponent<Image>(), ThemeGroup.Background_1, true);
            EditorThemeManager.AddGraphic(textEditorPopupPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

            EditorThemeManager.AddSelectable(close, ThemeGroup.Close, true);
            EditorThemeManager.AddGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorThemeManager.AddLightText(textEditorPopupPanelTitle);
            EditorThemeManager.AddInputField(EditorField);

            EditorThemeManager.AddSelectable(update.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(update.label, ThemeGroup.Function_2_Text);

            EditorThemeManager.AddToggle(toggle);
            EditorThemeManager.AddLightText(label);

            EditorHelper.AddEditorPopup(TEXT_EDITOR, textEditor);

            GameObject = textEditor;
        }
    }
}
