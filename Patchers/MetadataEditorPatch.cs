using BetterLegacy.Components;
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
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(MetadataEditor))]
    public class MetadataEditorPatch : MonoBehaviour
    {
        static MetadataEditor Instance { get => MetadataEditor.inst; set => MetadataEditor.inst = value; }

        static GameObject difficultyToggle;

        static GameObject tagPrefab;

        [HarmonyPatch("Awake")]
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

                var openLinkRT = (RectTransform)openLink.transform;
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

                var mask = tagScrollView.AddComponent<Mask>();

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
                toggle.onValueChanged.AddListener(delegate (bool _val)
                {
                    DataManager.inst.metaData.song.difficulty = index;
                    SetToggleList();
                });

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_1);

                num++;
            }
        }

        [HarmonyPatch("Render")]
        [HarmonyPrefix]
        static bool Render()
        {
            Debug.Log($"{Instance.className}Render the Metadata Editor");

            var content = EditorManager.inst.GetDialog("Metadata Editor").Dialog.Find("Scroll View/Viewport/Content");

            ((RectTransform)content.Find("spacer (1)")).sizeDelta = new Vector2(732f, 80f);

            var openArtistURL = content.Find("artist/link/inputs/openurl").GetComponent<Button>();
            openArtistURL.onClick.ClearAll();
            openArtistURL.onClick.AddListener(delegate ()
            {
                Application.OpenURL(DataManager.inst.metaData.artist.getUrl());
            });

            var artistName = content.Find("artist/name/input").GetComponent<InputField>();
            artistName.onEndEdit.RemoveAllListeners();
            artistName.text = DataManager.inst.metaData.artist.Name;
            artistName.onEndEdit.AddListener(delegate (string _val)
            {
                string oldVal = DataManager.inst.metaData.artist.Name;
                DataManager.inst.metaData.artist.Name = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Name", delegate ()
                {
                    DataManager.inst.metaData.artist.Name = _val;
                    Instance.Render();
                }, delegate ()
                {
                    DataManager.inst.metaData.artist.Name = oldVal;
                    Instance.Render();
                }), false);
            });

            var artistLink = content.Find("artist/link/inputs/input").GetComponent<InputField>();
            artistLink.onEndEdit.RemoveAllListeners();
            artistLink.text = DataManager.inst.metaData.artist.Link;
            artistLink.onEndEdit.AddListener(delegate (string _val)
            {
                string oldVal = DataManager.inst.metaData.artist.Link;
                DataManager.inst.metaData.artist.Link = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", delegate ()
                {
                    DataManager.inst.metaData.artist.Link = _val;
                    Instance.Render();
                }, delegate ()
                {
                    DataManager.inst.metaData.artist.Link = oldVal;
                    Instance.Render();
                }), false);
            });

            var artistLinkTypes = content.Find("artist/link/inputs/dropdown").GetComponent<Dropdown>();
            artistLinkTypes.options.Clear();
            artistLinkTypes.onValueChanged.RemoveAllListeners();
            artistLinkTypes.options = DataManager.inst.linkTypes.Select(x => new Dropdown.OptionData(x.name)).ToList();
            artistLinkTypes.value = DataManager.inst.metaData.artist.LinkType;
            artistLinkTypes.onValueChanged.AddListener(delegate (int _val)
            {
                int oldVal = DataManager.inst.metaData.artist.LinkType;
                DataManager.inst.metaData.artist.LinkType = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", delegate ()
                {
                    DataManager.inst.metaData.artist.LinkType = _val;
                    Instance.Render();
                }, delegate ()
                {
                    DataManager.inst.metaData.artist.LinkType = oldVal;
                    Instance.Render();
                }), false);
            });

            var creatorName = content.Find("creator/name/input").GetComponent<InputField>();
            creatorName.onValueChanged.RemoveAllListeners();
            creatorName.text = DataManager.inst.metaData.creator.steam_name;
            creatorName.onValueChanged.AddListener(delegate (string _val)
            {
                DataManager.inst.metaData.creator.steam_name = _val;
            });

            var moddedMetadata = MetaData.Current;

            var levelName = content.Find("creator/level_name/input").GetComponent<InputField>();
            levelName.onValueChanged.RemoveAllListeners();
            levelName.text = moddedMetadata.LevelBeatmap.name;
            levelName.onValueChanged.AddListener(delegate (string _val)
            {
                moddedMetadata.LevelBeatmap.name = _val;
            });

            var songTitle = content.Find("song/title/input").GetComponent<InputField>();
            songTitle.onValueChanged.RemoveAllListeners();
            songTitle.text = DataManager.inst.metaData.song.title;
            songTitle.onValueChanged.AddListener(delegate (string _val)
            {
                DataManager.inst.metaData.song.title = _val;
            });

            var openCreatorURL = content.Find("creator/link/inputs/openurl").GetComponent<Button>();
            openCreatorURL.onClick.ClearAll();
            openCreatorURL.onClick.AddListener(delegate ()
            {
                if (moddedMetadata.LevelCreator.URL != null)
                    Application.OpenURL(moddedMetadata.LevelCreator.URL);
            });

            var creatorLink = content.Find("creator/link/inputs/input").GetComponent<InputField>();
            creatorLink.onEndEdit.RemoveAllListeners();
            creatorLink.text = moddedMetadata.LevelCreator.link;
            creatorLink.onEndEdit.AddListener(delegate (string _val)
            {
                string oldVal = moddedMetadata.LevelCreator.link;
                moddedMetadata.LevelCreator.link = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", delegate ()
                {
                    moddedMetadata.LevelCreator.link = _val;
                    Instance.Render();
                }, delegate ()
                {
                    moddedMetadata.LevelCreator.link = oldVal;
                    Instance.Render();
                }), false);
            });

            var creatorLinkTypes = content.Find("creator/link/inputs/dropdown").GetComponent<Dropdown>();
            creatorLinkTypes.options.Clear();
            creatorLinkTypes.onValueChanged.RemoveAllListeners();
            creatorLinkTypes.options = LevelCreator.creatorLinkTypes.Select(x => new Dropdown.OptionData(x.name)).ToList();
            creatorLinkTypes.value = moddedMetadata.LevelCreator.linkType;
            creatorLinkTypes.onValueChanged.AddListener(delegate (int _val)
            {
                int oldVal = moddedMetadata.LevelCreator.linkType;
                moddedMetadata.LevelCreator.linkType = _val;
                EditorManager.inst.history.Add(new History.Command("Change Creator Link", delegate ()
                {
                    moddedMetadata.LevelCreator.linkType = _val;
                    Instance.Render();
                }, delegate ()
                {
                    moddedMetadata.LevelCreator.linkType = oldVal;
                    Instance.Render();
                }), false);
            });

            var openSongURL = content.Find("song/link/inputs/openurl").GetComponent<Button>();
            openSongURL.onClick.ClearAll();
            openSongURL.onClick.AddListener(delegate ()
            {
                if (!string.IsNullOrEmpty(moddedMetadata.LevelSong.link))
                    Application.OpenURL(moddedMetadata.SongURL);
            });

            var songLink = content.Find("song/link/inputs/input").GetComponent<InputField>();
            songLink.onEndEdit.RemoveAllListeners();
            songLink.text = moddedMetadata.LevelSong.link;
            songLink.onEndEdit.AddListener(delegate (string _val)
            {
                string oldVal = moddedMetadata.LevelSong.link;
                moddedMetadata.LevelSong.link = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", delegate ()
                {
                    moddedMetadata.LevelSong.link = _val;
                    Instance.Render();
                }, delegate ()
                {
                    moddedMetadata.LevelSong.link = oldVal;
                    Instance.Render();
                }), false);
            });

            var songLinkTypes = content.Find("song/link/inputs/dropdown").GetComponent<Dropdown>();
            songLinkTypes.options.Clear();
            songLinkTypes.onValueChanged.RemoveAllListeners();
            songLinkTypes.options = CoreHelper.InstanceLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
            songLinkTypes.value = moddedMetadata.LevelSong.linkType;
            songLinkTypes.onValueChanged.AddListener(delegate (int _val)
            {
                int oldVal = moddedMetadata.LevelSong.linkType;
                moddedMetadata.LevelSong.linkType = _val;
                EditorManager.inst.history.Add(new History.Command("Change Creator Link", delegate ()
                {
                    moddedMetadata.LevelSong.linkType = _val;
                    Instance.Render();
                }, delegate ()
                {
                    moddedMetadata.LevelSong.linkType = oldVal;
                    Instance.Render();
                }), false);
            });

            ((RectTransform)content.Find("creator/description")).sizeDelta = new Vector2(757f, 140f);
            ((RectTransform)content.Find("creator/description/input")).sizeDelta = new Vector2(523f, 140f);
            var creatorDescription = content.Find("creator/description/input").GetComponent<InputField>();
            creatorDescription.onValueChanged.RemoveAllListeners();
            creatorDescription.text = DataManager.inst.metaData.song.description;
            creatorDescription.onValueChanged.AddListener(delegate (string _val)
            {
                DataManager.inst.metaData.song.description = _val;
            });

            SetToggleList();

            RenderTags();
            content.Find("agreement/text").GetComponent<Text>().text = "If you want to upload to the Steam Workshop, you can convert the level to the current level format for vanilla PA and upload it to the workshop. Beware any modded features not in current PA will not be saved. " +
                "However, if you want to include modded features, then it's recommended to upload to the arcade server or zip the level.";
            //for (int i = 5; i < 9; i++)
            //{
            //    content.GetChild(i).gameObject.SetActive(false);
            //}

            bool hasID = !string.IsNullOrEmpty(moddedMetadata.ID);
            content.Find("id/id").GetComponent<Text>().text = hasID ? $"ID: {moddedMetadata.ID} (Click this text to copy)" : "No ID assigned.";
            var idClickable = content.Find("id").GetComponent<Clickable>() ?? content.Find("id").gameObject.AddComponent<Clickable>();
            idClickable.onClick = delegate (PointerEventData eventData)
            {
                LSText.CopyToClipboard(moddedMetadata.ID);
                EditorManager.inst.DisplayNotification($"Copied ID: {moddedMetadata.ID} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
            };

            content.Find("id/revisions").gameObject.SetActive(hasID);
            if (hasID)
                content.Find("id/revisions").GetComponent<Text>().text = "Version: " + DataManager.inst.metaData.beatmap.version_number.ToString();

            var uploadText = content.Find("submit/upload/Text").GetComponent<Text>();
            uploadText.text = hasID ? "Update" : "Upload";

            var submitBase = content.Find("submit");

            content.Find("spacer").gameObject.SetActive(true);
            submitBase.gameObject.SetActive(true);

            var convert = submitBase.Find("convert").GetComponent<Button>();
            convert.onClick.ClearAll();
            convert.onClick.AddListener(delegate ()
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

            bool active = false;

            var upload = submitBase.Find("upload").GetComponent<Button>();
            upload.onClick.ClearAll();
            upload.onClick.AddListener(delegate ()
            {
                if (!active)
                {
                    EditorManager.inst.DisplayNotification("Not implemented yet!", 2f, EditorManager.NotificationType.Warning);
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

                    var jn = MetaData.Current.ToJSON();
                    RTFile.WriteToFile(GameManager.inst.basePath + "metadata.lsb", jn.ToString());

                    if (RTFile.FileExists(path))
                        File.Delete(path);

                    ZipFile.CreateFromDirectory(GameManager.inst.basePath, path);

                    Instance.StartCoroutine(AlephNetworkManager.UploadBytes("", File.ReadAllBytes(path), delegate (string id)
                    {
                        MetaData.Current.serverID = id;
                        MetaData.Current.beatmap.version_number++;

                        var jn = MetaData.Current.ToJSON();
                        RTFile.WriteToFile(GameManager.inst.basePath + "metadata.lsb", jn.ToString());

                        if (RTFile.FileExists(path))
                            File.Delete(path);

                        Instance.Render();

                    }, delegate (string onError)
                    {
                        MetaData.Current.LevelBeatmap.date_published = "";
                        var jn = MetaData.Current.ToJSON();
                        RTFile.WriteToFile(GameManager.inst.basePath + "metadata.lsb", jn.ToString());

                        EditorManager.inst?.DisplayNotification("Upload failed.", 2f, EditorManager.NotificationType.Error);
                    }));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{Instance.className}There was an error in creating the ZIP file.\n{ex}");
                }
            });

            var zip = submitBase.Find("zip").GetComponent<Button>();
            zip.onClick.ClearAll();
            zip.onClick.AddListener(delegate ()
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
                var gameObject = tagPrefab.Duplicate(parent, index.ToString());
                var input = gameObject.transform.Find("input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.text = tag;
                input.onValueChanged.AddListener(delegate (string _val)
                {
                    moddedMetadata.LevelSong.tags[index] = _val;
                });

                var deleteStorage = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(delegate ()
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
            addButton.onClick.AddListener(delegate ()
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
