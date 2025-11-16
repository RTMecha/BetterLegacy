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
    public class LevelInfoEditorDialog : EditorDialog
    {
        public LevelInfoEditorDialog() : base() { }

        public RectTransform Content { get; set; }

        #region Values

        #region Reference

        public InputField PathField { get; set; }

        public InputField EditorPathField { get; set; }

        public InputField SongArtistField { get; set; }
        public InputField SongTitleField { get; set; }

        public InputField NameField { get; set; }

        public InputField CreatorField { get; set; }

        public RectTransform DifficultyContent { get; set; }

        public InputField ArcadeIDField { get; set; }

        public InputField ServerIDField { get; set; }

        public InputField WorkshopIDField { get; set; }

        #endregion

        #region Icon

        public RectTransform IconBase { get; set; }
        public Image IconImage { get; set; }

        public Button SelectIconButton { get; set; }
        public Toggle CollapseIconToggle { get; set; }

        #endregion

        #region Overwrite

        public Toggle UnlockRequiredToggle { get; set; }
        public Toggle OverwriteUnlockRequiredToggle { get; set; }
        
        public Toggle UnlockCompletionToggle { get; set; }
        public Toggle OverwriteUnlockCompletionToggle { get; set; }

        #endregion

        public Toggle HiddenToggle { get; set; }

        public Toggle ShowAfterUnlockToggle { get; set; }

        public Toggle SkipToggle { get; set; }

        public Button SubmitButton { get; set; }

        #endregion

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var editorDialogObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "LevelCollectionDialog");
            editorDialogObject.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = editorDialogObject.GetComponent<EditorDialogStorage>();

            dialogStorage.title.text = "- Level Info Editor -";

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorThemeManager.AddGraphic(dialogStorage.topPanel, ThemeGroup.Add);
            EditorThemeManager.AddGraphic(dialogStorage.title, ThemeGroup.Add_Text);

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            EditorHelper.AddEditorDialog(LEVEL_COLLECTION_EDITOR, editorDialogObject);

            InitDialog(LEVEL_COLLECTION_EDITOR);

            CoreHelper.Delete(GameObject.transform.Find("spacer"));
            CoreHelper.Delete(GameObject.transform.Find("Text"));

            var main = Creator.NewUIObject("Main", editorDialogObject.transform);
            main.transform.AsRT().sizeDelta = new Vector2(765f, 696f);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(main.transform, "Scroll View");
            RectValues.Default.SizeDelta(745f, 696f).AssignToRectTransform(scrollView.transform.AsRT());
            Content = scrollView.transform.Find("Viewport/Content").AsRT();

            #region Setup

            #region Reference

            new Labels(Labels.InitSettings.Default.Parent(Content), "Path");
            var path = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "path");
            PathField = path.GetComponent<InputField>();
            EditorThemeManager.AddInputField(PathField);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Editor Path");
            var editorPath = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "editor path");
            EditorPathField = editorPath.GetComponent<InputField>();
            EditorThemeManager.AddInputField(EditorPathField);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Song Artist");
            var songArtist = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "song artist");
            SongArtistField = songArtist.GetComponent<InputField>();
            EditorThemeManager.AddInputField(SongArtistField);
            
            new Labels(Labels.InitSettings.Default.Parent(Content), "Song Title");
            var songTitle = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "song title");
            SongTitleField = songTitle.GetComponent<InputField>();
            EditorThemeManager.AddInputField(SongTitleField);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Name");
            var name = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "name");
            NameField = name.GetComponent<InputField>();
            EditorThemeManager.AddInputField(NameField);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Creator");
            var creator = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "creator");
            CreatorField = creator.GetComponent<InputField>();
            EditorThemeManager.AddInputField(CreatorField);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Difficulty");
            var difficulty = Creator.NewUIObject("difficulty", Content);
            var difficultyLayout = difficulty.AddComponent<HorizontalLayoutGroup>();
            difficultyLayout.childControlHeight = false;
            difficultyLayout.childControlWidth = false;
            DifficultyContent = difficultyLayout.transform.AsRT();
            var difficultyLayoutElement = difficulty.AddComponent<LayoutElement>();
            difficultyLayoutElement.minHeight = 32f;
            difficultyLayoutElement.preferredHeight = 32f;
            difficulty.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Arcade ID");
            var arcadeID = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "arcade id");
            ArcadeIDField = arcadeID.GetComponent<InputField>();
            EditorThemeManager.AddInputField(ArcadeIDField);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Server ID");
            var serverID = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "server id");
            ServerIDField = serverID.GetComponent<InputField>();
            EditorThemeManager.AddInputField(ServerIDField);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Workshop ID");
            var workshopID = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "workshop id");
            WorkshopIDField = workshopID.GetComponent<InputField>();
            EditorThemeManager.AddInputField(WorkshopIDField);

            #endregion

            #region Icon

            var labelRect = new RectValues(new Vector2(16f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, -32f));

            var iconBase = Creator.NewUIObject("icon", Content);
            IconBase = iconBase.transform.AsRT();
            RectValues.Default.SizeDelta(764f, 574f).AssignToRectTransform(IconBase);
            new Labels(Labels.InitSettings.Default.Parent(IconBase).Rect(labelRect), new Label("Icon") { fontStyle = FontStyle.Bold, });

            var icon = Creator.NewUIObject("image", IconBase);
            IconImage = icon.AddComponent<Image>();
            new RectValues(new Vector2(16f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(512f, 512f)).AssignToRectTransform(IconImage.rectTransform);

            var selectIcon = EditorPrefabHolder.Instance.Function2Button.Duplicate(IconBase, "select");
            new RectValues(new Vector2(240f, -62f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(150f, 32f)).AssignToRectTransform(selectIcon.transform.AsRT());
            var selectIconStorage = selectIcon.GetComponent<FunctionButtonStorage>();
            SelectIconButton = selectIconStorage.button;
            selectIconStorage.Text = "Browse";

            EditorThemeManager.AddSelectable(SelectIconButton, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(selectIconStorage.label, ThemeGroup.Function_2_Text);

            var collapseIcon = EditorPrefabHolder.Instance.CollapseToggle.Duplicate(IconBase, "collapse");
            new RectValues(new Vector2(340f, -62f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(32f, 32f)).AssignToRectTransform(collapseIcon.transform.AsRT());
            CollapseIconToggle = collapseIcon.GetComponent<Toggle>();

            EditorThemeManager.AddToggle(CollapseIconToggle, ThemeGroup.Background_1);

            for (int i = 0; i < collapseIcon.transform.Find("dots").childCount; i++)
                EditorThemeManager.AddGraphic(collapseIcon.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

            #endregion

            #region Overwrite

            new Labels(Labels.InitSettings.Default.Parent(Content), "Overwrite");

            UnlockRequiredToggle = GenerateToggle(Content, "Unlock Required");
            OverwriteUnlockRequiredToggle = GenerateToggle(Content, "Overwrite");

            UnlockCompletionToggle = GenerateToggle(Content, "Unlock After Completion");
            OverwriteUnlockCompletionToggle = GenerateToggle(Content, "Overwrite");

            #endregion

            new Labels(Labels.InitSettings.Default.Parent(Content), "Behavior");

            HiddenToggle = GenerateToggle(Content, "Hidden");
            ShowAfterUnlockToggle = GenerateToggle(Content, "Show After Unlock");
            SkipToggle = GenerateToggle(Content, "Skip");

            var submit = EditorPrefabHolder.Instance.Function1Button.Duplicate(Content, "submit");
            submit.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var submitStorage = submit.GetComponent<FunctionButtonStorage>();
            SubmitButton = submitStorage.button;
            submitStorage.Text = "Submit";

            EditorThemeManager.AddGraphic(submitStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(submitStorage.label, ThemeGroup.Function_1_Text);

            #endregion
        }

        public void CollapseIcon(bool collapse)
        {
            var size = collapse ? 32f : 512f;
            IconImage.rectTransform.sizeDelta = new Vector2(size, size);
            IconBase.transform.AsRT().sizeDelta = new Vector2(764f, collapse ? 94f : 574f);

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
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
