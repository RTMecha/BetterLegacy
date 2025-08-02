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
            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(editorDialogObject.transform, "Scroll View");
            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 696f);
            Content = scrollView.transform.Find("Viewport/Content").AsRT();

            #region Setup

            new Labels(Labels.InitSettings.Default.Parent(Content), "Level Modifiers");
            LevelModifiers = new ModifiersEditorDialog();
            LevelModifiers.Init(Content.transform, false, false, false);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Modifier Blocks");
            var modifierBlocksScrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(Content, "Modifier Blocks");
            modifierBlocksScrollView.transform.AsRT().sizeDelta = new Vector2(765f, 400f);
            ModifierBlocksContent = modifierBlocksScrollView.transform.Find("Viewport/Content").AsRT();

            #endregion
        }
    }
}
