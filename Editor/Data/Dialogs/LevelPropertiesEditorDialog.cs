using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class LevelPropertiesEditorDialog : EditorDialog
    {
        public LevelPropertiesEditorDialog() : base() { }

        public RectTransform Content { get; set; }

        public InputFieldStorage LevelStartOffsetField { get; set; }

        public Toggle ReverseToggle { get; set; }

        public InputFieldStorage LevelEndOffsetField { get; set; }

        public Toggle AutoEndLevelToggle { get; set; }

        public Dropdown LevelEndFunctionDropdown { get; set; }

        public InputField LevelEndDataField { get; set; }

        public ModifiersEditorDialog LevelModifiers { get; set; }

        public List<ModifiersEditorDialog> ModifierBlocks { get; set; } = new List<ModifiersEditorDialog>();

        public RectTransform ModifierBlocksContent { get; set; }

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var editorDialogObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "LevelPropertiesDialog");
            editorDialogObject.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = editorDialogObject.GetComponent<EditorDialogStorage>();

            dialogStorage.title.text = "- Level Properties Editor -";

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorThemeManager.AddGraphic(dialogStorage.topPanel, ThemeGroup.Add);
            EditorThemeManager.AddGraphic(dialogStorage.title, ThemeGroup.Add_Text);

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            EditorHelper.AddEditorDialog(LEVEL_PROPERTIES_EDITOR, editorDialogObject);

            InitDialog(LEVEL_PROPERTIES_EDITOR);

            CoreHelper.Delete(GameObject.transform.Find("spacer"));
            CoreHelper.Delete(GameObject.transform.Find("Text"));

            var main = Creator.NewUIObject("Main", editorDialogObject.transform);
            main.transform.AsRT().sizeDelta = new Vector2(765f, 696f);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(main.transform, "Scroll View");
            RectValues.Default.SizeDelta(745f, 696f).AssignToRectTransform(scrollView.transform.AsRT());
            Content = scrollView.transform.Find("Viewport/Content").AsRT();

            #region Setup

            new Labels(Labels.InitSettings.Default.Parent(Content), new Label("General Settings") { fontStyle = FontStyle.Bold, });
            new Labels(Labels.InitSettings.Default.Parent(Content), "Start Offset");
            var levelStartOffset = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Content, "level start offset");
            levelStartOffset.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            LevelStartOffsetField = levelStartOffset.GetComponent<InputFieldStorage>();
            EditorThemeManager.AddInputField(LevelStartOffsetField);

            ReverseToggle = GenerateToggle(Content, "Level Can Rewind");

            new Labels(Labels.InitSettings.Default.Parent(Content), new Label("End Settings") { fontStyle = FontStyle.Bold, });
            new Labels(Labels.InitSettings.Default.Parent(Content), "End Offset");
            var levelEndOffset = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Content, "level end offset");
            levelEndOffset.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            LevelEndOffsetField = levelEndOffset.GetComponent<InputFieldStorage>();
            EditorThemeManager.AddInputField(LevelEndOffsetField);

            AutoEndLevelToggle = GenerateToggle(Content, "Auto End");

            LevelEndFunctionDropdown = GenerateDropdown(Content, "End Function", true, CoreHelper.ToOptionData<EndLevelFunction>());

            new Labels(Labels.InitSettings.Default.Parent(Content), "End Data");
            var levelEndData = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "level end data");
            levelEndData.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            LevelEndDataField = levelEndData.GetComponent<InputField>();
            EditorThemeManager.AddInputField(LevelEndDataField);

            new Labels(Labels.InitSettings.Default.Parent(Content), new Label("Level Modifiers") { fontStyle = FontStyle.Bold, });
            LevelModifiers = new ModifiersEditorDialog();
            LevelModifiers.Init(Content.transform, false, false, false);

            new Labels(Labels.InitSettings.Default.Parent(Content), new Label("Modifier Blocks") { fontStyle = FontStyle.Bold, });
            var modifierBlocksScrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(Content, "Modifier Blocks");
            modifierBlocksScrollView.transform.AsRT().sizeDelta = new Vector2(765f, 400f);
            ModifierBlocksContent = modifierBlocksScrollView.transform.Find("Viewport/Content").AsRT();

            #endregion
        }

        Dropdown GenerateDropdown(Transform parent, string name, bool doLabel, List<Dropdown.OptionData> list)
        {
            if (doLabel)
                new Labels(Labels.InitSettings.Default.Parent(parent), new Label(name));
            var gameObject = EditorPrefabHolder.Instance.Dropdown.Duplicate(parent, name.ToLower());
            gameObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var layoutElement = gameObject.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = false;
            layoutElement.minWidth = 200f;
            layoutElement.preferredWidth = 200f;
            var dropdown = gameObject.GetComponent<Dropdown>();
            dropdown.options = list;
            EditorThemeManager.AddDropdown(dropdown);
            return dropdown;
        }

        Toggle GenerateToggle(Transform parent, string text)
        {
            var gameObject = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parent, text.ToLower());
            gameObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var toggleStorage = gameObject.GetComponent<ToggleButtonStorage>();
            toggleStorage.label.text = text;
            EditorThemeManager.AddToggle(toggleStorage.toggle, graphic: toggleStorage.label);
            return toggleStorage.toggle;
        }
    }
}
