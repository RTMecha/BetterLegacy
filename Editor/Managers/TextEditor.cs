using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;

namespace BetterLegacy.Editor.Managers
{
    public class TextEditor : MonoBehaviour
    {
        public static TextEditor inst;

        public static void Init() => Creator.NewGameObject(nameof(TextEditor), EditorManager.inst.transform.parent).AddComponent<TextEditor>();

        void Awake()
        {
            inst = this;
            CoreHelper.StartCoroutine(SetupUI());
        }

        IEnumerator SetupUI()
        {
            try
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
                close.onClick.ClearAll();
                close.onClick.AddListener(() => RTEditor.inst.TextEditorPopup.Close());

                Destroy(textEditorPopup.Find("Level Name").gameObject);
                Destroy(textEditorPopup.Find("submit").gameObject);

                editor = textEditorPopup.Find("level-name").GetComponent<InputField>();
                editor.image.rectTransform.sizeDelta = new Vector2(492f, 344f);
                editor.textComponent.alignment = TextAnchor.UpperLeft;
                editor.GetPlaceholderText().alignment = TextAnchor.UpperLeft;
                editor.lineType = InputField.LineType.MultiLineNewline;
                editor.onValueChanged.ClearAll();
                editor.text = "";
                editor.characterLimit = 0;

                var overlay = Creator.NewUIObject("overlay", editor.transform);
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
                label.alignment = TextAnchor.MiddleCenter;
                label.text = "Auto-update";

                var toggle = EditorPrefabHolder.Instance.Toggle.Duplicate(updateLayout.transform, "auto").GetComponent<Toggle>();
                RectValues.Default.AnchoredPosition(226f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(toggle.transform.AsRT());

                toggle.onValueChanged.ClearAll();
                toggle.isOn = autoUpdate;
                toggle.onValueChanged.AddListener(_val => autoUpdate = _val);

                var update = EditorPrefabHolder.Instance.Function2Button.Duplicate(updateLayout.transform, "update").GetComponent<FunctionButtonStorage>();
                update.text.text = "Update";
                RectValues.Default.AnchoredPosition(-192f, 0f).SizeDelta(100f, 32f).AssignToRectTransform(update.transform.AsRT());

                update.button.onClick.ClearAll();
                update.button.onClick.AddListener(UpdateText);

                EditorThemeManager.AddGraphic(textEditorPopup.GetComponent<Image>(), ThemeGroup.Background_1, true);
                EditorThemeManager.AddGraphic(textEditorPopupPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

                EditorThemeManager.AddSelectable(close, ThemeGroup.Close, true);
                EditorThemeManager.AddGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

                EditorThemeManager.AddLightText(textEditorPopupPanelTitle);
                EditorThemeManager.AddInputField(editor);

                EditorThemeManager.AddSelectable(update.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(update.text, ThemeGroup.Function_2_Text);

                EditorThemeManager.AddToggle(toggle);
                EditorThemeManager.AddLightText(label);

                EditorHelper.AddEditorPopup(EditorPopup.TEXT_EDITOR, textEditor);

                RTEditor.inst.TextEditorPopup = new EditorPopup(EditorPopup.TEXT_EDITOR);
                RTEditor.inst.TextEditorPopup.GameObject = textEditor;
                RTEditor.inst.editorPopups.Add(RTEditor.inst.TextEditorPopup);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            yield break;
        }

        public bool autoUpdate = true;

        public InputField editor;
        public InputField currentInputField;

        public void UpdateText()
        {
            if (!currentInputField)
                return;

            currentInputField.text = editor.text;
        }

        public void SetInputField(InputField inputField)
        {
            if (!inputField)
                return;

            RTEditor.inst.TextEditorPopup.Open();

            currentInputField = inputField;
            editor.onValueChanged.ClearAll();
            editor.text = currentInputField.text;
            editor.onValueChanged.AddListener(SetText);
        }

        void SetText(string _val)
        {
            if (!currentInputField || !autoUpdate)
                return;
            currentInputField.text = _val;
        }
    }
}
