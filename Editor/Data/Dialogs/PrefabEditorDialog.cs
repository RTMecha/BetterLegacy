using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PrefabEditorDialog : EditorDialog, IServerDialog
    {
        public PrefabEditorDialog() : base() { }

        public RectTransform Content { get; set; }
        public FunctionButtonStorage TypeButton { get; set; }
        public InputField CreatorField { get; set; }
        public InputField NameField { get; set; }
        public InputField DescriptionField { get; set; }
        public InputField VersionField { get; set; }

        public RectTransform IconBase { get; set; }
        public Image IconImage { get; set; }

        public Button SelectIconButton { get; set; }
        public Toggle CollapseToggle { get; set; }

        public FunctionButtonStorage ImportPrefabButton { get; set; }

        public FunctionButtonStorage ConvertPrefabButton { get; set; }

        public RectTransform TagsScrollView { get; set; }

        public RectTransform TagsContent { get; set; }

        #region Server

        public RectTransform ServerBase { get; set; }
        public RectTransform ServerContent { get; set; }

        public Toggle RequireVersion { get; set; }

        public Dropdown VersionComparison { get; set; }

        public Dropdown ServerVisibilityDropdown { get; set; }

        public RectTransform CollaboratorsScrollView { get; set; }

        public RectTransform CollaboratorsContent { get; set; }

        public GameObject ChangelogLabel { get; set; }
        public GameObject Changelog { get; set; }
        public InputField ChangelogField { get; set; }

        public Text ServerIDText { get; set; }
        public ContextClickable ServerIDContextMenu { get; set; }
        public Text UserIDText { get; set; }
        public ContextClickable UserIDContextMenu { get; set; }

        public Button UploadButton { get; set; }
        public ContextClickable UploadContextMenu { get; set; }
        public Text UploadButtonText { get; set; }
        public Button PullButton { get; set; }
        public ContextClickable PullContextMenu { get; set; }
        public Button DeleteButton { get; set; }
        public ContextClickable DeleteContextMenu { get; set; }

        #endregion

        public override void Init()
        {
            if (init)
                return;

            var editorDialog = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "PrefabEditorDialog");
            var editorDialogStorage = editorDialog.GetComponent<EditorDialogStorage>();
            editorDialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            EditorHelper.AddEditorDialog(PREFAB_EXTERNAL_EDITOR, editorDialog);

            InitDialog(PREFAB_EXTERNAL_EDITOR);

            base.Init();

            #region Generation

            editorDialogStorage.topPanel.color = LSColors.HexToColor("4C4C4C");
            editorDialogStorage.title.text = "- Prefab Editor -";
            editorDialogStorage.title.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            var editorDialogSpacer = editorDialog.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            editorDialog.transform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 24f);

            var main = Creator.NewUIObject("Main", editorDialog.transform);
            main.transform.AsRT().sizeDelta = new Vector2(765f, 668f);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(main.transform, "Scroll View");
            RectValues.Default.SizeDelta(745f, 668f).AssignToRectTransform(scrollView.transform.AsRT());
            Content = scrollView.transform.Find("Viewport/Content").AsRT();

            CoreHelper.Delete(editorDialog.transform.GetChild(2));

            var buttons = Creator.NewUIObject("buttons", Content);
            buttons.transform.AsRT().sizeDelta = new Vector2(765f, 32f);

            var buttonsHLG = buttons.AddComponent<HorizontalLayoutGroup>();
            buttonsHLG.spacing = 60f;

            var importPrefab = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttons.transform, "import");
            importPrefab.SetActive(true);
            ImportPrefabButton = importPrefab.GetComponent<FunctionButtonStorage>();
            ImportPrefabButton.Text = "Import Prefab";

            var exportToVG = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttons.transform, "export");
            exportToVG.SetActive(true);
            ConvertPrefabButton = exportToVG.GetComponent<FunctionButtonStorage>();
            ConvertPrefabButton.Text = "Convert to VG Format";

            new Labels(Labels.InitSettings.Default.Parent(Content), "Type");

            var prefabType = EditorPrefabHolder.Instance.Function1Button.Duplicate(Content, "Show Type Editor");
            RectValues.Default.SizeDelta(742f, 34f).AssignToRectTransform(prefabType.transform.AsRT());
            TypeButton = prefabType.GetComponent<FunctionButtonStorage>();
            TypeButton.button.onClick.ClearAll();

            prefabType.AddComponent<ContrastColors>().Init(TypeButton.label, TypeButton.button.image);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Creator");

            var creator = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(Content);
            RectValues.Default.SizeDelta(740f, 32f).AssignToRectTransform(creator.transform.AsRT());

            CreatorField = creator.GetComponent<InputField>();
            CreatorField.GetPlaceholderText().text = "Set creator...";
            CreatorField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
            
            new Labels(Labels.InitSettings.Default.Parent(Content), "Name");

            var name = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(Content);
            RectValues.Default.SizeDelta(740f, 32f).AssignToRectTransform(name.transform.AsRT());

            NameField = name.GetComponent<InputField>();
            NameField.lineType = InputField.LineType.MultiLineNewline;
            NameField.GetPlaceholderText().text = "Set name...";
            NameField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Description");

            var description = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(Content);
            description.transform.localScale = Vector3.one;
            RectValues.Default.SizeDelta(740f, 300f).AssignToRectTransform(description.transform.AsRT());

            DescriptionField = description.GetComponent<InputField>();
            DescriptionField.lineType = InputField.LineType.MultiLineNewline;
            DescriptionField.GetPlaceholderText().text = "Set description...";
            DescriptionField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Prefab Version");

            var version = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(Content);
            RectValues.Default.SizeDelta(740f, 32f).AssignToRectTransform(version.transform.AsRT());

            VersionField = version.GetComponent<InputField>();
            VersionField.GetPlaceholderText().text = "Set version...";
            VersionField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            #region Icon

            var iconBase = Creator.NewUIObject("icon", Content);
            IconBase = iconBase.transform.AsRT();
            RectValues.Default.SizeDelta(764f, 574f).AssignToRectTransform(IconBase);
            new Labels(Labels.InitSettings.Default.Parent(IconBase).Rect(new RectValues(new Vector2(16f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, -32f))), new Label("Icon") { fontStyle = FontStyle.Bold, });

            var icon = Creator.NewUIObject("image", IconBase);
            IconImage = icon.AddComponent<Image>();
            icon.AddComponent<Button>();
            new RectValues(new Vector2(16f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(512f, 512f)).AssignToRectTransform(IconImage.rectTransform);

            var selectIcon = EditorPrefabHolder.Instance.Function2Button.Duplicate(IconBase, "select");
            new RectValues(new Vector2(240f, -62f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(150f, 32f)).AssignToRectTransform(selectIcon.transform.AsRT());
            var selectIconStorage = selectIcon.GetComponent<FunctionButtonStorage>();
            SelectIconButton = selectIconStorage.button;
            selectIconStorage.label.text = "Browse";

            EditorThemeManager.AddSelectable(SelectIconButton, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(selectIconStorage.label, ThemeGroup.Function_2_Text);

            var collapser = EditorPrefabHolder.Instance.CollapseToggle.Duplicate(IconBase, "collapse");
            new RectValues(new Vector2(340f, -62f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(32f, 32f)).AssignToRectTransform(collapser.transform.AsRT());
            CollapseToggle = collapser.GetComponent<Toggle>();

            EditorThemeManager.AddToggle(CollapseToggle, ThemeGroup.Background_1);

            for (int i = 0; i < collapser.transform.Find("dots").childCount; i++)
                EditorThemeManager.AddGraphic(collapser.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

            #endregion

            #region Tags

            new Labels(Labels.InitSettings.Default.Parent(Content), "Tags");
            var tagScrollView = Creator.NewUIObject("Tags Scroll View", Content);
            TagsScrollView = tagScrollView.transform.AsRT();
            RectValues.Default.SizeDelta(522f, 40f).AssignToRectTransform(TagsScrollView);

            var scroll = tagScrollView.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;

            var image = tagScrollView.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);

            tagScrollView.AddComponent<Mask>();

            var tagViewport = Creator.NewUIObject("Viewport", tagScrollView.transform);
            RectValues.FullAnchored.AssignToRectTransform(tagViewport.transform.AsRT());

            var tagContent = Creator.NewUIObject("Content", tagViewport.transform);
            TagsContent = tagContent.transform.AsRT();
            TagsContent.anchoredPosition = Vector2.zero;
            var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
            tagContentGLG.cellSize = new Vector2(168f, 32f);
            tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            tagContentGLG.constraintCount = 1;
            tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
            tagContentGLG.spacing = new Vector2(8f, 0f);

            var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
            tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            scroll.viewport = tagViewport.transform.AsRT();
            scroll.content = tagContent.transform.AsRT();

            #endregion

            #region Server

            var labelRect = new RectValues(new Vector2(16f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, -32f));

            var serverBase = Creator.NewUIObject("server", Content);
            ServerBase = serverBase.transform.AsRT();
            RectValues.Default.SizeDelta(764f, 800f).AssignToRectTransform(ServerBase);
            new Labels(Labels.InitSettings.Default.Parent(serverBase.transform).Rect(labelRect), new Label("Server") { fontStyle = FontStyle.Bold, });
            var server = Creator.NewUIObject("info", serverBase.transform);
            ServerContent = server.transform.AsRT();
            RectValues.FullAnchored.AnchorMax(1f, 0f).Pivot(0.5f, 0f).SizeDelta(-32f, 740f).AssignToRectTransform(ServerContent);
            var serverLayout = server.AddComponent<VerticalLayoutGroup>();
            serverLayout.childControlHeight = false;
            serverLayout.childForceExpandHeight = false;
            serverLayout.spacing = 4f;

            ChangelogLabel = new Labels(Labels.InitSettings.Default.Parent(server.transform), "Changelog").GameObject;
            var changelog = EditorPrefabHolder.Instance.StringInputField.Duplicate(server.transform, "changelog");
            changelog.transform.AsRT().sizeDelta = new Vector2(0f, 140f);
            ChangelogField = changelog.GetComponent<InputField>();
            ChangelogField.textComponent.alignment = TextAnchor.UpperLeft;
            ChangelogField.lineType = InputField.LineType.MultiLineNewline;
            EditorThemeManager.AddInputField(DescriptionField);
            var changelogLayoutElement = changelog.GetComponent<LayoutElement>();
            changelogLayoutElement.minHeight = 140f;
            changelogLayoutElement.preferredHeight = 140f;
            EditorThemeManager.AddInputField(ChangelogField);

            ServerVisibilityDropdown = GenerateDropdown(server.transform, "Server Visibility", true, CoreHelper.ToOptionData<ServerVisibility>());

            new Labels(Labels.InitSettings.Default.Parent(server.transform), "Collaborators");
            CollaboratorsScrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(server.transform, "Collaborators Scroll View").transform.AsRT();
            CollaboratorsContent = CollaboratorsScrollView.transform.Find("Viewport/Content").AsRT();

            CollaboratorsScrollView.transform.AsRT().sizeDelta = new Vector2(735f, 200f);

            CollaboratorsContent.GetComponent<VerticalLayoutGroup>().spacing = 8f;

            var serverID = new Labels(Labels.InitSettings.Default.Parent(server.transform).Rect(RectValues.Default.SizeDelta(0f, 40f)), new Label("Server ID:") { horizontalWrap = HorizontalWrapMode.Overflow });
            serverID.GameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            serverID.GameObject.AddComponent<Button>();
            ServerIDContextMenu = serverID.GameObject.GetOrAddComponent<ContextClickable>();
            ServerIDText = serverID.GameObject.transform.GetChild(0).GetComponent<Text>();

            var userID = new Labels(Labels.InitSettings.Default.Parent(server.transform).Rect(RectValues.Default.SizeDelta(0f, 40f)), new Label("User ID:") { horizontalWrap = HorizontalWrapMode.Overflow });
            userID.GameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            userID.GameObject.AddComponent<Button>();
            UserIDContextMenu = userID.GameObject.GetOrAddComponent<ContextClickable>();
            UserIDText = userID.GameObject.transform.GetChild(0).GetComponent<Text>();

            var serverButtons = Creator.NewUIObject("submit", server.transform);
            serverButtons.transform.AsRT().sizeDelta = new Vector2(0f, 64f);
            var buttonsLayout = serverButtons.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 8f;

            var upload = EditorPrefabHolder.Instance.Function1Button.Duplicate(serverButtons.transform, "upload");
            var uploadStorage = upload.GetComponent<FunctionButtonStorage>();
            uploadStorage.label.text = "Upload";
            UploadButton = uploadStorage.button;
            UploadButtonText = uploadStorage.label;
            UploadContextMenu = upload.GetOrAddComponent<ContextClickable>();
            EditorThemeManager.AddGraphic(uploadStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(uploadStorage.label, ThemeGroup.Function_1_Text);

            var pull = EditorPrefabHolder.Instance.Function1Button.Duplicate(serverButtons.transform, "pull");
            var pullStorage = pull.GetComponent<FunctionButtonStorage>();
            pullStorage.label.text = "Pull";
            PullButton = pullStorage.button;
            PullContextMenu = pull.GetOrAddComponent<ContextClickable>();
            EditorThemeManager.AddGraphic(pullStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(pullStorage.label, ThemeGroup.Function_1_Text);

            var delete = EditorPrefabHolder.Instance.Function1Button.Duplicate(serverButtons.transform, "delete");
            var deleteStorage = delete.GetComponent<FunctionButtonStorage>();
            deleteStorage.label.text = "Delete";
            DeleteButton = deleteStorage.button;
            DeleteContextMenu = delete.GetOrAddComponent<ContextClickable>();
            EditorThemeManager.AddGraphic(deleteStorage.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.AddGraphic(deleteStorage.label, ThemeGroup.Delete_Text);

            #endregion

            #endregion

            #region Editor Themes

            EditorThemeManager.AddGraphic(TypeButton.button.image, ThemeGroup.Null, true);

            EditorThemeManager.AddInputField(CreatorField);
            EditorThemeManager.AddInputField(NameField);
            EditorThemeManager.AddInputField(DescriptionField);
            EditorThemeManager.AddInputField(VersionField);

            EditorThemeManager.AddGraphic(editorDialog.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddSelectable(ImportPrefabButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(ImportPrefabButton.label, ThemeGroup.Function_2_Text);
            EditorThemeManager.AddSelectable(ConvertPrefabButton.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(ConvertPrefabButton.label, ThemeGroup.Function_2_Text);

            #endregion
        }

        public void CollapseIcon(bool collapse)
        {
            var size = collapse ? 32f : 512f;
            IconImage.rectTransform.sizeDelta = new Vector2(size, size);
            IconBase.transform.AsRT().sizeDelta = new Vector2(764f, collapse ? 94f : 574f);

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        }

        public void ShowChangelog(bool show)
        {
            ChangelogLabel.SetActive(show);
            ChangelogField.gameObject.SetActive(show);

            ServerBase.transform.AsRT().sizeDelta = new Vector2(750f, show ? 1020 : 854f);
            ServerContent.transform.AsRT().sizeDelta = new Vector2(-32f, show ? 960 : 794f);

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
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
            //var layoutElement = gameObject.GetOrAddComponent<LayoutElement>();
            //layoutElement.ignoreLayout = false;
            //layoutElement.minHeight = 32f;
            //layoutElement.minWidth = 200f;
            //layoutElement.preferredHeight = 32f;
            //layoutElement.preferredWidth = 200f;
            var toggleStorage = gameObject.GetComponent<ToggleButtonStorage>();
            toggleStorage.label.text = text;
            EditorThemeManager.AddToggle(toggleStorage.toggle, graphic: toggleStorage.label);
            return toggleStorage.toggle;
        }

        Dropdown GenerateDropdown(Transform content, Text creatorLinkTitle, string name, string text, int siblingIndex)
        {
            var dropdownBase = Creator.NewUIObject(name, content, siblingIndex);
            var dropdownBaseLayout = dropdownBase.AddComponent<HorizontalLayoutGroup>();
            dropdownBaseLayout.childControlHeight = true;
            dropdownBaseLayout.childControlWidth = false;
            dropdownBaseLayout.childForceExpandHeight = true;
            dropdownBaseLayout.childForceExpandWidth = false;
            dropdownBase.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var label = creatorLinkTitle.gameObject.Duplicate(dropdownBase.transform, "label");
            var labelText = label.GetComponent<Text>();
            labelText.text = text;
            labelText.rectTransform.sizeDelta = new Vector2(210f, 32f);
            EditorThemeManager.AddLightText(labelText);

            var dropdownObj = EditorPrefabHolder.Instance.Dropdown.Duplicate(dropdownBase.transform, "dropdown");
            var layoutElement = dropdownObj.GetOrAddComponent<LayoutElement>();
            layoutElement.preferredWidth = 126f;
            layoutElement.minWidth = 126f;
            dropdownObj.transform.AsRT().sizeDelta = new Vector2(256f, 32f);
            var dropdown = dropdownObj.GetComponent<Dropdown>();
            EditorThemeManager.AddDropdown(dropdown);
            return dropdown;
        }

        Toggle GenerateToggle(Transform content, Text creatorLinkTitle, string name, string text, int siblingIndex)
        {
            var toggleBase = Creator.NewUIObject(name, content, siblingIndex);
            var toggleBaseLayout = toggleBase.AddComponent<HorizontalLayoutGroup>();
            toggleBaseLayout.childControlHeight = true;
            toggleBaseLayout.childControlWidth = false;
            toggleBaseLayout.childForceExpandHeight = true;
            toggleBaseLayout.childForceExpandWidth = false;
            toggleBase.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var label = creatorLinkTitle.gameObject.Duplicate(toggleBase.transform, "label");
            var labelText = label.GetComponent<Text>();
            labelText.text = text;
            labelText.rectTransform.sizeDelta = new Vector2(210, 32f);
            EditorThemeManager.AddLightText(labelText);

            var toggleObj = EditorPrefabHolder.Instance.Toggle.Duplicate(toggleBase.transform, "toggle");
            var layoutElement = toggleObj.GetOrAddComponent<LayoutElement>();
            layoutElement.preferredWidth = 32f;
            layoutElement.minWidth = 32f;
            var toggle = toggleObj.GetComponent<Toggle>();
            EditorThemeManager.AddToggle(toggle);
            return toggle;
        }
    }
}
