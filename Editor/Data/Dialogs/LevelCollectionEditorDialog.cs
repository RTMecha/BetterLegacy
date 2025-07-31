using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class LevelCollectionEditorDialog : EditorDialog
    {
        public LevelCollectionEditorDialog() : base() { }

        #region Values

        public Transform Content { get; set; }

        public InputField NameField { get; set; }

        public InputField DescriptionField { get; set; }

        public InputField CreatorField { get; set; }

        #endregion

        #region Methods

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var editorDialogObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "LevelCollectionDialog");
            editorDialogObject.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = editorDialogObject.GetComponent<EditorDialogStorage>();

            dialogStorage.title.text = "- Level Collection Editor -";

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorThemeManager.AddGraphic(dialogStorage.topPanel, ThemeGroup.Add);
            EditorThemeManager.AddGraphic(dialogStorage.title, ThemeGroup.Add_Text);

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            CoreHelper.Delete(editorDialogObject.transform.GetChild(2).gameObject);

            EditorHelper.AddEditorDialog(LEVEL_COLLECTION_EDITOR, editorDialogObject);

            InitDialog(LEVEL_COLLECTION_EDITOR);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(editorDialogObject.transform, "Scroll View");
            Content = scrollView.transform.Find("Viewport/Content");

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 280f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 542f);

            #region Setup

            new Labels(Labels.InitSettings.Default.Parent(Content), "Name");
            var name = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "name");
            NameField = name.GetComponent<InputField>();
            EditorThemeManager.AddInputField(NameField);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Description");
            var description = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "description");
            DescriptionField = description.GetComponent<InputField>();
            EditorThemeManager.AddInputField(DescriptionField);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Creator");
            var creator = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "creator");
            CreatorField = creator.GetComponent<InputField>();
            EditorThemeManager.AddInputField(CreatorField);

            #endregion
        }

        #endregion
    }
}
