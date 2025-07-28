using System;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
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

        public Image IconImage { get; set; }

        public RectTransform TagsScrollView { get; set; }

        public RectTransform TagsContent { get; set; }

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var dialog = GameObject.transform;

            Content = dialog.Find("Scroll View/Viewport/Content").AsRT();

            IconImage = Content.Find("creator/cover_art/image").GetComponent<Image>();

            RTMetaDataEditor.inst.difficultyToggle = Content.Find("song/difficulty/toggles/easy").gameObject;
            RTMetaDataEditor.inst.difficultyToggle.transform.SetParent(RTMetaDataEditor.inst.transform);
            LSHelpers.DeleteChildren(Content.Find("song/difficulty/toggles"));
            CoreHelper.Destroy(Content.Find("song/difficulty/toggles").GetComponent<ToggleGroup>());

            if (!Content.Find("artist/link/inputs/openurl"))
            {
                var openLink = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x").gameObject.Duplicate(Content.Find("artist/link/inputs"), "openurl", 0);
                openLink.transform.Find("Image").gameObject.GetComponent<Image>().sprite = EditorSprites.LinkSprite;

                var openLinkLE = openLink.AddComponent<LayoutElement>();
                var openLinkButton = openLink.GetComponent<Button>();

                openLinkLE.minWidth = 32f;
                openLinkButton.onClick.ClearAll();

                var cb = openLinkButton.colors;
                cb.normalColor = new Color(1f, 1f, 1f, 1f);
                cb.pressedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
                cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
                cb.selectedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
                openLinkButton.colors = cb;
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

            var pull = convert.Duplicate(submitBase, "pull");

            pull.transform.AsRT().anchoredPosition = new Vector2(240f, 0f);
            pull.transform.AsRT().sizeDelta = new Vector2(230f, 48f);
            var pullText = pull.transform.Find("Text").GetComponent<Text>();
            pullText.resizeTextForBestFit = false;
            pullText.fontSize = 22;
            pullText.text = "Pull Level";
            
            var delete = convert.Duplicate(submitBase, "delete");

            delete.transform.AsRT().anchoredPosition = new Vector2(240f, 0f);
            delete.transform.AsRT().sizeDelta = new Vector2(230f, 48f);
            var deleteText = delete.transform.Find("Text").GetComponent<Text>();
            deleteText.resizeTextForBestFit = false;
            deleteText.fontSize = 22;
            deleteText.text = "Delete Level";

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

            GenerateToggle(Content, creatorLinkTitle, "is hub level", "Is Hub Level", 4);
            GenerateToggle(Content, creatorLinkTitle, "unlock required", "Unlock Required", 5);
            GenerateToggle(Content, creatorLinkTitle, "unlock complete", "Unlock Completed", 6);

            GenerateDropdown(Content, creatorLinkTitle, "preferred player count", "Preferred Players", 7);
            GenerateToggle(Content, creatorLinkTitle, "hide intro", "Hide Intro", 8);
            GenerateToggle(Content, creatorLinkTitle, "replay end level off", "Replay End Level Off", 9);
            GenerateToggle(Content, creatorLinkTitle, "require version", "Require Version", 10);
            GenerateDropdown(Content, creatorLinkTitle, "version comparison", "Version Comparison", 11);

            TooltipHelper.AssignTooltip(Content.Find("require version").gameObject, "Require Version");
            TooltipHelper.AssignTooltip(Content.Find("version comparison").gameObject, "Version Comparison");

            var serverID = Content.Find("id").gameObject.Duplicate(Content, "server id", 16);
            CoreHelper.Delete(serverID.transform.GetChild(1).gameObject);

            var uploadInfo = Content.Find("creator").gameObject.Duplicate(Content, "upload", 12);

            try
            {
                CoreHelper.Delete(
                    uploadInfo.transform.Find("cover_art").gameObject,
                    uploadInfo.transform.Find("name").gameObject,
                    uploadInfo.transform.Find("link").gameObject,
                    uploadInfo.transform.Find("tags").gameObject);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            GenerateDropdown(uploadInfo.transform, creatorLinkTitle, "server visibility", "Visibility", 2);
            uploadInfo.transform.Find("description").gameObject.name = "changelog";
            uploadInfo.transform.AsRT().sizeDelta = new Vector2(738.5f, 200f);
            var uploadInfoLabel = uploadInfo.transform.Find("title/title").GetComponent<Text>();
            uploadInfoLabel.text = "Upload Info";

            var changelogLabel = uploadInfo.transform.Find("changelog/Panel/title").GetComponent<Text>();
            changelogLabel.text = "Changelog";
            EditorThemeManager.AddInputField(uploadInfo.transform.Find("changelog/input").GetComponent<InputField>());

            #region Editor Theme Setup

            EditorThemeManager.AddGraphic(convertImage, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(convertText, ThemeGroup.Function_1_Text);
            EditorThemeManager.AddGraphic(upload.GetComponent<Image>(), ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(uploadText, ThemeGroup.Function_1_Text);
            EditorThemeManager.AddGraphic(pull.GetComponent<Image>(), ThemeGroup.Delete, true);
            EditorThemeManager.AddGraphic(pullText, ThemeGroup.Delete_Text);
            EditorThemeManager.AddGraphic(delete.GetComponent<Image>(), ThemeGroup.Delete, true);
            EditorThemeManager.AddGraphic(deleteText, ThemeGroup.Delete_Text);
            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);

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

            song.AsRT().sizeDelta = new Vector2(738.5f, 134f);
            var levelName = creator.Find("name").gameObject.Duplicate(creator, "level_name", 4);
            var levelNameTitle = levelName.transform.Find("title").GetComponent<Text>();
            levelNameTitle.text = "Level Name";
            EditorThemeManager.AddLightText(levelNameTitle);
            var levelNameInput = levelName.transform.Find("input").GetComponent<InputField>();
            ((Text)levelNameInput.placeholder).text = "Level Name";
            EditorThemeManager.AddInputField(levelNameInput);

            var uploaderName = creator.Find("name").gameObject.Duplicate(creator, "uploader_name", 1);
            var uploaderNameTitle = uploaderName.transform.Find("title").GetComponent<Text>();
            uploaderNameTitle.text = "Uploader Name";
            EditorThemeManager.AddLightText(uploaderNameTitle);
            var uploaderNameInput = uploaderName.transform.Find("input").GetComponent<InputField>();
            ((Text)uploaderNameInput.placeholder).text = "Uploader Name";
            EditorThemeManager.AddInputField(uploaderNameInput);

            #endregion
        }

        void GenerateDropdown(Transform content, Text creatorLinkTitle, string name, string text, int siblingIndex)
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

            var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(dropdownBase.transform, "dropdown");
            var layoutElement = dropdown.GetComponent<LayoutElement>() ?? dropdown.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 126f;
            layoutElement.minWidth = 126f;
            dropdown.transform.AsRT().sizeDelta = new Vector2(256f, 32f);
            EditorThemeManager.AddDropdown(dropdown.GetComponent<Dropdown>());
        }

        void GenerateToggle(Transform content, Text creatorLinkTitle, string name, string text, int siblingIndex)
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

            var toggle = EditorPrefabHolder.Instance.Toggle.Duplicate(toggleBase.transform, "toggle");
            var layoutElement = toggle.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 32f;
            layoutElement.minWidth = 32f;
            EditorThemeManager.AddToggle(toggle.GetComponent<Toggle>());
        }
    }
}
