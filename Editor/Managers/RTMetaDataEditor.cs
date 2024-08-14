using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Components;
using BetterLegacy.Configs;
using System.IO.Compression;
using System.Net;
using UnityEngine.Networking;
using HarmonyLib;

namespace BetterLegacy.Editor.Managers
{
    public class RTMetaDataEditor : MonoBehaviour
    {
        public static RTMetaDataEditor inst;

        #region Variables

        GameObject difficultyToggle;

        JSONObject authData;

        HttpListener _listener;

        #endregion

        public static void Init() => MetadataEditor.inst?.gameObject?.AddComponent<RTMetaDataEditor>();

        void Awake()
        {
            inst = this;
            StartCoroutine(SetupUI());
        }

        /// <summary>
        /// Sets up the MetaData Editor UI.
        /// </summary>
        /// <returns></returns>
        IEnumerator SetupUI()
        {
            var dialog = EditorManager.inst.GetDialog("Metadata Editor").Dialog;

            var content = dialog.Find("Scroll View/Viewport/Content");

            difficultyToggle = content.Find("song/difficulty/toggles/easy").gameObject;
            difficultyToggle.transform.SetParent(transform);
            LSHelpers.DeleteChildren(content.Find("song/difficulty/toggles"));
            Destroy(content.Find("song/difficulty/toggles").GetComponent<ToggleGroup>());

            if (!content.Find("artist/link/inputs/openurl"))
            {
                var openLink = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x").gameObject.Duplicate(content.Find("artist/link/inputs"), "openurl", 0);
                openLink.transform.Find("Image").gameObject.GetComponent<Image>().sprite = EditorManager.inst.DropdownMenus[3].transform.Find("Open Workshop").Find("Image").gameObject.GetComponent<Image>().sprite;

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

            if (!content.Find("creator/link"))
                content.Find("artist/link").gameObject.Duplicate(content.Find("creator"), "link", 3);

            if (!content.Find("song/link"))
                content.Find("artist/link").gameObject.Duplicate(content.Find("song"), "link", 2);

            // Tag Scroll View
            {
                var tagParent = content.Find("creator/description (1)");
                tagParent.name = "tags";
                tagParent.gameObject.SetActive(true);
                Destroy(tagParent.Find("input").gameObject);

                tagParent.AsRT().sizeDelta = new Vector2(757f, 32f);

                tagParent.Find("Panel/title").GetComponent<Text>().text = "Tags";

                var tagScrollView = Creator.NewUIObject("Scroll View", tagParent);
                tagScrollView.transform.AsRT().sizeDelta = new Vector2(522f, 40f);

                var scroll = tagScrollView.AddComponent<ScrollRect>();
                scroll.horizontal = true;
                scroll.vertical = false;

                var image = tagScrollView.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.01f);

                tagScrollView.AddComponent<Mask>();

                var tagViewport = Creator.NewUIObject("Viewport", tagScrollView.transform);
                UIManager.SetRectTransform(tagViewport.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

                var tagContent = Creator.NewUIObject("Content", tagViewport.transform);
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

            var submitBase = content.Find("submit");
            var convert = submitBase.Find("submit").gameObject;
            convert.name = "convert";

            var convertImage = convert.GetComponent<Image>();
            convertImage.sprite = null;

            convert.transform.AsRT().anchoredPosition = new Vector2(-240f, 0f);
            convert.transform.AsRT().sizeDelta = new Vector2(230f, 48f);
            var convertText = convert.transform.Find("Text").GetComponent<Text>();
            convertText.resizeTextForBestFit = false;
            convertText.fontSize = 22;
            convertText.text = "Convert to VG Format";

            var upload = convert.Duplicate(submitBase, "upload");

            upload.transform.AsRT().anchoredPosition = new Vector2(0f, 0f);
            upload.transform.AsRT().sizeDelta = new Vector2(230f, 48f);
            var uploadText = upload.transform.Find("Text").GetComponent<Text>();
            uploadText.resizeTextForBestFit = false;
            uploadText.fontSize = 22;
            uploadText.text = "Upload to Server";

            var zip = convert.Duplicate(submitBase, "delete");

            zip.transform.AsRT().anchoredPosition = new Vector2(240f, 0f);
            zip.transform.AsRT().sizeDelta = new Vector2(230f, 48f);
            var zipText = zip.transform.Find("Text").GetComponent<Text>();
            zipText.resizeTextForBestFit = false;
            zipText.fontSize = 22;
            zipText.text = "Delete Level";

            content.Find("id").gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var artist = content.Find("artist");
            var song = content.Find("song");
            var creator = content.Find("creator");

            var creatorLinkTitle = creator.Find("link/title").GetComponent<Text>();
            creatorLinkTitle.text = "Creator Link";

            content.Find("spacer (1)").AsRT().sizeDelta = new Vector2(732f, 80f);

            content.Find("creator/description").AsRT().sizeDelta = new Vector2(757f, 140f);
            content.Find("creator/description/input").AsRT().sizeDelta = new Vector2(523f, 140f);
            content.Find("agreement").AsRT().sizeDelta = new Vector2(732f, 100f);

            content.Find("id/revisions").gameObject.SetActive(true);

            content.Find("spacer").gameObject.SetActive(true);
            submitBase.gameObject.SetActive(true);

            #region Editor Theme Setup

            EditorThemeManager.AddGraphic(convertImage, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(convertText, ThemeGroup.Function_1_Text);
            EditorThemeManager.AddGraphic(upload.GetComponent<Image>(), ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(uploadText, ThemeGroup.Function_1_Text);
            EditorThemeManager.AddGraphic(zip.GetComponent<Image>(), ThemeGroup.Delete, true);
            EditorThemeManager.AddGraphic(zipText, ThemeGroup.Delete_Text);
            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);

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
            EditorThemeManager.AddLightText(content.Find("agreement/text").GetComponent<Text>());
            EditorThemeManager.AddLightText(content.Find("id/id").GetComponent<Text>());
            EditorThemeManager.AddLightText(content.Find("id/revisions").GetComponent<Text>());

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

            var authPath = Path.Combine(Application.persistentDataPath, "auth.json");
            if (RTFile.FileExists(authPath))
                authData = JSON.Parse(RTFile.ReadFromFile(authPath)).AsObject;

            yield break;
        }

        void RenderDifficultyToggles()
        {
            var content = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content");
            var toggles = content.Find("song/difficulty/toggles");
            LSHelpers.DeleteChildren(toggles);

            int num = 0;
            foreach (var difficulty in DataManager.inst.difficulties)
            {
                int index = num;
                var gameObject = difficultyToggle.Duplicate(toggles, difficulty.name.ToLower(), num == DataManager.inst.difficulties.Count - 1 ? 0 : num + 1);
                gameObject.transform.localScale = Vector3.one;

                ((RectTransform)gameObject.transform).sizeDelta = new Vector2(69f, 32f);

                var text = gameObject.transform.Find("Background/Text").GetComponent<Text>();
                text.color = LSColors.ContrastColor(difficulty.color);
                text.text = num == DataManager.inst.difficulties.Count - 1 ? "Anim" : difficulty.name;
                text.fontSize = 17;
                var toggle = gameObject.GetComponent<Toggle>();
                toggle.image.color = difficulty.color;
                toggle.group = null;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = DataManager.inst.metaData.song.difficulty == num;
                toggle.onValueChanged.AddListener(_val =>
                {
                    DataManager.inst.metaData.song.difficulty = index;
                    RenderDifficultyToggles();
                });

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_1);

                num++;
            }
        }

        void RenderTags()
        {
            var content = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content");
            var parent = content.Find("creator/tags/Scroll View/Viewport/Content");
            var moddedMetadata = MetaData.Current;

            LSHelpers.DeleteChildren(parent);

            for (int i = 0; i < moddedMetadata.LevelSong.tags.Length; i++)
            {
                int index = i;
                var tag = moddedMetadata.LevelSong.tags[i];
                var gameObject = RTEditor.inst.tagPrefab.Duplicate(parent, index.ToString());
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.text = tag;
                input.onValueChanged.AddListener(_val =>
                {
                    var oldVal = moddedMetadata.LevelSong.tags[index];
                    moddedMetadata.LevelSong.tags[index] = _val;

                    EditorManager.inst.history.Add(new History.Command("Change MetaData Tag", () =>
                    {
                        moddedMetadata.LevelSong.tags[index] = _val;
                        MetadataEditor.inst.OpenDialog();
                    }, () =>
                    {
                        moddedMetadata.LevelSong.tags[index] = oldVal;
                        MetadataEditor.inst.OpenDialog();
                    }));
                });

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(() =>
                {
                    var list = moddedMetadata.LevelSong.tags.ToList();
                    var oldTag = list[index];
                    list.RemoveAt(index);
                    moddedMetadata.LevelSong.tags = list.ToArray();
                    RenderTags();

                    EditorManager.inst.history.Add(new History.Command("Delete MetaData Tag", () =>
                    {
                        var list = moddedMetadata.LevelSong.tags.ToList();
                        list.RemoveAt(index);
                        moddedMetadata.LevelSong.tags = list.ToArray();
                        MetadataEditor.inst.OpenDialog();
                    }, () =>
                    {
                        var list = moddedMetadata.LevelSong.tags.ToList();
                        list.Insert(index, oldTag);
                        moddedMetadata.LevelSong.tags = list.ToArray();
                        MetadataEditor.inst.OpenDialog();
                    }));
                });

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Input_Field, true);

                EditorThemeManager.ApplyInputField(input);

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);
            }

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(content.Find("creator/tags/Scroll View/Viewport/Content"), "Add");
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Tag";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(() =>
            {
                var list = moddedMetadata.LevelSong.tags.ToList();
                list.Add("New Tag");
                moddedMetadata.LevelSong.tags = list.ToArray();
                RenderTags();

                EditorManager.inst.history.Add(new History.Command("Add MetaData Tag", () =>
                {
                    var list = moddedMetadata.LevelSong.tags.ToList();
                    list.Add("New Tag");
                    moddedMetadata.LevelSong.tags = list.ToArray();
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    var list = moddedMetadata.LevelSong.tags.ToList();
                    list.RemoveAt(list.Count - 1);
                    moddedMetadata.LevelSong.tags = list.ToArray();
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);
        }

        public void RenderEditor()
        {
            Debug.Log($"{MetadataEditor.inst.className}Render the Metadata Editor");

            var content = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content");

            if (!MetaData.IsValid)
            {
                EditorManager.inst.HideDialog("Metadata Editor");
                EditorManager.inst.DisplayNotification("Metadata was not valid.", 1.4f, EditorManager.NotificationType.Error);
                return;
            }

            var metadata = MetaData.Current;

            var openArtistURL = content.Find("artist/link/inputs/openurl").GetComponent<Button>();
            openArtistURL.onClick.ClearAll();
            openArtistURL.onClick.AddListener(() => { Application.OpenURL(metadata.artist.getUrl()); });

            var artistName = content.Find("artist/name/input").GetComponent<InputField>();
            artistName.onEndEdit.ClearAll();
            artistName.text = metadata.artist.Name;
            artistName.onEndEdit.AddListener(_val =>
            {
                string oldVal = metadata.artist.Name;
                metadata.artist.Name = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Name", () =>
                {
                    metadata.artist.Name = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.artist.Name = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            var artistLink = content.Find("artist/link/inputs/input").GetComponent<InputField>();
            artistLink.onEndEdit.ClearAll();
            artistLink.text = metadata.artist.Link;
            artistLink.onEndEdit.AddListener(_val =>
            {
                string oldVal = metadata.artist.Link;
                metadata.artist.Link = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", () =>
                {
                    metadata.artist.Link = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.artist.Link = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            var artistLinkTypes = content.Find("artist/link/inputs/dropdown").GetComponent<Dropdown>();
            artistLinkTypes.onValueChanged.ClearAll();
            artistLinkTypes.options = DataManager.inst.linkTypes.Select(x => new Dropdown.OptionData(x.name)).ToList();
            artistLinkTypes.value = metadata.artist.LinkType;
            artistLinkTypes.onValueChanged.AddListener(_val =>
            {
                int oldVal = metadata.artist.LinkType;
                metadata.artist.LinkType = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", () =>
                {
                    metadata.artist.LinkType = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.artist.LinkType = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            var uploaderName = content.Find("creator/uploader_name/input").GetComponent<InputField>();
            uploaderName.onValueChanged.ClearAll();
            uploaderName.text = metadata.uploaderName;
            uploaderName.onValueChanged.AddListener(_val => { metadata.uploaderName = _val; });

            var creatorName = content.Find("creator/name/input").GetComponent<InputField>();
            creatorName.onValueChanged.ClearAll();
            creatorName.text = metadata.creator.steam_name;
            creatorName.onValueChanged.AddListener(_val => { metadata.creator.steam_name = _val; });

            var levelName = content.Find("creator/level_name/input").GetComponent<InputField>();
            levelName.onValueChanged.ClearAll();
            levelName.text = metadata.LevelBeatmap.name;
            levelName.onValueChanged.AddListener(_val => { metadata.LevelBeatmap.name = _val; });

            var songTitle = content.Find("song/title/input").GetComponent<InputField>();
            songTitle.onValueChanged.ClearAll();
            songTitle.text = metadata.song.title;
            songTitle.onValueChanged.AddListener(_val => { metadata.song.title = _val; });

            var openCreatorURL = content.Find("creator/link/inputs/openurl").GetComponent<Button>();
            openCreatorURL.onClick.ClearAll();
            openCreatorURL.onClick.AddListener(() =>
            {
                if (metadata.LevelCreator.URL != null)
                    Application.OpenURL(metadata.LevelCreator.URL);
            });

            var creatorLink = content.Find("creator/link/inputs/input").GetComponent<InputField>();
            creatorLink.onEndEdit.ClearAll();
            creatorLink.text = metadata.LevelCreator.link;
            creatorLink.onEndEdit.AddListener(_val =>
            {
                string oldVal = metadata.LevelCreator.link;
                metadata.LevelCreator.link = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", () =>
                {
                    metadata.LevelCreator.link = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.LevelCreator.link = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            var creatorLinkTypes = content.Find("creator/link/inputs/dropdown").GetComponent<Dropdown>();
            creatorLinkTypes.onValueChanged.ClearAll();
            creatorLinkTypes.options = LevelCreator.creatorLinkTypes.Select(x => new Dropdown.OptionData(x.name)).ToList();
            creatorLinkTypes.value = metadata.LevelCreator.linkType;
            creatorLinkTypes.onValueChanged.AddListener(_val =>
            {
                int oldVal = metadata.LevelCreator.linkType;
                metadata.LevelCreator.linkType = _val;
                EditorManager.inst.history.Add(new History.Command("Change Creator Link", () =>
                {
                    metadata.LevelCreator.linkType = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.LevelCreator.linkType = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            var openSongURL = content.Find("song/link/inputs/openurl").GetComponent<Button>();
            openSongURL.onClick.ClearAll();
            openSongURL.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(metadata.LevelSong.link))
                    Application.OpenURL(metadata.SongURL);
            });

            var songLink = content.Find("song/link/inputs/input").GetComponent<InputField>();
            songLink.onEndEdit.ClearAll();
            songLink.text = metadata.LevelSong.link;
            songLink.onEndEdit.AddListener(_val =>
            {
                string oldVal = metadata.LevelSong.link;
                metadata.LevelSong.link = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", () =>
                {
                    metadata.LevelSong.link = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.LevelSong.link = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            var songLinkTypes = content.Find("song/link/inputs/dropdown").GetComponent<Dropdown>();
            songLinkTypes.onValueChanged.ClearAll();
            songLinkTypes.options = CoreHelper.InstanceLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
            songLinkTypes.value = metadata.LevelSong.linkType;
            songLinkTypes.onValueChanged.AddListener(_val =>
            {
                int oldVal = metadata.LevelSong.linkType;
                metadata.LevelSong.linkType = _val;
                EditorManager.inst.history.Add(new History.Command("Change Creator Link", () =>
                {
                    metadata.LevelSong.linkType = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.LevelSong.linkType = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            var creatorDescription = content.Find("creator/description/input").GetComponent<InputField>();
            creatorDescription.onValueChanged.ClearAll();
            creatorDescription.text = metadata.song.description;
            creatorDescription.onValueChanged.AddListener(_val => { metadata.song.description = _val; });

            RenderDifficultyToggles();
            RenderTags();

            content.Find("agreement/text").GetComponent<Text>().text = "If you want to upload to the Steam Workshop, you can convert the level to the current level format for vanilla PA and upload it to the workshop. Beware any modded features not in current PA will not be saved. " +
                "However, if you want to include modded features, then it's recommended to upload to the arcade server or zip the level.";

            bool hasID = !string.IsNullOrEmpty(metadata.serverID); // Only check for server id.
            content.Find("id/id").GetComponent<Text>().text = !string.IsNullOrEmpty(metadata.ID) ? $"Level ID: {metadata.ID} (Click this text to copy)" : "No ID assigned.";
            var idClickable = content.Find("id").GetComponent<Clickable>() ?? content.Find("id").gameObject.AddComponent<Clickable>();
            idClickable.onClick = eventData =>
            {
                LSText.CopyToClipboard(metadata.ID);
                EditorManager.inst.DisplayNotification($"Copied ID: {metadata.ID} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
            };

            // Changed revisions to modded display.
            content.Find("id/revisions").GetComponent<Text>().text = $"Modded: {(GameData.Current.Modded ? "Yes" : "No")}";

            var uploadText = content.Find("submit/upload/Text").GetComponent<Text>();
            uploadText.text = hasID ? "Update" : "Upload";

            var submitBase = content.Find("submit");

            var convert = submitBase.Find("convert").GetComponent<Button>();
            convert.onClick.ClearAll();
            convert.onClick.AddListener(ConvertLevel);

            var upload = submitBase.Find("upload").GetComponent<Button>();
            upload.onClick.ClearAll();
            upload.onClick.AddListener(UploadLevel);

            var zip = submitBase.Find("delete").GetComponent<Button>();
            zip.onClick.ClearAll();
            zip.onClick.AddListener(DeleteLevel);
        }

        public void ConvertLevel()
        {
            var exportPath = EditorConfig.Instance.ConvertLevelLSToVGExportPath.Value;

            if (string.IsNullOrEmpty(exportPath))
            {
                if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/exports"))
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/exports");
                exportPath = RTFile.ApplicationDirectory + "beatmaps/exports/";
            }

            if (exportPath[exportPath.Length - 1] != '/')
                exportPath += "/";

            if (!RTFile.DirectoryExists(Path.GetDirectoryName(exportPath)))
            {
                EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var vg = GameData.Current.ToJSONVG();

            var metadata = MetaData.Current.ToJSONVG();

            var path = exportPath + EditorManager.inst.currentLoadedLevel;

            if (!RTFile.DirectoryExists(path))
                Directory.CreateDirectory(path);

            var ogPath = GameManager.inst.path.Replace("/level.lsb", "");

            if (RTFile.FileExists(ogPath + "/level.ogg"))
            {
                File.Copy(ogPath + "/level.ogg", path + "/audio.ogg", RTFile.FileExists(path + "/audio.ogg"));
            }

            if (RTFile.FileExists(ogPath + "/level.jpg"))
            {
                File.Copy(ogPath + "/level.jpg", path + "/cover.jpg", RTFile.FileExists(path + "/cover.jpg"));
            }

            try
            {
                RTFile.WriteToFile(path + "/metadata.vgm", metadata.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"{MetadataEditor.inst.className}Convert to VG error (MetaData) {ex}");
            }

            try
            {
                RTFile.WriteToFile(path + "/level.vgd", vg.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"{MetadataEditor.inst.className}Convert to VG error (GameData) {ex}");
            }

            EditorManager.inst.DisplayNotification($"Converted Level \"{EditorManager.inst.currentLoadedLevel}\" from LS format to VG format and saved to {Path.GetFileName(path)}.", 4f,
                EditorManager.NotificationType.Success);
        }

        public void UploadLevel()
        {
            if (!AlephNetworkManager.ServerFinished)
            {
                EditorManager.inst.DisplayNotification("Server is not up yet.", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            var exportPath = EditorConfig.Instance.ZIPLevelExportPath.Value;

            if (string.IsNullOrEmpty(exportPath))
            {
                if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/exports"))
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/exports");
                exportPath = RTFile.ApplicationDirectory + "beatmaps/exports/";
            }

            if (exportPath[exportPath.Length - 1] != '/')
                exportPath += "/";

            if (!RTFile.DirectoryExists(Path.GetDirectoryName(exportPath)))
            {
                EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var path = exportPath + EditorManager.inst.currentLoadedLevel + "-server-upload.zip";

            try
            {
                MetaData.Current.LevelBeatmap.date_published = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");

                if (RTFile.FileExists(path))
                    File.Delete(path);

                ZipFile.CreateFromDirectory(GameManager.inst.basePath, path);

                var headers = new Dictionary<string, string>();
                if (authData != null && authData["access_token"] != null)
                    headers["Authorization"] = $"Bearer {authData["access_token"].Value}";

                CoreHelper.StartCoroutine(AlephNetworkManager.UploadBytes($"{AlephNetworkManager.ArcadeServerURL}api/level", File.ReadAllBytes(path), id =>
                {
                    MetaData.Current.serverID = id;
                    MetaData.Current.beatmap.version_number++;

                    var jn = MetaData.Current.ToJSON();
                    RTFile.WriteToFile(GameManager.inst.basePath + "metadata.lsb", jn.ToString());

                    if (RTFile.FileExists(path))
                        File.Delete(path);

                    EditorManager.inst.DisplayNotification($"Level uploaded! ID: {id}", 3f, EditorManager.NotificationType.Success);
                    MetadataEditor.inst.Render();
                }, (string onError, long responseCode, string errorMsg) =>
                {
                    MetaData.Current.LevelBeatmap.date_published = "";
                    var jn = MetaData.Current.ToJSON();
                    RTFile.WriteToFile(GameManager.inst.basePath + "metadata.lsb", jn.ToString());

                    if (RTFile.FileExists(path))
                        File.Delete(path);

                    switch (responseCode)
                    {
                        case 404:
                            EditorManager.inst.DisplayNotification("404 not found.", 2f, EditorManager.NotificationType.Error);
                            return;
                        case 401:
                            {
                                if (authData != null && authData["access_token"] != null && authData["refresh_token"] != null)
                                {
                                    CoreHelper.StartCoroutine(RefreshTokens(UploadLevel));
                                    return;
                                }
                                ShowLoginPopup();
                                break;
                            }
                        default:
                            EditorManager.inst.DisplayNotification($"Upload failed. Error code: {onError}", 2f, EditorManager.NotificationType.Error);
                            break;
                    }

                    if (errorMsg != null)
                        CoreHelper.LogError($"Error Message: {errorMsg}");

                }, headers));
            }
            catch (Exception ex)
            {
                Debug.LogError($"{MetadataEditor.inst.className}There was an error while creating the ZIP file.\n{ex}");
            }
        }

        public void DeleteLevel()
        {
            if (!AlephNetworkManager.ServerFinished)
            {
                EditorManager.inst.DisplayNotification("Server is not up yet.", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            var id = MetaData.Current.serverID;

            try
            {
                var headers = new Dictionary<string, string>();
                if (authData != null && authData["access_token"] != null)
                    headers["Authorization"] = $"Bearer {authData["access_token"].Value}";

                CoreHelper.StartCoroutine(AlephNetworkManager.Delete($"{AlephNetworkManager.ArcadeServerURL}api/level/{id}", () =>
                {
                    MetaData.Current.serverID = null;
                    DataManager.inst.SaveMetadata(GameManager.inst.basePath + "metadata.lsb");

                    EditorManager.inst.DisplayNotification($"Successfully deleted level off the Arcade server.", 2.5f, EditorManager.NotificationType.Success);
                }, (string onError, long responseCode) =>
                {
                    switch (responseCode)
                    {
                        case 404:
                            EditorManager.inst.DisplayNotification("404 not found.", 2f, EditorManager.NotificationType.Error);
                            return;
                        case 401:
                            {
                                if (authData != null && authData["access_token"] != null && authData["refresh_token"] != null)
                                {
                                    CoreHelper.StartCoroutine(RefreshTokens(DeleteLevel));
                                    return;
                                }
                                ShowLoginPopup();
                                break;
                            }
                        default:
                            EditorManager.inst.DisplayNotification($"Delete failed. Error code: {onError}", 2f, EditorManager.NotificationType.Error);
                            break;
                    }
                }, headers));
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        IEnumerator IDeleteLevel()
        {
            yield break;
        }

        public void ShowLoginPopup()
        {
            RTEditor.inst.ShowWarningPopup("You are not logged in.", () =>
            {
                Application.OpenURL($"{AlephNetworkManager.ArcadeServerURL}api/auth/login");
                CreateLoginListener();
                RTEditor.inst.HideWarningPopup();
            }, RTEditor.inst.HideWarningPopup, "Login", "Cancel");
        }

        public IEnumerator RefreshTokens(Action onRefreshed)
        {
            EditorManager.inst.DisplayNotification("Access token expired. Refreshing...", 5f, EditorManager.NotificationType.Warning);

            var form = new WWWForm();
            form.AddField("AccessToken", authData["access_token"].Value);
            form.AddField("RefreshToken", authData["refresh_token"].Value);

            using var www = UnityWebRequest.Post($"{AlephNetworkManager.ArcadeServerURL}api/auth/refresh", form);
            www.certificateHandler = new AlephNetworkManager.ForceAcceptAll();
            yield return www.SendWebRequest();

            if (www.isNetworkError)
            {
                EditorManager.inst.DisplayNotification($"Upload failed. Error: {www.error}", 5f, EditorManager.NotificationType.Error);
                yield break;
            }

            if (www.isHttpError)
            {
                EditorManager.inst.DisplayNotification(www.downloadHandler.text, 5f, EditorManager.NotificationType.Error);
                ShowLoginPopup();
                yield break;
            }

            var jn = JSON.Parse(www.downloadHandler.text);
            authData["access_token"] = jn["accessToken"].Value;
            authData["refresh_token"] = jn["refreshToken"].Value;
            authData["access_token_expiry_time"] = jn["accessTokenExpiryTime"].Value;

            RTFile.WriteToFile(Path.Combine(Application.persistentDataPath, "auth.json"), authData.ToString());
            EditorManager.inst.DisplayNotification("Refreshed tokens! Uploading...", 5f, EditorManager.NotificationType.Success);
            //UploadLevel();
            onRefreshed?.Invoke();
        }

        void CreateLoginListener()
        {
            if (_listener == null)
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://localhost:1234/");
                _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                _listener.Start();
            }

            CoreHelper.StartCoroutine(StartListenerCoroutine());
        }

        IEnumerator StartListenerCoroutine()
        {
            while (_listener.IsListening)
            {
                var task = _listener.GetContextAsync();
                yield return new WaitUntil(() => task.IsCompleted);
                ProcessRequest(task.Result);
            }
        }

        void ProcessRequest(HttpListenerContext context)
        {
            var query = context.Request.QueryString;
            if (query["success"] != "true")
            {
                SendResponse(context.Response, HttpStatusCode.Unauthorized, "Unauthorized");
                return;
            }

            var id = query["id"];
            var username = query["username"];
            var steamId = query["steam_id"];
            var accessToken = query["access_token"];
            var refreshToken = query["refresh_token"];
            var accessTokenExpiryTime = query["access_token_expiry_time"];

            if (id == null || username == null || steamId == null || accessToken == null || refreshToken == null || accessTokenExpiryTime == null)
            {
                SendResponse(context.Response, HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            authData = new JSONObject
            {
                ["id"] = id,
                ["username"] = username,
                ["steam_id"] = steamId,
                ["access_token"] = accessToken,
                ["refresh_token"] = refreshToken,
                ["access_token_expiry_time"] = accessTokenExpiryTime
            };

            RTFile.WriteToFile(Path.Combine(Application.persistentDataPath, "auth.json"), authData.ToString());
            EditorManager.inst.DisplayNotification($"Successfully logged in as {username}!", 8f, EditorManager.NotificationType.Success);
            SendResponse(context.Response, HttpStatusCode.OK, "Success! You can close this page and go back to the game now.");
        }

        void SendResponse(HttpListenerResponse response, HttpStatusCode code, string message = null)
        {
            response.StatusCode = (int)code;
            if (message != null)
            {
                response.ContentType = "text/plain";
                var body = Encoding.UTF8.GetBytes(message);
                response.OutputStream.Write(body, 0, body.Length);
            }
            response.Close();
        }
    }
}
