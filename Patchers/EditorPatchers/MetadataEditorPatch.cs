﻿using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using SimpleJSON;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(MetadataEditor))]
    public class MetadataEditorPatch : MonoBehaviour
    {
        static MetadataEditor Instance { get => MetadataEditor.inst; set => MetadataEditor.inst = value; }

        static GameObject difficultyToggle;

        static GameObject tagPrefab;

        static JSONObject authData;

        [HarmonyPatch(nameof(MetadataEditor.Awake))]
        [HarmonyPrefix]
        static bool Awake(MetadataEditor __instance)
        {
            if (Instance == null)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(__instance.className);

            Instance.StartCoroutine(Wait());

            return false;
        }

        static IEnumerator Wait()
        {
            yield return new WaitForSeconds(0.2f);

            var dialog = EditorManager.inst.GetDialog("Metadata Editor").Dialog;

            var content = dialog.Find("Scroll View/Viewport/Content");

            difficultyToggle = content.Find("song/difficulty/toggles/easy").gameObject;
            difficultyToggle.transform.SetParent(null);
            LSHelpers.DeleteChildren(content.Find("song/difficulty/toggles"));
            Destroy(content.Find("song/difficulty/toggles").GetComponent<ToggleGroup>());

            if (!content.Find("artist/link/inputs/openurl"))
            {
                var openLink = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x").gameObject.Duplicate(content.Find("artist/link/inputs"), "openurl", 0);
                openLink.transform.Find("Image").gameObject.GetComponent<Image>().sprite = EditorManager.inst.DropdownMenus[3].transform.Find("Open Workshop").Find("Image").gameObject.GetComponent<Image>().sprite;

                var openLinkLE = openLink.AddComponent<LayoutElement>();
                var openLinkButton = openLink.GetComponent<Button>();

                openLinkLE.minWidth = 32f;
                openLinkButton.onClick.RemoveAllListeners();

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

            // Tag Prefab
            {
                var tagParent = content.Find("creator/description (1)");
                tagParent.name = "tags";
                tagParent.gameObject.SetActive(true);
                Destroy(tagParent.Find("input").gameObject);

                ((RectTransform)tagParent).sizeDelta = new Vector2(757f, 32f);

                tagParent.Find("Panel/title").GetComponent<Text>().text = "Tags";

                var tagScrollView = new GameObject("Scroll View");
                tagScrollView.transform.SetParent(tagParent);
                tagScrollView.transform.localScale = Vector3.one;

                var tagScrollViewRT = tagScrollView.AddComponent<RectTransform>();
                tagScrollViewRT.sizeDelta = new Vector2(522f, 40f);
                var scroll = tagScrollView.AddComponent<ScrollRect>();

                scroll.horizontal = true;
                scroll.vertical = false;

                var image = tagScrollView.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.01f);

                tagScrollView.AddComponent<Mask>();

                var tagViewport = new GameObject("Viewport");
                tagViewport.transform.SetParent(tagScrollViewRT);
                tagViewport.transform.localScale = Vector3.one;

                var tagViewPortRT = tagViewport.AddComponent<RectTransform>();
                tagViewPortRT.anchoredPosition = Vector2.zero;
                tagViewPortRT.anchorMax = Vector2.one;
                tagViewPortRT.anchorMin = Vector2.zero;
                tagViewPortRT.sizeDelta = Vector2.zero;

                var tagContent = new GameObject("Content");
                tagContent.transform.SetParent(tagViewPortRT);
                tagContent.transform.localScale = Vector3.one;

                var tagContentRT = tagContent.AddComponent<RectTransform>();

                var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
                tagContentGLG.cellSize = new Vector2(168f, 32f);
                tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                tagContentGLG.constraintCount = 1;
                tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
                tagContentGLG.spacing = new Vector2(8f, 0f);

                var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
                tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

                scroll.viewport = tagViewPortRT;
                scroll.content = tagContentRT;

                tagPrefab = new GameObject("Tag");
                var tagPrefabRT = tagPrefab.AddComponent<RectTransform>();
                var tagPrefabImage = tagPrefab.AddComponent<Image>();
                tagPrefabImage.color = new Color(1f, 1f, 1f, 0.12f);
                var tagPrefabLayout = tagPrefab.AddComponent<HorizontalLayoutGroup>();
                tagPrefabLayout.childControlWidth = false;
                tagPrefabLayout.childForceExpandWidth = false;

                var input = RTEditor.inst.defaultIF.Duplicate(tagPrefabRT, "input");
                ((RectTransform)input.transform).sizeDelta = new Vector2(136f, 32f);
                input.transform.Find("Text").GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                input.transform.Find("Text").GetComponent<Text>().fontSize = 17;

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(tagPrefabRT, "delete");
                UIManager.SetRectTransform(delete.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.one, Vector2.one, new Vector2(32f, 32f));
            }

            var submitBase = content.Find("submit");
            var convert = submitBase.Find("submit").gameObject;
            convert.name = "convert";

            var bcol = new Color(0.3922f, 0.7098f, 0.9647f, 1f);

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

            var zip = convert.Duplicate(submitBase, "zip");

            zip.transform.AsRT().anchoredPosition = new Vector2(240f, 0f);
            zip.transform.AsRT().sizeDelta = new Vector2(230f, 48f);
            var zipText = zip.transform.Find("Text").GetComponent<Text>();
            zipText.resizeTextForBestFit = false;
            zipText.fontSize = 22;
            zipText.text = "ZIP Level";

            content.Find("id").gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            EditorThemeManager.AddGraphic(convertImage, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(convertText, ThemeGroup.Function_1_Text);
            EditorThemeManager.AddGraphic(upload.GetComponent<Image>(), ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(uploadText, ThemeGroup.Function_1_Text);
            EditorThemeManager.AddGraphic(zip.GetComponent<Image>(), ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(zipText, ThemeGroup.Function_1_Text);
            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);

            var artist = content.Find("artist");
            var song = content.Find("song");
            var creator = content.Find("creator");

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
            var creatorLinkTitle = creator.Find("link/title").GetComponent<Text>();
            creatorLinkTitle.text = "Creator Link";
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

            var authPath = Path.Combine(Application.persistentDataPath, "auth.json");
            if (RTFile.FileExists(authPath))
                authData = JSON.Parse(RTFile.ReadFromFile(authPath)).AsObject;
        }

        static void SetToggleList()
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
                    SetToggleList();
                });

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_1);

                num++;
            }
        }

        [HarmonyPatch(nameof(MetadataEditor.Render))]
        [HarmonyPrefix]
        static bool Render()
        {
            Debug.Log($"{Instance.className}Render the Metadata Editor");

            var content = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content");

            ((RectTransform)content.Find("spacer (1)")).sizeDelta = new Vector2(732f, 80f);

            if (!MetaData.IsValid)
            {
                EditorManager.inst.HideDialog("Metadata Editor");
                EditorManager.inst.DisplayNotification("Metadata was not valid.", 1.4f, EditorManager.NotificationType.Error);
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
                    Instance.Render();
                }, () =>
                {
                    metadata.artist.Name = oldVal;
                    Instance.Render();
                }), false);
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
                    Instance.Render();
                }, () =>
                {
                    metadata.artist.Link = oldVal;
                    Instance.Render();
                }), false);
            });

            var artistLinkTypes = content.Find("artist/link/inputs/dropdown").GetComponent<Dropdown>();
            artistLinkTypes.options.Clear();
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
                    Instance.Render();
                }, () =>
                {
                    metadata.artist.LinkType = oldVal;
                    Instance.Render();
                }), false);
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
                    Instance.Render();
                }, () =>
                {
                    metadata.LevelCreator.link = oldVal;
                    Instance.Render();
                }), false);
            });

            var creatorLinkTypes = content.Find("creator/link/inputs/dropdown").GetComponent<Dropdown>();
            creatorLinkTypes.options.Clear();
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
                    Instance.Render();
                }, () =>
                {
                    metadata.LevelCreator.linkType = oldVal;
                    Instance.Render();
                }), false);
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
                    Instance.Render();
                }, () =>
                {
                    metadata.LevelSong.link = oldVal;
                    Instance.Render();
                }), false);
            });

            var songLinkTypes = content.Find("song/link/inputs/dropdown").GetComponent<Dropdown>();
            songLinkTypes.options.Clear();
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
                    Instance.Render();
                }, () =>
                {
                    metadata.LevelSong.linkType = oldVal;
                    Instance.Render();
                }), false);
            });

            ((RectTransform)content.Find("creator/description")).sizeDelta = new Vector2(757f, 140f);
            ((RectTransform)content.Find("creator/description/input")).sizeDelta = new Vector2(523f, 140f);
            var creatorDescription = content.Find("creator/description/input").GetComponent<InputField>();
            creatorDescription.onValueChanged.ClearAll();
            creatorDescription.text = metadata.song.description;
            creatorDescription.onValueChanged.AddListener(_val => { metadata.song.description = _val; });

            SetToggleList();

            RenderTags();
            content.Find("agreement/text").GetComponent<Text>().text = "If you want to upload to the Steam Workshop, you can convert the level to the current level format for vanilla PA and upload it to the workshop. Beware any modded features not in current PA will not be saved. " +
                "However, if you want to include modded features, then it's recommended to upload to the arcade server or zip the level.";

            bool hasID = !string.IsNullOrEmpty(metadata.serverID);
            content.Find("id/id").GetComponent<Text>().text = !string.IsNullOrEmpty(metadata.ID) ? $"Level ID: {metadata.ID} (Click this text to copy)" : "No ID assigned.";
            var idClickable = content.Find("id").GetComponent<Clickable>() ?? content.Find("id").gameObject.AddComponent<Clickable>();
            idClickable.onClick = eventData =>
            {
                LSText.CopyToClipboard(metadata.ID);
                EditorManager.inst.DisplayNotification($"Copied ID: {metadata.ID} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
            };

            // Changed revisions to modded display.
            content.Find("id/revisions").gameObject.SetActive(true);
            content.Find("id/revisions").GetComponent<Text>().text = $"Modded: {(GameData.Current.Modded ? "Yes" : "No")}";

            var uploadText = content.Find("submit/upload/Text").GetComponent<Text>();
            uploadText.text = hasID ? "Update" : "Upload";

            var submitBase = content.Find("submit");

            content.Find("spacer").gameObject.SetActive(true);
            submitBase.gameObject.SetActive(true);

            var convert = submitBase.Find("convert").GetComponent<Button>();
            convert.onClick.ClearAll();
            convert.onClick.AddListener(() =>
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
                    Debug.LogError($"{Instance.className}Convert to VG error (MetaData) {ex}");
                }

                try
                {
                    RTFile.WriteToFile(path + "/level.vgd", vg.ToString());
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{Instance.className}Convert to VG error (GameData) {ex}");
                }

                EditorManager.inst.DisplayNotification($"Converted Level \"{EditorManager.inst.currentLoadedLevel}\" from LS format to VG format and saved to {Path.GetFileName(path)}.", 4f,
                    EditorManager.NotificationType.Success);
            });

            var upload = submitBase.Find("upload").GetComponent<Button>();
            upload.onClick.ClearAll();
            upload.onClick.AddListener(UploadLevel);

            var zip = submitBase.Find("zip").GetComponent<Button>();
            zip.onClick.ClearAll();
            zip.onClick.AddListener(() =>
            {
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

                var path = exportPath + EditorManager.inst.currentLoadedLevel + ".zip";

                try
                {
                    if (RTFile.FileExists(path))
                        File.Delete(path);

                    ZipFile.CreateFromDirectory(GameManager.inst.basePath, path);
                    EditorManager.inst.DisplayNotification($"Sucessfully created {EditorManager.inst.currentLoadedLevel}.zip.", 2f, EditorManager.NotificationType.Success);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{Instance.className}There was an error in creating the ZIP file.\n{ex}");
                }
            });

            return false;
        }

        public static void UploadLevel()
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

                Instance.StartCoroutine(AlephNetworkManager.UploadBytes($"{AlephNetworkManager.ArcadeServerURL}api/level", File.ReadAllBytes(path), id =>
                {
                    MetaData.Current.serverID = id;
                    MetaData.Current.beatmap.version_number++;

                    var jn = MetaData.Current.ToJSON();
                    RTFile.WriteToFile(GameManager.inst.basePath + "metadata.lsb", jn.ToString());

                    if (RTFile.FileExists(path))
                        File.Delete(path);

                    EditorManager.inst.DisplayNotification($"Level uploaded! ID: {id}", 3f, EditorManager.NotificationType.Success);
                    Instance.Render();
                }, (string onError, long responseCode) =>
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
                                Instance.StartCoroutine(RefreshTokens());
                                return;
                            }
                            ShowLoginPopup();
                            break;
                        }
                        default:
                            EditorManager.inst.DisplayNotification($"Upload failed. Error code: {onError}", 2f, EditorManager.NotificationType.Error);
                            break;
                    }
                }, headers));
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Instance.className}There was an error while creating the ZIP file.\n{ex}");
            }
        }

        public static void ShowLoginPopup()
        {
            RTEditor.inst.ShowWarningPopup("You are not logged in.", () =>
            {
                Application.OpenURL($"{AlephNetworkManager.ArcadeServerURL}api/auth/login");
                CreateLoginListener();
                EditorManager.inst.HideDialog("Warning Popup");
            }, () => { EditorManager.inst.HideDialog("Warning Popup"); }, "Login", "Cancel");
        }

        public static IEnumerator RefreshTokens()
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
            UploadLevel();
        }

        static HttpListener _listener;

        static void CreateLoginListener()
        {
            if (_listener == null)
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://localhost:1234/");
                _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                _listener.Start();
            }
            
            Instance.StartCoroutine(StartListenerCoroutine());
        }

        static IEnumerator StartListenerCoroutine()
        {
            while (_listener.IsListening)
            {
                var task = _listener.GetContextAsync();
                yield return new WaitUntil(() => task.IsCompleted);
                ProcessRequest(task.Result);
            }
        }

        static void ProcessRequest(HttpListenerContext context)
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
        
        static void SendResponse(HttpListenerResponse response, HttpStatusCode code, string message = null)
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

        public static void RenderTags()
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
                var input = gameObject.transform.Find("input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.text = tag;
                input.onValueChanged.AddListener(_val => { moddedMetadata.LevelSong.tags[index] = _val; });

                var deleteStorage = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(() =>
                {
                    var list = moddedMetadata.LevelSong.tags.ToList();
                    list.RemoveAt(index);
                    moddedMetadata.LevelSong.tags = list.ToArray();
                    RenderTags();
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
            });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);
        }
    }
}
