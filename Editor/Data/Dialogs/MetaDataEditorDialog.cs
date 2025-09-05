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
    public class MetaDataEditorDialog : EditorDialog
    {
        public MetaDataEditorDialog() : base(METADATA_EDITOR) { }

        public RectTransform Content { get; set; }

        public bool Setup { get; set; } = false;

        bool rework = true;

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

        #region Settings

        public Toggle IsHubLevelToggle { get; set; }
        public Toggle UnlockRequiredToggle { get; set; }
        public Toggle UnlockCompletedToggle { get; set; }

        public Dropdown PreferredPlayerCountDropdown { get; set; }
        public Dropdown PreferredControlTypeDropdown { get; set; }

        public Toggle HideIntroToggle { get; set; }

        public Toggle ReplayEndLevelOffToggle { get; set; }

        #endregion

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

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var old = GameObject.Duplicate(EditorManager.inst.dialogs, Name + " Old");

            RTMetaDataEditor.inst.difficultyToggle = old.transform.Find("Scroll View/Viewport/Content/song/difficulty/toggles/easy").gameObject;
            RTMetaDataEditor.inst.difficultyToggle.transform.SetParent(RTMetaDataEditor.inst.transform);

            if (rework)
            {
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

                EditorThemeManager.AddGraphic(GameObject.GetComponent<Image>(), ThemeGroup.Background_1);

                var labelRect = new RectValues(new Vector2(16f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, -32f));

                #region Prefabs

                linkPrefab = Creator.NewUIObject("link", RTMetaDataEditor.inst.transform);
                var artistLinkLayout = linkPrefab.AddComponent<HorizontalLayoutGroup>();
                artistLinkLayout.spacing = 4f;

                var openLink = EditorPrefabHolder.Instance.DeleteButton.Duplicate(linkPrefab.transform, "open url");
                var openLinkLayoutElement = openLink.GetComponent<LayoutElement>();
                openLinkLayoutElement.ignoreLayout = false;
                openLinkLayoutElement.minWidth = 32f;
                openLinkLayoutElement.preferredWidth = 32f;
                var openLinkStorage = openLink.GetComponent<DeleteButtonStorage>();
                openLinkStorage.image.sprite = EditorSprites.LinkSprite;

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

                var artistBase = Creator.NewUIObject("artist", Content);
                RectValues.Default.SizeDelta(764f, 200f).AssignToRectTransform(artistBase.transform.AsRT());
                new Labels(Labels.InitSettings.Default.Parent(artistBase.transform).Rect(labelRect), new Label("Artist") { fontStyle = FontStyle.Bold, });
                var artist = Creator.NewUIObject("info", artistBase.transform);
                RectValues.FullAnchored.AnchorMax(1f, 0f).Pivot(0.5f, 0f).SizeDelta(-32f, 140f).AssignToRectTransform(artist.transform.AsRT());
                var artistLayout = artist.AddComponent<VerticalLayoutGroup>();
                artistLayout.spacing = 4f;

                new Labels(Labels.InitSettings.Default.Parent(artist.transform), "Name");
                var artistName = EditorPrefabHolder.Instance.StringInputField.Duplicate(artist.transform, "name");
                ArtistNameField = artistName.GetComponent<InputField>();
                ArtistNameField.textComponent.alignment = TextAnchor.MiddleLeft;
                EditorThemeManager.AddInputField(ArtistNameField);

                new Labels(Labels.InitSettings.Default.Parent(artist.transform), "Link");
                var artistLinkBase = linkPrefab.Duplicate(artist.transform, "link");

                var openArtistLinkButton = artistLinkBase.transform.Find("open url").GetComponent<DeleteButtonStorage>();
                OpenArtistURLButton = openArtistLinkButton.button;
                EditorThemeManager.AddGraphic(openArtistLinkButton.baseImage, ThemeGroup.Copy, true);
                EditorThemeManager.AddGraphic(openArtistLinkButton.image, ThemeGroup.Copy_Text);

                ArtistLinkTypeDropdown = artistLinkBase.transform.Find("type").GetComponent<Dropdown>();
                ArtistLinkTypeDropdown.options = AlephNetwork.ArtistLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
                EditorThemeManager.AddDropdown(ArtistLinkTypeDropdown);

                ArtistLinkField = artistLinkBase.transform.Find("input").GetComponent<InputField>();
                ArtistLinkField.textComponent.alignment = TextAnchor.MiddleLeft;
                EditorThemeManager.AddInputField(ArtistLinkField);

                #endregion

                #region Song

                var songBase = Creator.NewUIObject("song", Content);
                RectValues.Default.SizeDelta(764f, 200f).AssignToRectTransform(songBase.transform.AsRT());
                new Labels(Labels.InitSettings.Default.Parent(songBase.transform).Rect(labelRect), new Label("Song") { fontStyle = FontStyle.Bold, });
                var song = Creator.NewUIObject("info", songBase.transform);
                RectValues.FullAnchored.AnchorMax(1f, 0f).Pivot(0.5f, 0f).SizeDelta(-32f, 140f).AssignToRectTransform(song.transform.AsRT());
                var songLayout = song.AddComponent<VerticalLayoutGroup>();
                songLayout.spacing = 4f;

                new Labels(Labels.InitSettings.Default.Parent(song.transform), "Title");
                var songTitle = EditorPrefabHolder.Instance.StringInputField.Duplicate(song.transform, "title");
                SongTitleField = songTitle.GetComponent<InputField>();
                SongTitleField.textComponent.alignment = TextAnchor.MiddleLeft;
                EditorThemeManager.AddInputField(SongTitleField);

                new Labels(Labels.InitSettings.Default.Parent(song.transform), "Link");
                var songLinkBase = linkPrefab.Duplicate(song.transform, "link");

                var openSongLinkButton = songLinkBase.transform.Find("open url").GetComponent<DeleteButtonStorage>();
                OpenSongURLButton = openSongLinkButton.button;
                EditorThemeManager.AddGraphic(openSongLinkButton.baseImage, ThemeGroup.Copy, true);
                EditorThemeManager.AddGraphic(openSongLinkButton.image, ThemeGroup.Copy_Text);

                SongLinkTypeDropdown = songLinkBase.transform.Find("type").GetComponent<Dropdown>();
                SongLinkTypeDropdown.options = AlephNetwork.SongLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
                EditorThemeManager.AddDropdown(SongLinkTypeDropdown);

                SongLinkField = songLinkBase.transform.Find("input").GetComponent<InputField>();
                SongLinkField.textComponent.alignment = TextAnchor.MiddleLeft;
                EditorThemeManager.AddInputField(SongLinkField);

                #endregion

                #region Creator

                var creatorBase = Creator.NewUIObject("creator", Content);
                RectValues.Default.SizeDelta(764f, 200f).AssignToRectTransform(creatorBase.transform.AsRT());
                new Labels(Labels.InitSettings.Default.Parent(creatorBase.transform).Rect(labelRect), new Label("Creator") { fontStyle = FontStyle.Bold, });
                var creator = Creator.NewUIObject("info", creatorBase.transform);
                RectValues.FullAnchored.AnchorMax(1f, 0f).Pivot(0.5f, 0f).SizeDelta(-32f, 140f).AssignToRectTransform(creator.transform.AsRT());
                var creatorLayout = creator.AddComponent<VerticalLayoutGroup>();
                creatorLayout.spacing = 4f;

                new Labels(Labels.InitSettings.Default.Parent(creator.transform), "Name");
                var creatorName = EditorPrefabHolder.Instance.StringInputField.Duplicate(creator.transform, "name");
                CreatorNameField = creatorName.GetComponent<InputField>();
                CreatorNameField.textComponent.alignment = TextAnchor.MiddleLeft;
                EditorThemeManager.AddInputField(CreatorNameField);

                new Labels(Labels.InitSettings.Default.Parent(creator.transform), "Link");
                var creatorLinkBase = linkPrefab.Duplicate(creator.transform, "link");

                var openCreatorLinkButton = creatorLinkBase.transform.Find("open url").GetComponent<DeleteButtonStorage>();
                OpenCreatorURLButton = openCreatorLinkButton.button;
                EditorThemeManager.AddGraphic(openCreatorLinkButton.baseImage, ThemeGroup.Copy, true);
                EditorThemeManager.AddGraphic(openCreatorLinkButton.image, ThemeGroup.Copy_Text);

                CreatorLinkTypeDropdown = creatorLinkBase.transform.Find("type").GetComponent<Dropdown>();
                CreatorLinkTypeDropdown.options = AlephNetwork.CreatorLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
                EditorThemeManager.AddDropdown(CreatorLinkTypeDropdown);

                CreatorLinkField = creatorLinkBase.transform.Find("input").GetComponent<InputField>();
                CreatorLinkField.textComponent.alignment = TextAnchor.MiddleLeft;
                EditorThemeManager.AddInputField(CreatorLinkField);

                #endregion

                #region Level

                var levelBase = Creator.NewUIObject("level", Content);
                RectValues.Default.SizeDelta(764f, 520f).AssignToRectTransform(levelBase.transform.AsRT());
                new Labels(Labels.InitSettings.Default.Parent(levelBase.transform).Rect(labelRect), new Label("Level") { fontStyle = FontStyle.Bold, });
                var level = Creator.NewUIObject("info", levelBase.transform);
                RectValues.FullAnchored.AnchorMax(1f, 0f).Pivot(0.5f, 0f).SizeDelta(-32f, 460f).AssignToRectTransform(level.transform.AsRT());
                var levelLayout = level.AddComponent<VerticalLayoutGroup>();
                levelLayout.childControlHeight = false;
                levelLayout.childForceExpandHeight = false;
                levelLayout.spacing = 4f;

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
                EditorThemeManager.AddInputField(LevelNameField);

                new Labels(Labels.InitSettings.Default.Parent(level.transform), "Description");
                var description = EditorPrefabHolder.Instance.StringInputField.Duplicate(level.transform, "desc");
                description.transform.AsRT().sizeDelta = new Vector2(0f, 140f);
                DescriptionField = description.GetComponent<InputField>();
                DescriptionField.textComponent.alignment = TextAnchor.UpperLeft;
                DescriptionField.lineType = InputField.LineType.MultiLineNewline;
                EditorThemeManager.AddInputField(DescriptionField);
                var descriptionLayoutElement = description.GetComponent<LayoutElement>();
                descriptionLayoutElement.minHeight = 140f;
                descriptionLayoutElement.preferredHeight = 140f;

                new Labels(Labels.InitSettings.Default.Parent(level.transform), "Tags");
                var tagScrollView = Creator.NewUIObject("Tags Scroll View", level.transform);
                TagsScrollView = tagScrollView.transform.AsRT();
                RectValues.Default.SizeDelta(522f, 40f).AssignToRectTransform(TagsScrollView);

                var scroll = tagScrollView.AddComponent<ScrollRect>();
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
                EditorThemeManager.AddGraphic(openVideoLinkButton.baseImage, ThemeGroup.Copy, true);
                EditorThemeManager.AddGraphic(openVideoLinkButton.image, ThemeGroup.Copy_Text);

                VideoLinkTypeDropdown = videoLinkBase.transform.Find("type").GetComponent<Dropdown>();
                VideoLinkTypeDropdown.options = AlephNetwork.VideoLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
                EditorThemeManager.AddDropdown(VideoLinkTypeDropdown);

                VideoLinkField = videoLinkBase.transform.Find("input").GetComponent<InputField>();
                VideoLinkField.textComponent.alignment = TextAnchor.MiddleLeft;
                EditorThemeManager.AddInputField(VideoLinkField);

                new Labels(Labels.InitSettings.Default.Parent(level.transform), "Level Version");

                var version = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(level.transform);
                RectValues.Default.SizeDelta(740f, 32f).AssignToRectTransform(version.transform.AsRT());

                VersionField = version.GetComponent<InputField>();
                VersionField.GetPlaceholderText().text = "Set version...";
                VersionField.GetPlaceholderText().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
                EditorThemeManager.AddInputField(VersionField);

                #endregion

                #region Icon

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

                #region Settings

                var settingsBase = Creator.NewUIObject("settings", Content);
                RectValues.Default.SizeDelta(764f, 420f).AssignToRectTransform(settingsBase.transform.AsRT());
                new Labels(Labels.InitSettings.Default.Parent(settingsBase.transform).Rect(labelRect), new Label("Settings") { fontStyle = FontStyle.Bold, });
                var settings = Creator.NewUIObject("info", settingsBase.transform);
                RectValues.FullAnchored.AnchorMax(1f, 0f).Pivot(0.5f, 0f).SizeDelta(-32f, 360f).AssignToRectTransform(settings.transform.AsRT());
                var settingsLayout = settings.AddComponent<VerticalLayoutGroup>();
                settingsLayout.childControlHeight = false;
                settingsLayout.childForceExpandHeight = false;
                settingsLayout.spacing = 4f;

                IsHubLevelToggle = GenerateToggle(settings.transform, "Is Hub Level");
                UnlockRequiredToggle = GenerateToggle(settings.transform, "Unlock Required");
                UnlockCompletedToggle = GenerateToggle(settings.transform, "Unlock After Completion");

                PreferredPlayerCountDropdown = GenerateDropdown(settings.transform, "Preferred Player Count", true, CoreHelper.StringToOptionData("Any", "One", "Two", "Three", "Four", "More than four"));
                PreferredControlTypeDropdown = GenerateDropdown(settings.transform, "Preferred Control Type", true, CoreHelper.StringToOptionData("Any Device", "Keyboard Only", "Keyboard Extra Only", "Mouse Only", "Keyboard Mouse Only", "Controller Only"));

                HideIntroToggle = GenerateToggle(settings.transform, "Hide Intro");
                ReplayEndLevelOffToggle = GenerateToggle(settings.transform, $"Prevent \"{CoreConfig.Instance.ReplayLevel.Key}\" setting");

                RequireVersion = GenerateToggle(settings.transform, "Require Mod Version");
                VersionComparison = GenerateDropdown(settings.transform, "Mod Version Comparison", true, CoreHelper.ToOptionData<DataManager.VersionComparison>());

                #endregion

                #region Server

                var serverBase = Creator.NewUIObject("server", Content);
                ServerBase = serverBase.transform.AsRT();
                RectValues.Default.SizeDelta(764f, 800f).AssignToRectTransform(ServerBase);
                new Labels(Labels.InitSettings.Default.Parent(serverBase.transform).Rect(labelRect), new Label("Server / Arcade") { fontStyle = FontStyle.Bold, });
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
                convertStorage.label.text = "Convert";
                ConvertButton = convertStorage.button;
                ConvertContextMenu = convert.GetOrAddComponent<ContextClickable>();
                EditorThemeManager.AddGraphic(convertStorage.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(convertStorage.label, ThemeGroup.Function_1_Text);
                
                var upload = EditorPrefabHolder.Instance.Function1Button.Duplicate(buttons.transform, "upload");
                var uploadStorage = upload.GetComponent<FunctionButtonStorage>();
                uploadStorage.label.text = "Upload";
                UploadButton = uploadStorage.button;
                UploadButtonText = uploadStorage.label;
                UploadContextMenu = upload.GetOrAddComponent<ContextClickable>();
                EditorThemeManager.AddGraphic(uploadStorage.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(uploadStorage.label, ThemeGroup.Function_1_Text);
                
                var pull = EditorPrefabHolder.Instance.Function1Button.Duplicate(buttons.transform, "pull");
                var pullStorage = pull.GetComponent<FunctionButtonStorage>();
                pullStorage.label.text = "Pull";
                PullButton = pullStorage.button;
                PullContextMenu = pull.GetOrAddComponent<ContextClickable>();
                EditorThemeManager.AddGraphic(pullStorage.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(pullStorage.label, ThemeGroup.Function_1_Text);
                
                var delete = EditorPrefabHolder.Instance.Function1Button.Duplicate(buttons.transform, "delete");
                var deleteStorage = delete.GetComponent<FunctionButtonStorage>();
                deleteStorage.label.text = "Delete";
                DeleteButton = deleteStorage.button;
                DeleteContextMenu = delete.GetOrAddComponent<ContextClickable>();
                EditorThemeManager.AddGraphic(deleteStorage.button.image, ThemeGroup.Delete, true);
                EditorThemeManager.AddGraphic(deleteStorage.label, ThemeGroup.Delete_Text);

                #endregion

                Setup = true;
            }
            else
            {
                Setup = true;

                var dialog = GameObject.transform;

                Content = dialog.Find("Scroll View/Viewport/Content").AsRT();

                IconImage = Content.Find("creator/cover_art/image").GetComponent<Image>();

                DifficultyContent = Content.Find("song/difficulty/toggles").AsRT();
                LSHelpers.DeleteChildren(DifficultyContent);
                CoreHelper.Destroy(DifficultyContent.GetComponent<ToggleGroup>());

                if (!Content.Find("artist/link/inputs/openurl"))
                {
                    var openLink = EditorPrefabHolder.Instance.DeleteButton.Duplicate(Content.Find("artist/link/inputs"), "openurl", 0);
                    var storage = openLink.GetComponent<DeleteButtonStorage>();
                    storage.image.sprite = EditorSprites.LinkSprite;

                    var openLinkLE = openLink.AddComponent<LayoutElement>();

                    openLinkLE.minWidth = 32f;
                    storage.button.onClick.ClearAll();

                    var cb = storage.button.colors;
                    cb.normalColor = new Color(1f, 1f, 1f, 1f);
                    cb.pressedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
                    cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
                    cb.selectedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
                    storage.button.colors = cb;
                }

                if (!Content.Find("creator/link"))
                    Content.Find("artist/link").gameObject.Duplicate(Content.Find("creator"), "link", 3);

                if (!Content.Find("song/link"))
                    Content.Find("artist/link").gameObject.Duplicate(Content.Find("song"), "link", 2);

                // Tag Scroll View
                {
                    var tagParent = Content.Find("creator/description (1)");
                    tagParent.name = "tags";
                    tagParent.gameObject.SetActive(true);
                    CoreHelper.Delete(tagParent.Find("input").gameObject);

                    tagParent.AsRT().sizeDelta = new Vector2(757f, 32f);

                    tagParent.Find("Panel/title").GetComponent<Text>().text = "Tags";

                    var tagScrollView = Creator.NewUIObject("Scroll View", tagParent);
                    TagsScrollView = tagScrollView.transform.AsRT();
                    TagsScrollView.sizeDelta = new Vector2(522f, 40f);

                    var scroll = tagScrollView.AddComponent<ScrollRect>();
                    scroll.horizontal = true;
                    scroll.vertical = false;

                    var image = tagScrollView.AddComponent<Image>();
                    image.color = new Color(1f, 1f, 1f, 0.01f);

                    tagScrollView.AddComponent<Mask>();

                    var tagViewport = Creator.NewUIObject("Viewport", tagScrollView.transform);
                    UIManager.SetRectTransform(tagViewport.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

                    var tagContent = Creator.NewUIObject("Content", tagViewport.transform);
                    TagsContent = tagContent.transform.AsRT();
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
                }

                var submitBase = Content.Find("submit");
                var submitLayout = submitBase.gameObject.AddComponent<HorizontalLayoutGroup>();
                submitLayout.spacing = 8f;
                var convert = submitBase.Find("submit").gameObject;
                convert.name = "convert";

                var convertImage = convert.GetComponent<Image>();
                convertImage.sprite = null;

                convert.transform.AsRT().anchoredPosition = new Vector2(-240f, 0f);
                convert.transform.AsRT().sizeDelta = new Vector2(190f, 48f);
                var convertText = convert.transform.Find("Text").GetComponent<Text>();
                convertText.resizeTextForBestFit = false;
                convertText.fontSize = 18;
                convertText.text = "Convert to VG Format";
                ConvertButton = convert.GetComponent<Button>();
                ConvertContextMenu = convert.GetOrAddComponent<ContextClickable>();

                var upload = convert.Duplicate(submitBase, "upload");

                upload.transform.AsRT().anchoredPosition = new Vector2(0f, 0f);
                upload.transform.AsRT().sizeDelta = new Vector2(230f, 48f);
                var uploadText = upload.transform.Find("Text").GetComponent<Text>();
                uploadText.resizeTextForBestFit = false;
                uploadText.fontSize = 22;
                uploadText.text = "Upload to Server";

                TooltipHelper.AssignTooltip(upload, "Upload Level");
                var uploadContextMenu = upload.AddComponent<ContextClickable>();
                uploadContextMenu.onClick = eventData =>
                {
                    if (eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Upload / Update", RTMetaDataEditor.inst.UploadLevel),
                        new ButtonFunction("Verify Level is on Server", () => RTEditor.inst.ShowWarningPopup("Do you want to verify that the level is on the Arcade server?", () =>
                        {
                            RTEditor.inst.HideWarningPopup();
                            EditorManager.inst.DisplayNotification("Verifying...", 1.5f, EditorManager.NotificationType.Info);
                            RTMetaDataEditor.inst.VerifyLevelIsOnServer();
                        }, RTEditor.inst.HideWarningPopup)),
                        new ButtonFunction("Pull Changes from Server", () => RTEditor.inst.ShowWarningPopup("Do you want to pull the level from the Arcade server?", () =>
                        {
                            RTEditor.inst.HideWarningPopup();
                            EditorManager.inst.DisplayNotification("Pulling level...", 1.5f, EditorManager.NotificationType.Info);
                            RTMetaDataEditor.inst.PullLevel();
                        }, RTEditor.inst.HideWarningPopup)),
                        new ButtonFunction(true),
                        new ButtonFunction("Guidelines", () => EditorDocumentation.inst.OpenDocument("Uploading a Level"))
                        );
                };
                UploadButton = upload.GetComponent<Button>();
                UploadContextMenu = upload.GetOrAddComponent<ContextClickable>();
                UploadButtonText = uploadText;

                var pull = convert.Duplicate(submitBase, "pull");

                pull.transform.AsRT().anchoredPosition = new Vector2(240f, 0f);
                pull.transform.AsRT().sizeDelta = new Vector2(230f, 48f);
                var pullText = pull.transform.Find("Text").GetComponent<Text>();
                pullText.resizeTextForBestFit = false;
                pullText.fontSize = 22;
                pullText.text = "Pull Level";
                PullButton = pull.GetComponent<Button>();
                PullContextMenu = pull.GetOrAddComponent<ContextClickable>();

                var delete = convert.Duplicate(submitBase, "delete");

                delete.transform.AsRT().anchoredPosition = new Vector2(240f, 0f);
                delete.transform.AsRT().sizeDelta = new Vector2(230f, 48f);
                var deleteText = delete.transform.Find("Text").GetComponent<Text>();
                deleteText.resizeTextForBestFit = false;
                deleteText.fontSize = 22;
                deleteText.text = "Delete Level";
                DeleteButton = delete.GetComponent<Button>();
                DeleteContextMenu = delete.GetOrAddComponent<ContextClickable>();

                Content.Find("id").gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

                var artist = Content.Find("artist");
                var song = Content.Find("song");
                var creator = Content.Find("creator");

                var creatorLinkTitle = creator.Find("link/title").GetComponent<Text>();
                creatorLinkTitle.text = "Creator Link";

                Content.Find("spacer (1)").AsRT().sizeDelta = new Vector2(732f, 20f);

                Content.Find("creator/description").AsRT().sizeDelta = new Vector2(757f, 140f);
                Content.Find("creator/description/input").AsRT().sizeDelta = new Vector2(523f, 140f);
                Content.Find("agreement").AsRT().sizeDelta = new Vector2(732f, 260f);
                CoreHelper.Destroy(true, Content.Find("agreement/text").GetComponent<Text>());
                var agreementText = Content.Find("agreement/text").gameObject.AddComponent<TMPro.TextMeshProUGUI>();
                agreementText.alignment = TMPro.TextAlignmentOptions.Top;
                agreementText.font = FontManager.inst.allFontAssets["Inconsolata Variable"];
                agreementText.fontSize = 22;
                agreementText.enableWordWrapping = true;
                agreementText.text =
                    "If your level does not use any modded features (not including alpha ported ones) and your level uses a verified song, you can convert the level to an alpha level format and upload it using the alpha editor.*\n" +
                    "However, if your level DOES use modded features not present anywhere in vanilla or the song is not verified, then upload it to the Arcade server.\n\n" +
                    "* Make sure you test the level in vanilla first!\n\n" +
                    "If you want to know more, <link=\"DOC_UPLOAD_LEVEL\">check out the documentation</link>.";
                var agreementLink = agreementText.gameObject.AddComponent<OpenHyperlinks>();
                agreementLink.RegisterLink("DOC_UPLOAD_LEVEL", () => EditorDocumentation.inst.OpenDocument("Uploading a Level"));
                EditorThemeManager.ClearSelectableColors(agreementText.gameObject.AddComponent<Button>());

                Content.Find("id/revisions").gameObject.SetActive(true);

                Content.Find("spacer").gameObject.SetActive(true);
                submitBase.gameObject.SetActive(true);

                Creator.NewUIObject("spacer toggles", Content, 3).transform.AsRT().sizeDelta = new Vector2(0f, 80f);

                IsHubLevelToggle = GenerateToggle(Content, creatorLinkTitle, "is hub level", "Is Hub Level", 4);
                UnlockRequiredToggle = GenerateToggle(Content, creatorLinkTitle, "unlock required", "Unlock Required", 5);
                UnlockCompletedToggle = GenerateToggle(Content, creatorLinkTitle, "unlock complete", "Unlock Completed", 6);

                PreferredPlayerCountDropdown = GenerateDropdown(Content, creatorLinkTitle, "preferred player count", "Preferred Players", 7);
                HideIntroToggle = GenerateToggle(Content, creatorLinkTitle, "hide intro", "Hide Intro", 8);
                ReplayEndLevelOffToggle = GenerateToggle(Content, creatorLinkTitle, "replay end level off", "Replay End Level Off", 9);
                RequireVersion = GenerateToggle(Content, creatorLinkTitle, "require version", "Require Version", 10);
                VersionComparison = GenerateDropdown(Content, creatorLinkTitle, "version comparison", "Version Comparison", 11);

                TooltipHelper.AssignTooltip(Content.Find("require version").gameObject, "Require Version");
                TooltipHelper.AssignTooltip(Content.Find("version comparison").gameObject, "Version Comparison");

                ArcadeIDText = Content.Find("id/id").GetComponent<Text>();
                ArcadeIDContextMenu = Content.Find("id").gameObject.GetOrAddComponent<ContextClickable>();

                var serverID = Content.Find("id").gameObject.Duplicate(Content, "server id", 16);
                CoreHelper.Delete(serverID.transform.GetChild(1).gameObject);
                ServerIDText = serverID.transform.Find("id").GetComponent<Text>();
                ServerIDContextMenu = serverID.GetOrAddComponent<ContextClickable>();

                var uploaderID = Content.Find("id").gameObject.Duplicate(Content, "uploader id", 17);
                CoreHelper.Delete(uploaderID.transform.GetChild(1).gameObject);
                UserIDText = uploaderID.transform.Find("id").GetComponent<Text>();
                UserIDContextMenu = uploaderID.GetOrAddComponent<ContextClickable>();

                var uploadInfo = Content.Find("creator").gameObject.Duplicate(Content, "upload", 12);
                ServerParent = uploadInfo.transform.AsRT();

                try
                {
                    CoreHelper.Delete(
                        ServerParent.Find("cover_art").gameObject,
                        ServerParent.Find("name").gameObject,
                        ServerParent.Find("link").gameObject,
                        ServerParent.Find("tags").gameObject);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }

                ServerVisibilityDropdown = GenerateDropdown(uploadInfo.transform, creatorLinkTitle, "server visibility", "Visibility", 2);
                Changelog = ServerParent.Find("description").gameObject;
                Changelog.name = "changelog";
                ServerParent.sizeDelta = new Vector2(738.5f, 200f);
                var uploadInfoLabel = ServerParent.Find("title/title").GetComponent<Text>();
                uploadInfoLabel.text = "Upload Info";

                var changelogLabel = Changelog.transform.Find("Panel/title").GetComponent<Text>();
                changelogLabel.text = "Changelog";
                ChangelogField = Changelog.transform.Find("input").GetComponent<InputField>();
                EditorThemeManager.AddInputField(ChangelogField);

                ModdedDisplayText = Content.Find("id/revisions").GetComponent<Text>();

                song.AsRT().sizeDelta = new Vector2(738.5f, 134f);
                var levelName = creator.Find("name").gameObject.Duplicate(creator, "level_name", 4);
                var levelNameTitle = levelName.transform.Find("title").GetComponent<Text>();
                levelNameTitle.text = "Level Name";
                EditorThemeManager.AddLightText(levelNameTitle);
                LevelNameField = levelName.transform.Find("input").GetComponent<InputField>();
                LevelNameField.GetPlaceholderText().text = "Level Name";
                EditorThemeManager.AddInputField(LevelNameField);

                var uploaderName = creator.Find("name").gameObject.Duplicate(creator, "uploader_name", 1);
                var uploaderNameTitle = uploaderName.transform.Find("title").GetComponent<Text>();
                uploaderNameTitle.text = "Uploader Name";
                EditorThemeManager.AddLightText(uploaderNameTitle);
                UploaderNameField = uploaderName.transform.Find("input").GetComponent<InputField>();
                UploaderNameField.GetPlaceholderText().text = "Uploader Name";
                EditorThemeManager.AddInputField(UploaderNameField);

                #region Cache

                OpenArtistURLButton = Content.Find("artist/link/inputs/openurl").GetComponent<Button>();
                ArtistNameField = Content.Find("artist/name/input").GetComponent<InputField>();
                ArtistLinkField = Content.Find("artist/link/inputs/input").GetComponent<InputField>();
                ArtistLinkTypeDropdown = Content.Find("artist/link/inputs/dropdown").GetComponent<Dropdown>();

                OpenSongURLButton = Content.Find("song/link/inputs/openurl").GetComponent<Button>();
                SongTitleField = Content.Find("song/title/input").GetComponent<InputField>();
                SongLinkField = Content.Find("song/link/inputs/input").GetComponent<InputField>();
                SongLinkTypeDropdown = Content.Find("song/link/inputs/dropdown").GetComponent<Dropdown>();

                DescriptionField = Content.Find("creator/description/input").GetComponent<InputField>();

                CreatorNameField = Content.Find("creator/name/input").GetComponent<InputField>();
                OpenCreatorURLButton = Content.Find("creator/link/inputs/openurl").GetComponent<Button>();
                CreatorLinkField = Content.Find("creator/link/inputs/input").GetComponent<InputField>();
                CreatorLinkTypeDropdown = Content.Find("creator/link/inputs/dropdown").GetComponent<Dropdown>();

                #endregion

                #region Editor Theme Setup

                EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);
                EditorThemeManager.AddGraphic(convertImage, ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(convertText, ThemeGroup.Function_1_Text);
                EditorThemeManager.AddGraphic(upload.GetComponent<Image>(), ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(uploadText, ThemeGroup.Function_1_Text);
                EditorThemeManager.AddGraphic(pull.GetComponent<Image>(), ThemeGroup.Delete, true);
                EditorThemeManager.AddGraphic(pullText, ThemeGroup.Delete_Text);
                EditorThemeManager.AddGraphic(delete.GetComponent<Image>(), ThemeGroup.Delete, true);
                EditorThemeManager.AddGraphic(deleteText, ThemeGroup.Delete_Text);

                EditorThemeManager.AddLightText(uploadInfoLabel);
                EditorThemeManager.AddLightText(changelogLabel);
                EditorThemeManager.AddLightText(artist.Find("title/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(song.Find("title_/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(creator.Find("title/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(artist.Find("name/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(artist.Find("link/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(song.Find("title/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(song.Find("difficulty/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(creator.Find("cover_art/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(creator.Find("cover_art/title (1)").GetComponent<Text>());
                EditorThemeManager.AddLightText(creator.Find("name/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(creatorLinkTitle);
                EditorThemeManager.AddLightText(creator.Find("description/Panel/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(creator.Find("tags/Panel/title").GetComponent<Text>());
                EditorThemeManager.AddLightText(agreementText);
                EditorThemeManager.AddLightText(Content.Find("id/id").GetComponent<Text>());
                EditorThemeManager.AddLightText(Content.Find("id/revisions").GetComponent<Text>());
                EditorThemeManager.AddLightText(serverID.transform.Find("id").GetComponent<Text>());

                EditorThemeManager.AddInputField(artist.Find("name/input").GetComponent<InputField>());

                EditorThemeManager.AddGraphic(artist.Find("link/inputs/openurl").GetComponent<Image>(), ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(artist.Find("link/inputs/openurl/Image").GetComponent<Image>(), ThemeGroup.Function_1_Text);

                EditorThemeManager.AddInputField(artist.Find("link/inputs/input").GetComponent<InputField>());

                EditorThemeManager.AddDropdown(artist.Find("link/inputs/dropdown").GetComponent<Dropdown>());

                EditorThemeManager.AddInputField(song.Find("title/input").GetComponent<InputField>());

                EditorThemeManager.AddGraphic(creator.Find("cover_art/browse").GetComponent<Image>(), ThemeGroup.Function_2_Normal, true);
                EditorThemeManager.AddGraphic(creator.Find("cover_art/browse/Text").GetComponent<Text>(), ThemeGroup.Function_2_Text);

                EditorThemeManager.AddInputField(creator.Find("name/input").GetComponent<InputField>());

                EditorThemeManager.AddGraphic(creator.Find("link/inputs/openurl").GetComponent<Image>(), ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(creator.Find("link/inputs/openurl/Image").GetComponent<Image>(), ThemeGroup.Function_1_Text);

                EditorThemeManager.AddInputField(creator.Find("link/inputs/input").GetComponent<InputField>());

                EditorThemeManager.AddDropdown(creator.Find("link/inputs/dropdown").GetComponent<Dropdown>());

                EditorThemeManager.AddGraphic(song.Find("link/inputs/openurl").GetComponent<Image>(), ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(song.Find("link/inputs/openurl/Image").GetComponent<Image>(), ThemeGroup.Function_1_Text);

                EditorThemeManager.AddInputField(song.Find("link/inputs/input").GetComponent<InputField>());

                EditorThemeManager.AddDropdown(song.Find("link/inputs/dropdown").GetComponent<Dropdown>());

                EditorThemeManager.AddInputField(creator.Find("description/input").GetComponent<InputField>());

                #endregion
            }
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
