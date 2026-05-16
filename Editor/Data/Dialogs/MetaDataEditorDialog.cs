using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

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
    public class MetaDataEditorDialog : EditorDialog, IServerDialog, ITagDialog
    {
        public MetaDataEditorDialog() : base(METADATA_EDITOR) { }

        #region Values

        public RectTransform Content { get; set; }

        public bool Setup { get; set; } = false;

        #region Artist

        public InputField ArtistNameField { get; set; }
        public Button OpenArtistURLButton { get; set; }
        public InputField ArtistLinkField { get; set; }
        public Dropdown ArtistLinkTypeDropdown { get; set; }

        #endregion

        #region Song

        public InputField SongTitleField { get; set; }
        public Button OpenSongURLButton { get; set; }
        public InputField SongLinkField { get; set; }
        public Dropdown SongLinkTypeDropdown { get; set; }

        #endregion

        #region Creator

        public InputField UploaderNameField { get; set; }
        public InputField CreatorNameField { get; set; }
        public Button OpenCreatorURLButton { get; set; }
        public InputField CreatorLinkField { get; set; }
        public Dropdown CreatorLinkTypeDropdown { get; set; }

        #endregion

        #region Beatmap

        public InputField LevelNameField { get; set; }

        public InputField DescriptionField { get; set; }

        public RectTransform DifficultyContent { get; set; }

        public Button OpenVideoURLButton { get; set; }
        public InputField VideoLinkField { get; set; }
        public Dropdown VideoLinkTypeDropdown { get; set; }

        #endregion

        public RectTransform IconBase { get; set; }
        public Image IconImage { get; set; }

        public Button SelectIconButton { get; set; }
        public Toggle CollapseToggle { get; set; }

        public RectTransform TagsScrollView { get; set; }

        public RectTransform TagsContent { get; set; }

        public InputField VersionField { get; set; }

        public RectTransform ServerParent { get; set; }

        GameObject linkPrefab;

        #region Package

        public ToggleButtonStorage PackageToggle { get; set; }

        public RectTransform PackageContent { get; set; }

        public InputField MainAudioField { get; set; }

        public InputField MainPreviewAudioField { get; set; }

        public InputField MainCoverField { get; set; }

        public InputField MainLockedCoverField { get; set; }

        public InputField MainLevelField { get; set; }

        public RectTransform FileContent { get; set; }

        #endregion

        #region Settings

        public ToggleButtonStorage IsHubLevelToggle { get; set; }
        public ToggleButtonStorage UnlockRequiredToggle { get; set; }
        public ToggleButtonStorage UnlockCompletedToggle { get; set; }

        public Dropdown PreferredPlayerCountDropdown { get; set; }
        public Dropdown PreferredControlTypeDropdown { get; set; }

        public ToggleButtonStorage HideIntroToggle { get; set; }

        public ToggleButtonStorage ReplayEndLevelOffToggle { get; set; }

        #endregion

        #region Server

        public RectTransform ServerContent { get; set; }

        public ToggleButtonStorage RequireVersion { get; set; }

        public Dropdown VersionComparison { get; set; }

        public Dropdown ServerVisibilityDropdown { get; set; }

        public RectTransform CollaboratorsScrollView { get; set; }

        public RectTransform CollaboratorsContent { get; set; }

        public GameObject ChangelogLabel { get; set; }
        public GameObject Changelog { get; set; }
        public InputField ChangelogField { get; set; }

        public Text ModdedDisplayText { get; set; }

        public Text ArcadeIDText { get; set; }
        public ContextClickable ArcadeIDContextMenu { get; set; }
        public Text ServerIDText { get; set; }
        public ContextClickable ServerIDContextMenu { get; set; }
        public Text UserIDText { get; set; }
        public ContextClickable UserIDContextMenu { get; set; }

        public Button ConvertButton { get; set; }
        public ContextClickable ConvertContextMenu { get; set; }
        public Button UploadButton { get; set; }
        public ContextClickable UploadContextMenu { get; set; }
        public Text UploadButtonText { get; set; }
        public Button PullButton { get; set; }
        public ContextClickable PullContextMenu { get; set; }
        public Button DeleteButton { get; set; }
        public ContextClickable DeleteContextMenu { get; set; }

        #endregion

        #endregion

        #region Functions

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var old = GameObject.Duplicate(EditorManager.inst.dialogs, Name + " Old");

            RTMetaDataEditor.inst.difficultyToggle = old.transform.Find("Scroll View/Viewport/Content/song/difficulty/toggles/easy").gameObject;
            RTMetaDataEditor.inst.difficultyToggle.transform.SetParent(RTMetaDataEditor.inst.transform);

            RTMetaDataEditor.inst.filePefab = Creator.NewUIObject("File", RTMetaDataEditor.inst.transform);
            // File Prefab
            {
                var horizontalLayoutGroup = RTMetaDataEditor.inst.filePefab.AddComponent<HorizontalLayoutGroup>();
                horizontalLayoutGroup.spacing = 8f;

                var idField = EditorPrefabHolder.Instance.StringInputField.Duplicate(RTMetaDataEditor.inst.filePefab.transform, "id");
                var fileNameField = EditorPrefabHolder.Instance.StringInputField.Duplicate(RTMetaDataEditor.inst.filePefab.transform, "file name");
                var deleteButton = EditorPrefabHolder.Instance.DeleteButton.Duplicate(RTMetaDataEditor.inst.filePefab.transform, "delete");
                deleteButton.GetComponent<LayoutElement>().ignoreLayout = false;
            }

            CoreHelper.Delete(GameObject);

            GameObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, Name);
            GameObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            GetLegacyDialog().Dialog = GameObject.transform;
            var editorDialogStorage = GameObject.GetComponent<EditorDialogStorage>();
            editorDialogStorage.title.text = "- Metadata Editor -";
            editorDialogStorage.title.color = Color.white;
            editorDialogStorage.topPanel.color = RTColors.HexToColor("373737");

            CoreHelper.Delete(GameObject.transform.Find("spacer"));
            CoreHelper.Delete(GameObject.transform.Find("Text"));
            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(GameObject.transform, "Scroll View");
            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 696f);
            Content = scrollView.transform.Find("Viewport/Content").AsRT();
            Content.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(left: 8, right: 8, top: 8, bottom: 8);

            EditorThemeManager.ApplyGraphic(GameObject.GetComponent<Image>(), ThemeGroup.Background_1);

            var labelRect = new RectValues(new Vector2(16f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 16f));

            #region Prefabs

            linkPrefab = Creator.NewUIObject("link", RTMetaDataEditor.inst.transform);
            linkPrefab.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var linkPrefabLayout = linkPrefab.AddComponent<HorizontalLayoutGroup>();
            linkPrefabLayout.spacing = 4f;

            var openLink = EditorPrefabHolder.Instance.DeleteButton.Duplicate(linkPrefab.transform, "open url");
            var openLinkLayoutElement = openLink.GetComponent<LayoutElement>();
            openLinkLayoutElement.ignoreLayout = false;
            openLinkLayoutElement.minWidth = 32f;
            openLinkLayoutElement.preferredWidth = 32f;
            var openLinkStorage = openLink.GetComponent<DeleteButtonStorage>();
            openLinkStorage.Sprite = EditorSprites.LinkSprite;

            var cb = openLinkStorage.button.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 1f);
            cb.pressedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            cb.selectedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            openLinkStorage.button.colors = cb;

            var linkType = EditorPrefabHolder.Instance.Dropdown.Duplicate(linkPrefab.transform, "type");
            var linkTypeLayoutElement = linkType.GetComponent<LayoutElement>();
            linkTypeLayoutElement.ignoreLayout = false;
            linkTypeLayoutElement.minWidth = 200f;
            linkTypeLayoutElement.preferredWidth = 200f;

            EditorPrefabHolder.Instance.StringInputField.Duplicate(linkPrefab.transform, "input");

            #endregion

            #region Artist

            new Labels(Labels.InitSettings.Default.Parent(Content).Rect(labelRect), new Label("Artist") { fontStyle = FontStyle.Bold, });
            var artist = Creator.NewUIObject("artist", Content);
            var artistLayout = artist.AddComponent<VerticalLayoutGroup>();
            artistLayout.childControlHeight = false;
            artistLayout.childForceExpandHeight = false;
            artistLayout.spacing = 4f;
            artist.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.MinSize;

            new Labels(Labels.InitSettings.Default.Parent(artist.transform), "Name");
            var artistName = EditorPrefabHolder.Instance.StringInputField.Duplicate(artist.transform, "name");
            ArtistNameField = artistName.GetComponent<InputField>();
            ArtistNameField.textComponent.alignment = TextAnchor.MiddleLeft;
            EditorThemeManager.ApplyInputField(ArtistNameField);

            new Labels(Labels.InitSettings.Default.Parent(artist.transform), "Link");
            var artistLinkBase = linkPrefab.Duplicate(artist.transform, "link");

            var openArtistLinkButton = artistLinkBase.transform.Find("open url").GetComponent<DeleteButtonStorage>();
            OpenArtistURLButton = openArtistLinkButton.button;
            EditorThemeManager.ApplyGraphic(openArtistLinkButton.baseImage, ThemeGroup.Copy, true);
            EditorThemeManager.ApplyGraphic(openArtistLinkButton.image, ThemeGroup.Copy_Text);

            ArtistLinkTypeDropdown = artistLinkBase.transform.Find("type").GetComponent<Dropdown>();
            ArtistLinkTypeDropdown.options = AlephNetwork.ArtistLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
            EditorThemeManager.ApplyDropdown(ArtistLinkTypeDropdown);

            ArtistLinkField = artistLinkBase.transform.Find("input").GetComponent<InputField>();
            ArtistLinkField.textComponent.alignment = TextAnchor.MiddleLeft;
            EditorThemeManager.ApplyInputField(ArtistLinkField);

            #endregion

            #region Song

            new Labels(Labels.InitSettings.Default.Parent(Content).Rect(labelRect), new Label("Song") { fontStyle = FontStyle.Bold, });
            var song = Creator.NewUIObject("song", Content);
            var songLayout = song.AddComponent<VerticalLayoutGroup>();
            songLayout.childControlHeight = false;
            songLayout.childForceExpandHeight = false;
            songLayout.spacing = 4f;
            song.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.MinSize;

            new Labels(Labels.InitSettings.Default.Parent(song.transform), "Title");
            var songTitle = EditorPrefabHolder.Instance.StringInputField.Duplicate(song.transform, "title");
            SongTitleField = songTitle.GetComponent<InputField>();
            SongTitleField.textComponent.alignment = TextAnchor.MiddleLeft;
            EditorThemeManager.ApplyInputField(SongTitleField);

            new Labels(Labels.InitSettings.Default.Parent(song.transform), "Link");
            var songLinkBase = linkPrefab.Duplicate(song.transform, "link");

            var openSongLinkButton = songLinkBase.transform.Find("open url").GetComponent<DeleteButtonStorage>();
            OpenSongURLButton = openSongLinkButton.button;
            EditorThemeManager.ApplyGraphic(openSongLinkButton.baseImage, ThemeGroup.Copy, true);
            EditorThemeManager.ApplyGraphic(openSongLinkButton.image, ThemeGroup.Copy_Text);

            SongLinkTypeDropdown = songLinkBase.transform.Find("type").GetComponent<Dropdown>();
            SongLinkTypeDropdown.options = AlephNetwork.SongLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
            EditorThemeManager.ApplyDropdown(SongLinkTypeDropdown);

            SongLinkField = songLinkBase.transform.Find("input").GetComponent<InputField>();
            SongLinkField.textComponent.alignment = TextAnchor.MiddleLeft;
            EditorThemeManager.ApplyInputField(SongLinkField);

            #endregion

            #region Creator

            new Labels(Labels.InitSettings.Default.Parent(Content).Rect(labelRect), new Label("Creator") { fontStyle = FontStyle.Bold, });
            var creator = Creator.NewUIObject("creator", Content);
            var creatorLayout = creator.AddComponent<VerticalLayoutGroup>();
            creatorLayout.childControlHeight = false;
            creatorLayout.childForceExpandHeight = false;
            creatorLayout.spacing = 4f;
            creator.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.MinSize;

            new Labels(Labels.InitSettings.Default.Parent(creator.transform), "Name");
            var creatorName = EditorPrefabHolder.Instance.StringInputField.Duplicate(creator.transform, "name");
            CreatorNameField = creatorName.GetComponent<InputField>();
            CreatorNameField.textComponent.alignment = TextAnchor.MiddleLeft;
            EditorThemeManager.ApplyInputField(CreatorNameField);

            new Labels(Labels.InitSettings.Default.Parent(creator.transform), "Link");
            var creatorLinkBase = linkPrefab.Duplicate(creator.transform, "link");

            var openCreatorLinkButton = creatorLinkBase.transform.Find("open url").GetComponent<DeleteButtonStorage>();
            OpenCreatorURLButton = openCreatorLinkButton.button;
            EditorThemeManager.ApplyGraphic(openCreatorLinkButton.baseImage, ThemeGroup.Copy, true);
            EditorThemeManager.ApplyGraphic(openCreatorLinkButton.image, ThemeGroup.Copy_Text);

            CreatorLinkTypeDropdown = creatorLinkBase.transform.Find("type").GetComponent<Dropdown>();
            CreatorLinkTypeDropdown.options = AlephNetwork.CreatorLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
            EditorThemeManager.ApplyDropdown(CreatorLinkTypeDropdown);

            CreatorLinkField = creatorLinkBase.transform.Find("input").GetComponent<InputField>();
            CreatorLinkField.textComponent.alignment = TextAnchor.MiddleLeft;
            EditorThemeManager.ApplyInputField(CreatorLinkField);

            #endregion

            #region Level

            new Labels(Labels.InitSettings.Default.Parent(Content).Rect(labelRect), new Label("Level") { fontStyle = FontStyle.Bold, });
            var level = Creator.NewUIObject("creator", Content);
            var levelLayout = level.AddComponent<VerticalLayoutGroup>();
            levelLayout.childControlHeight = false;
            levelLayout.childForceExpandHeight = false;
            levelLayout.spacing = 4f;
            level.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.MinSize;

            new Labels(Labels.InitSettings.Default.Parent(level.transform), "Difficulty");
            var difficulty = Creator.NewUIObject("difficulty", level.transform);
            var difficultyLayout = difficulty.AddComponent<HorizontalLayoutGroup>();
            difficultyLayout.childControlHeight = false;
            difficultyLayout.childControlWidth = false;
            DifficultyContent = difficultyLayout.transform.AsRT();
            var difficultyLayoutElement = difficulty.AddComponent<LayoutElement>();
            difficultyLayoutElement.minHeight = 32f;
            difficultyLayoutElement.preferredHeight = 32f;
            difficulty.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            new Labels(Labels.InitSettings.Default.Parent(level.transform), "Name");
            var levelName = EditorPrefabHolder.Instance.StringInputField.Duplicate(level.transform, "name");
            LevelNameField = levelName.GetComponent<InputField>();
            LevelNameField.textComponent.alignment = TextAnchor.MiddleLeft;
            EditorThemeManager.ApplyInputField(LevelNameField);

            new Labels(Labels.InitSettings.Default.Parent(level.transform), "Description");
            var description = EditorPrefabHolder.Instance.StringInputField.Duplicate(level.transform, "desc");
            description.transform.AsRT().sizeDelta = new Vector2(0f, 140f);
            DescriptionField = description.GetComponent<InputField>();
            DescriptionField.textComponent.alignment = TextAnchor.UpperLeft;
            DescriptionField.lineType = InputField.LineType.MultiLineNewline;
            EditorThemeManager.ApplyInputField(DescriptionField);
            var descriptionLayoutElement = description.GetComponent<LayoutElement>();
            descriptionLayoutElement.minHeight = 140f;
            descriptionLayoutElement.preferredHeight = 140f;

            new Labels(Labels.InitSettings.Default.Parent(level.transform), "Tags");
            var tagScrollView = Creator.NewUIObject("Tags Scroll View", level.transform);
            TagsScrollView = tagScrollView.transform.AsRT();
            RectValues.Default.SizeDelta(522f, 40f).AssignToRectTransform(TagsScrollView);

            var scroll = tagScrollView.AddComponent<ScrollRect>();
            scroll.scrollSensitivity = 20f;
            scroll.horizontal = true;
            scroll.vertical = false;

            var image = tagScrollView.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);

            tagScrollView.AddComponent<Mask>();

            var tagViewport = Creator.NewUIObject("Viewport", tagScrollView.transform);
            UIManager.SetRectTransform(tagViewport.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

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

            new Labels(Labels.InitSettings.Default.Parent(level.transform), "Video Link");
            var videoLinkBase = linkPrefab.Duplicate(level.transform, "video link");
            videoLinkBase.transform.AsRT().sizeDelta = new Vector2(718f, 32f);

            var openVideoLinkButton = videoLinkBase.transform.Find("open url").GetComponent<DeleteButtonStorage>();
            OpenVideoURLButton = openVideoLinkButton.button;
            EditorThemeManager.ApplyGraphic(openVideoLinkButton.baseImage, ThemeGroup.Copy, true);
            EditorThemeManager.ApplyGraphic(openVideoLinkButton.image, ThemeGroup.Copy_Text);

            VideoLinkTypeDropdown = videoLinkBase.transform.Find("type").GetComponent<Dropdown>();
            VideoLinkTypeDropdown.options = AlephNetwork.VideoLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
            EditorThemeManager.ApplyDropdown(VideoLinkTypeDropdown);

            VideoLinkField = videoLinkBase.transform.Find("input").GetComponent<InputField>();
            VideoLinkField.textComponent.alignment = TextAnchor.MiddleLeft;
            EditorThemeManager.ApplyInputField(VideoLinkField);

            new Labels(Labels.InitSettings.Default.Parent(level.transform), "Level Version");

            var version = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(level.transform);
            RectValues.Default.SizeDelta(740f, 32f).AssignToRectTransform(version.transform.AsRT());

            VersionField = version.GetComponent<InputField>();
            VersionField.GetPlaceholderText().text = "Set version...";
            VersionField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
            EditorThemeManager.ApplyInputField(VersionField);

            #endregion

            #region Icon

            new Labels(Labels.InitSettings.Default.Parent(Content).Rect(labelRect), new Label("Icon") { fontStyle = FontStyle.Bold, });
            var iconBase = Creator.NewUIObject("icon", Content);
            IconBase = iconBase.transform.AsRT();
            RectValues.Default.SizeDelta(764f, 512f).AssignToRectTransform(IconBase);

            var icon = Creator.NewUIObject("image", IconBase);
            IconImage = icon.AddComponent<Image>();
            new RectValues(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(512f, 512f)).AssignToRectTransform(IconImage.rectTransform);

            var selectIcon = EditorPrefabHolder.Instance.Function2Button.Duplicate(IconBase, "select");
            new RectValues(new Vector2(240f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(150f, 32f)).AssignToRectTransform(selectIcon.transform.AsRT());
            var selectIconStorage = selectIcon.GetComponent<FunctionButtonStorage>();
            SelectIconButton = selectIconStorage.button;
            selectIconStorage.Text = "Browse";

            EditorThemeManager.ApplySelectable(SelectIconButton, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(selectIconStorage.label, ThemeGroup.Function_2_Text);

            var collapser = EditorPrefabHolder.Instance.CollapseToggle.Duplicate(IconBase, "collapse");
            new RectValues(new Vector2(340f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(32f, 32f)).AssignToRectTransform(collapser.transform.AsRT());
            CollapseToggle = collapser.GetComponent<Toggle>();

            EditorThemeManager.ApplyToggle(CollapseToggle, ThemeGroup.Background_1);

            for (int i = 0; i < collapser.transform.Find("dots").childCount; i++)
                EditorThemeManager.ApplyGraphic(collapser.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

            #endregion

            #region Package

            new Labels(Labels.InitSettings.Default.Parent(Content).Rect(labelRect), new Label("Package (File Info)") { fontStyle = FontStyle.Bold, });
            var packageToggleGO = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Content, "package toggle");
            packageToggleGO.transform.AsRT().sizeDelta = new Vector2(764f, 32f);
            PackageToggle = packageToggleGO.GetComponent<ToggleButtonStorage>();
            PackageToggle.Text = "Show File Info";
            PackageToggle.SetIsOnWithoutNotify(false);
            EditorThemeManager.ApplyToggle(PackageToggle);

            var package = Creator.NewUIObject("package", Content);
            PackageContent = package.transform.AsRT();
            PackageContent.sizeDelta = new Vector2(764f, 0f);
            package.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.MinSize;
            var packageLayout = package.AddComponent<VerticalLayoutGroup>();
            packageLayout.childControlHeight = false;
            packageLayout.childForceExpandHeight = false;
            packageLayout.spacing = 4f;
            packageLayout.padding = new RectOffset(left: 0, right: 0, top: 0, bottom: 0);

            new LabelsElement("Main Audio").Init(EditorElement.InitSettings.Default.Parent(PackageContent));
            var audio = EditorPrefabHolder.Instance.StringInputField.Duplicate(PackageContent, "audio");
            audio.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            MainAudioField = audio.GetComponent<InputField>();
            MainAudioField.textComponent.alignment = TextAnchor.UpperLeft;
            EditorThemeManager.ApplyInputField(MainAudioField);
                
            new LabelsElement("Main Preview Audio").Init(EditorElement.InitSettings.Default.Parent(PackageContent));
            var previewAudio = EditorPrefabHolder.Instance.StringInputField.Duplicate(PackageContent, "preview audio");
            previewAudio.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            MainPreviewAudioField = previewAudio.GetComponent<InputField>();
            MainPreviewAudioField.textComponent.alignment = TextAnchor.UpperLeft;
            EditorThemeManager.ApplyInputField(MainPreviewAudioField);
                
            new LabelsElement("Main Cover").Init(EditorElement.InitSettings.Default.Parent(PackageContent));
            var cover = EditorPrefabHolder.Instance.StringInputField.Duplicate(PackageContent, "cover");
            cover.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            MainCoverField = cover.GetComponent<InputField>();
            MainCoverField.textComponent.alignment = TextAnchor.UpperLeft;
            EditorThemeManager.ApplyInputField(MainCoverField);
                
            new LabelsElement("Main Locked Cover").Init(EditorElement.InitSettings.Default.Parent(PackageContent));
            var lockedCover = EditorPrefabHolder.Instance.StringInputField.Duplicate(PackageContent, "locked cover");
            lockedCover.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            MainLockedCoverField = lockedCover.GetComponent<InputField>();
            MainLockedCoverField.textComponent.alignment = TextAnchor.UpperLeft;
            EditorThemeManager.ApplyInputField(MainLockedCoverField);
                
            new LabelsElement("Main Level").Init(EditorElement.InitSettings.Default.Parent(PackageContent));
            var mainLevel = EditorPrefabHolder.Instance.StringInputField.Duplicate(PackageContent, "level");
            mainLevel.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            MainLevelField = mainLevel.GetComponent<InputField>();
            MainLevelField.textComponent.alignment = TextAnchor.UpperLeft;
            EditorThemeManager.ApplyInputField(MainLevelField);

            new LabelsElement("Files").Init(EditorElement.InitSettings.Default.Parent(PackageContent));
            var fileScrollView = new ScrollViewElement(ScrollViewElement.Direction.Vertical);
            fileScrollView.Init(EditorElement.InitSettings.Default.Parent(PackageContent).Rect(RectValues.Default.SizeDelta(734f, 165f)));
            FileContent = fileScrollView.Content;

            #endregion

            #region Settings

            new Labels(Labels.InitSettings.Default.Parent(Content).Rect(labelRect), new Label("Settings") { fontStyle = FontStyle.Bold, });
            var settings = Creator.NewUIObject("settings", Content);
            var settingsLayout = settings.AddComponent<VerticalLayoutGroup>();
            settingsLayout.childControlHeight = false;
            settingsLayout.childForceExpandHeight = false;
            settingsLayout.spacing = 4f;
            settings.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.MinSize;

            IsHubLevelToggle = GenerateToggle(settings.transform, "Is Hub Level");
            UnlockRequiredToggle = GenerateToggle(settings.transform, "Unlock Required");
            UnlockCompletedToggle = GenerateToggle(settings.transform, "Unlock After Completion");

            new Labels(Labels.InitSettings.Default.Parent(settings.transform), new Label("Preferred Player Count"));
            PreferredPlayerCountDropdown = GenerateDropdown(settings.transform, "Preferred Player Count", CoreHelper.StringToOptionData("Any", "One", "Two", "Three", "Four", "More than four"));
            new Labels(Labels.InitSettings.Default.Parent(settings.transform), new Label("Preferred Control Type"));
            PreferredControlTypeDropdown = GenerateDropdown(settings.transform, "Preferred Control Type", CoreHelper.StringToOptionData("Any Device", "Keyboard Only", "Keyboard Extra Only", "Mouse Only", "Keyboard Mouse Only", "Controller Only"));

            HideIntroToggle = GenerateToggle(settings.transform, "Hide Intro");
            ReplayEndLevelOffToggle = GenerateToggle(settings.transform, $"Prevent \"{CoreConfig.Instance.ReplayLevel.Key}\" setting");

            RequireVersion = GenerateToggle(settings.transform, "Require Mod Version");
            new Labels(Labels.InitSettings.Default.Parent(settings.transform), new Label("Mod Version Comparison"));
            VersionComparison = GenerateDropdown(settings.transform, "Mod Version Comparison", CoreHelper.ToOptionData<DataManager.VersionComparison>());

            #endregion

            #region Server

            new Labels(Labels.InitSettings.Default.Parent(Content).Rect(labelRect), new Label("Server / Arcade") { fontStyle = FontStyle.Bold, });
            var server = Creator.NewUIObject("server", Content);
            ServerContent = server.transform.AsRT();
            var serverLayout = server.AddComponent<VerticalLayoutGroup>();
            serverLayout.childControlHeight = false;
            serverLayout.childForceExpandHeight = false;
            serverLayout.spacing = 4f;
            server.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.MinSize;

            ChangelogLabel = new Labels(Labels.InitSettings.Default.Parent(server.transform), "Changelog").GameObject;
            var changelog = EditorPrefabHolder.Instance.StringInputField.Duplicate(server.transform, "changelog");
            changelog.transform.AsRT().sizeDelta = new Vector2(0f, 140f);
            ChangelogField = changelog.GetComponent<InputField>();
            ChangelogField.textComponent.alignment = TextAnchor.UpperLeft;
            ChangelogField.lineType = InputField.LineType.MultiLineNewline;
            var changelogLayoutElement = changelog.GetComponent<LayoutElement>();
            changelogLayoutElement.minHeight = 140f;
            changelogLayoutElement.preferredHeight = 140f;
            EditorThemeManager.ApplyInputField(ChangelogField);

            new Labels(Labels.InitSettings.Default.Parent(settings.transform), new Label("Server Visibility"));
            ServerVisibilityDropdown = GenerateDropdown(server.transform, "Server Visibility", CoreHelper.ToOptionData<ServerVisibility>());

            new Labels(Labels.InitSettings.Default.Parent(server.transform), "Collaborators");
            CollaboratorsScrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(server.transform, "Collaborators Scroll View").transform.AsRT();
            CollaboratorsContent = CollaboratorsScrollView.transform.Find("Viewport/Content").AsRT();

            CollaboratorsScrollView.transform.AsRT().sizeDelta = new Vector2(735f, 200f);

            CollaboratorsContent.GetComponent<VerticalLayoutGroup>().spacing = 8f;

            var agreement = old.transform.Find("Scroll View/Viewport/Content/agreement").gameObject.Duplicate(server.transform);
            agreement.transform.AsRT().sizeDelta = new Vector2(732f, 260f);
            CoreHelper.Destroy(agreement.transform.Find("text").GetComponent<Text>(), true);
            var agreementText = agreement.transform.Find("text").gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            agreementText.alignment = TMPro.TextAlignmentOptions.Top;
            agreementText.font = FontManager.inst.allFontAssets["Inconsolata Variable"];
            agreementText.fontSize = 22;
            agreementText.enableWordWrapping = true;
            agreementText.text =
                "If your level does not use any modded features (not including alpha ported ones) and your level uses a verified song, you can convert the level to a VG level format and upload it using the modern editor.*\n" +
                "However, if your level DOES use modded features not present anywhere in vanilla or the song is not verified, then upload it to the Arcade server.\n\n" +
                "* Make sure you test the level in vanilla first!\n\n" +
                "If you want to know more, <link=\"DOC_UPLOAD_LEVEL\">check out the documentation</link>.";
            var agreementLink = agreementText.gameObject.AddComponent<OpenHyperlinks>();
            agreementLink.RegisterLink("DOC_UPLOAD_LEVEL", () => EditorDocumentation.inst.OpenDocument("Uploading a Level"));
            EditorThemeManager.ClearSelectableColors(agreementText.gameObject.AddComponent<Button>());

            var arcadeID = new Labels(Labels.InitSettings.Default.Parent(server.transform).Rect(RectValues.Default.SizeDelta(0f, 40f)), new Label("Arcade ID:") { horizontalWrap = HorizontalWrapMode.Overflow });
            arcadeID.GameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            ArcadeIDContextMenu = arcadeID.GameObject.GetOrAddComponent<ContextClickable>();
            ArcadeIDText = arcadeID.GameObject.transform.GetChild(0).GetComponent<Text>();
                
            var serverID = new Labels(Labels.InitSettings.Default.Parent(server.transform).Rect(RectValues.Default.SizeDelta(0f, 40f)), new Label("Server ID:") { horizontalWrap = HorizontalWrapMode.Overflow });
            serverID.GameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            ServerIDContextMenu = serverID.GameObject.GetOrAddComponent<ContextClickable>();
            ServerIDText = serverID.GameObject.transform.GetChild(0).GetComponent<Text>();
                
            var userID = new Labels(Labels.InitSettings.Default.Parent(server.transform).Rect(RectValues.Default.SizeDelta(0f, 40f)), new Label("User ID:") { horizontalWrap = HorizontalWrapMode.Overflow });
            userID.GameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            UserIDContextMenu = userID.GameObject.GetOrAddComponent<ContextClickable>();
            UserIDText = userID.GameObject.transform.GetChild(0).GetComponent<Text>();
                
            var moddedDisplay = new Labels(Labels.InitSettings.Default.Parent(server.transform).Rect(RectValues.Default.SizeDelta(0f, 40f)), "Modded:");
            ModdedDisplayText = moddedDisplay.GameObject.transform.GetChild(0).GetComponent<Text>();

            var buttons = Creator.NewUIObject("submit", server.transform);
            buttons.transform.AsRT().sizeDelta = new Vector2(0f, 64f);
            var buttonsLayout = buttons.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 8f;

            var convert = EditorPrefabHolder.Instance.Function1Button.Duplicate(buttons.transform, "convert");
            var convertStorage = convert.GetComponent<FunctionButtonStorage>();
            convertStorage.Text = "Convert";
            ConvertButton = convertStorage.button;
            ConvertContextMenu = convert.GetOrAddComponent<ContextClickable>();
            EditorThemeManager.ApplyGraphic(convertStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(convertStorage.label, ThemeGroup.Function_1_Text);
                
            var upload = EditorPrefabHolder.Instance.Function1Button.Duplicate(buttons.transform, "upload");
            var uploadStorage = upload.GetComponent<FunctionButtonStorage>();
            uploadStorage.Text = "Upload";
            UploadButton = uploadStorage.button;
            UploadButtonText = uploadStorage.label;
            UploadContextMenu = upload.GetOrAddComponent<ContextClickable>();
            EditorThemeManager.ApplyGraphic(uploadStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(uploadStorage.label, ThemeGroup.Function_1_Text);
                
            var pull = EditorPrefabHolder.Instance.Function1Button.Duplicate(buttons.transform, "pull");
            var pullStorage = pull.GetComponent<FunctionButtonStorage>();
            pullStorage.Text = "Pull";
            PullButton = pullStorage.button;
            PullContextMenu = pull.GetOrAddComponent<ContextClickable>();
            EditorThemeManager.ApplyGraphic(pullStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(pullStorage.label, ThemeGroup.Function_1_Text);
                
            var delete = EditorPrefabHolder.Instance.Function1Button.Duplicate(buttons.transform, "delete");
            var deleteStorage = delete.GetComponent<FunctionButtonStorage>();
            deleteStorage.Text = "Delete";
            DeleteButton = deleteStorage.button;
            DeleteContextMenu = delete.GetOrAddComponent<ContextClickable>();
            EditorThemeManager.ApplyGraphic(deleteStorage.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(deleteStorage.label, ThemeGroup.Delete_Text);

            #endregion

            Setup = true;
        }

        public void CollapseIcon(bool collapse)
        {
            var size = collapse ? 32f : 512f;
            IconImage.rectTransform.sizeDelta = new Vector2(size, size);
            IconBase.transform.AsRT().sizeDelta = new Vector2(764f, size);

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        }

        public void ShowChangelog(bool show)
        {
            ChangelogLabel.SetActive(show);
            ChangelogField.gameObject.SetActive(show);

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        }

        Dropdown GenerateDropdown(Transform parent, string name, List<Dropdown.OptionData> list)
        {
            var gameObject = EditorPrefabHolder.Instance.Dropdown.Duplicate(parent, name.ToLower());
            var dropdown = gameObject.GetComponent<Dropdown>();
            dropdown.options = list;
            EditorThemeManager.ApplyDropdown(dropdown);
            return dropdown;
        }

        ToggleButtonStorage GenerateToggle(Transform parent, string text)
        {
            var gameObject = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parent, text.ToLower());
            var toggleStorage = gameObject.GetComponent<ToggleButtonStorage>();
            toggleStorage.Text = text;
            EditorThemeManager.ApplyToggle(toggleStorage);
            return toggleStorage;
        }

        #endregion
    }
}
