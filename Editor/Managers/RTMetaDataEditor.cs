using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Components;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    public class RTMetaDataEditor : MonoBehaviour
    {
        public static RTMetaDataEditor inst;

        #region Variables

        bool uploading;

        public GameObject difficultyToggle;

        JSONObject authData;

        HttpListener _listener;

        public MetaDataEditorDialog Dialog { get; set; }

        #endregion

        #region Init

        public static void Init() => MetadataEditor.inst?.gameObject?.AddComponent<RTMetaDataEditor>();

        void Awake()
        {
            inst = this;

            if (LegacyPlugin.authData != null)
                authData = LegacyPlugin.authData;

            try
            {
                Dialog = new MetaDataEditorDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        #endregion

        #region Dialog

        /// <summary>
        /// Opens the MetaData Editor Dialog and renders it.
        /// </summary>
        public void OpenDialog() => OpenDialog(MetaData.Current);

        /// <summary>
        /// Opens the MetaData Editor Dialog and renders it.
        /// </summary>
        /// <param name="metadata">MetaData to edit.</param>
        public void OpenDialog(MetaData metadata)
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Load a level first before trying to upload!", 5f, EditorManager.NotificationType.Error);
                return;
            }

            if (EditorLevelManager.inst.CurrentLevel)
                Dialog.IconImage.sprite = EditorLevelManager.inst.CurrentLevel.icon;

            Dialog.Open();
            RenderDialog(metadata);
        }

        /// <summary>
        /// Renders the MetaData Editor Dialog.
        /// </summary>
        public void RenderDialog() => RenderDialog(MetaData.Current);

        /// <summary>
        /// Renders the MetaData Editor Dialog.
        /// </summary>
        /// <param name="metadata">MetaData to edit.</param>
        public void RenderDialog(MetaData metadata)
        {
            Debug.Log($"{MetadataEditor.inst.className}Render the Metadata Editor");

            if (!metadata)
            {
                Dialog.Close();
                EditorManager.inst.DisplayNotification("Metadata was not valid.", 1.4f, EditorManager.NotificationType.Error);
                return;
            }

            RenderArtist(metadata);
            RenderCreator(metadata);
            RenderSong(metadata);
            RenderLevel(metadata);
            RenderDifficulty(metadata);
            RenderTags(metadata);
            RenderSettings(metadata);
            RenderServer(metadata);
        }

        public void RenderArtist(MetaData metadata)
        {
            var openArtistURL = Dialog.Content.Find("artist/link/inputs/openurl").GetComponent<Button>();
            openArtistURL.onClick.ClearAll();
            openArtistURL.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(metadata.artist.URL))
                    Application.OpenURL(metadata.artist.URL);
            });

            var artistName = Dialog.Content.Find("artist/name/input").GetComponent<InputField>();
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

            var artistLink = Dialog.Content.Find("artist/link/inputs/input").GetComponent<InputField>();
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

            var artistLinkTypes = Dialog.Content.Find("artist/link/inputs/dropdown").GetComponent<Dropdown>();
            artistLinkTypes.onValueChanged.ClearAll();
            artistLinkTypes.options = AlephNetwork.ArtistLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
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
        }

        public void RenderCreator(MetaData metadata)
        {
            var uploaderName = Dialog.Content.Find("creator/uploader_name/input").GetComponent<InputField>();
            uploaderName.onValueChanged.ClearAll();
            uploaderName.text = metadata.uploaderName;
            uploaderName.onValueChanged.AddListener(_val => metadata.uploaderName = _val);

            var creatorName = Dialog.Content.Find("creator/name/input").GetComponent<InputField>();
            creatorName.onValueChanged.ClearAll();
            creatorName.text = metadata.creator.steam_name;
            creatorName.onValueChanged.AddListener(_val => metadata.creator.steam_name = _val);

            var openCreatorURL = Dialog.Content.Find("creator/link/inputs/openurl").GetComponent<Button>();
            openCreatorURL.onClick.ClearAll();
            openCreatorURL.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(metadata.creator.URL))
                    Application.OpenURL(metadata.creator.URL);
            });

            var creatorLink = Dialog.Content.Find("creator/link/inputs/input").GetComponent<InputField>();
            creatorLink.onEndEdit.ClearAll();
            creatorLink.text = metadata.creator.link;
            creatorLink.onEndEdit.AddListener(_val =>
            {
                string oldVal = metadata.creator.link;
                metadata.creator.link = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", () =>
                {
                    metadata.creator.link = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.creator.link = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            var creatorLinkTypes = Dialog.Content.Find("creator/link/inputs/dropdown").GetComponent<Dropdown>();
            creatorLinkTypes.onValueChanged.ClearAll();
            creatorLinkTypes.options = AlephNetwork.CreatorLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
            creatorLinkTypes.value = metadata.creator.linkType;
            creatorLinkTypes.onValueChanged.AddListener(_val =>
            {
                int oldVal = metadata.creator.linkType;
                metadata.creator.linkType = _val;
                EditorManager.inst.history.Add(new History.Command("Change Creator Link", () =>
                {
                    metadata.creator.linkType = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.creator.linkType = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });
        }

        public void RenderSong(MetaData metadata)
        {
            var songTitle = Dialog.Content.Find("song/title/input").GetComponent<InputField>();
            songTitle.onValueChanged.ClearAll();
            songTitle.text = metadata.song.title;
            songTitle.onValueChanged.AddListener(_val => metadata.song.title = _val);

            var openSongURL = Dialog.Content.Find("song/link/inputs/openurl").GetComponent<Button>();
            openSongURL.onClick.ClearAll();
            openSongURL.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(metadata.SongURL))
                    Application.OpenURL(metadata.SongURL);
            });

            var songLink = Dialog.Content.Find("song/link/inputs/input").GetComponent<InputField>();
            songLink.onEndEdit.ClearAll();
            songLink.text = metadata.song.link;
            songLink.onEndEdit.AddListener(_val =>
            {
                string oldVal = metadata.song.link;
                metadata.song.link = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", () =>
                {
                    metadata.song.link = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.song.link = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            var songLinkTypes = Dialog.Content.Find("song/link/inputs/dropdown").GetComponent<Dropdown>();
            songLinkTypes.onValueChanged.ClearAll();
            songLinkTypes.options = AlephNetwork.SongLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
            songLinkTypes.value = metadata.song.linkType;
            songLinkTypes.onValueChanged.AddListener(_val =>
            {
                int oldVal = metadata.song.linkType;
                metadata.song.linkType = _val;
                EditorManager.inst.history.Add(new History.Command("Change Creator Link", () =>
                {
                    metadata.song.linkType = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.song.linkType = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });
        }

        public void RenderLevel(MetaData metadata)
        {
            var levelName = Dialog.Content.Find("creator/level_name/input").GetComponent<InputField>();
            levelName.onValueChanged.ClearAll();
            levelName.text = metadata.beatmap.name;
            levelName.onValueChanged.AddListener(_val => metadata.beatmap.name = _val);

            var creatorDescription = Dialog.Content.Find("creator/description/input").GetComponent<InputField>();
            creatorDescription.onValueChanged.ClearAll();
            creatorDescription.text = metadata.song.description;
            creatorDescription.onValueChanged.AddListener(_val => metadata.song.description = _val);

        }

        public void RenderSettings(MetaData metadata)
        {
            var isHubLevel = Dialog.Content.Find("is hub level/toggle").GetComponent<Toggle>();
            isHubLevel.onValueChanged.ClearAll();
            isHubLevel.isOn = metadata.isHubLevel;
            isHubLevel.onValueChanged.AddListener(_val => metadata.isHubLevel = _val);

            var requireUnlock = Dialog.Content.Find("unlock required/toggle").GetComponent<Toggle>();
            requireUnlock.onValueChanged.ClearAll();
            requireUnlock.isOn = metadata.requireUnlock;
            requireUnlock.onValueChanged.AddListener(_val => metadata.requireUnlock = _val);

            var unlockComplete = Dialog.Content.Find("unlock complete/toggle").GetComponent<Toggle>();
            unlockComplete.onValueChanged.ClearAll();
            unlockComplete.isOn = metadata.unlockAfterCompletion;
            unlockComplete.onValueChanged.AddListener(_val => metadata.unlockAfterCompletion = _val);

            var levelData = GameData.Current?.data?.level;

            if (!levelData)
                return;

            var showIntro = Dialog.Content.Find("show intro/toggle").GetComponent<Toggle>();
            showIntro.onValueChanged.ClearAll();
            showIntro.isOn = !levelData.showIntro;
            showIntro.onValueChanged.AddListener(_val =>
            {
                if (GameData.Current && GameData.Current.data != null && GameData.Current.data.level is LevelData levelData)
                    levelData.showIntro = !_val;
            });

            var replayEndLevelOff = Dialog.Content.Find("replay end level off/toggle").GetComponent<Toggle>();
            replayEndLevelOff.onValueChanged.ClearAll();
            replayEndLevelOff.isOn = levelData.forceReplayLevelOff;
            replayEndLevelOff.onValueChanged.AddListener(_val =>
            {
                if (GameData.Current && GameData.Current.data != null && GameData.Current.data.level is LevelData levelData)
                    levelData.forceReplayLevelOff = !_val;
            });

            var preferredPlayerCount = Dialog.Content.Find("preferred player count/dropdown").GetComponent<Dropdown>();
            preferredPlayerCount.options = CoreHelper.StringToOptionData("Any", "One", "Two", "Three", "Four", "More than four");
            preferredPlayerCount.value = (int)metadata.beatmap.preferredPlayerCount;
            preferredPlayerCount.onValueChanged.AddListener(_val =>
            {
                metadata.beatmap.preferredPlayerCount = (LevelBeatmap.PreferredPlayerCount)_val;
            });

            var requireVersion = Dialog.Content.Find("require version/toggle").GetComponent<Toggle>();
            requireVersion.onValueChanged.ClearAll();
            requireVersion.isOn = metadata.requireVersion;
            requireVersion.onValueChanged.AddListener(_val => metadata.requireVersion = _val);

            var versionComparison = Dialog.Content.Find("version comparison/dropdown").GetComponent<Dropdown>();
            versionComparison.options = CoreHelper.ToOptionData<DataManager.VersionComparison>();
            versionComparison.value = (int)metadata.versionRange;
            versionComparison.onValueChanged.AddListener(_val =>
            {
                metadata.versionRange = (DataManager.VersionComparison)_val;
            });
        }

        public void RenderServer(MetaData metadata)
        {
            var serverVisibility = Dialog.Content.Find("upload/server visibility/dropdown").GetComponent<Dropdown>();
            serverVisibility.options = CoreHelper.StringToOptionData("Public", "Unlisted", "Private");
            serverVisibility.value = (int)metadata.visibility;
            serverVisibility.onValueChanged.AddListener(_val =>
            {
                metadata.visibility = (ServerVisibility)_val;
            });

            bool hasID = !string.IsNullOrEmpty(metadata.serverID); // Only check for server id.

            Dialog.Content.Find("upload/changelog").gameObject.SetActive(hasID);
            Dialog.Content.Find("upload").transform.AsRT().sizeDelta = new Vector2(738.5f, !hasID ? 60f : 200f);
            if (hasID)
            {
                var changelog = Dialog.Content.Find("upload/changelog/input").GetComponent<InputField>();
                changelog.onValueChanged.ClearAll();
                changelog.text = metadata.changelog;
                changelog.onValueChanged.AddListener(_val =>
                {
                    metadata.changelog = _val;
                });
            }

            Dialog.Content.Find("id/id").GetComponent<Text>().text = !string.IsNullOrEmpty(metadata.ID) ? $"Arcade ID: {metadata.arcadeID} (Click to copy)" : "No ID assigned.";
            var idClickable = Dialog.Content.Find("id").gameObject.GetOrAddComponent<Clickable>();
            idClickable.onClick = eventData =>
            {
                LSText.CopyToClipboard(metadata.arcadeID);
                EditorManager.inst.DisplayNotification($"Copied ID: {metadata.arcadeID} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
            };

            var serverID = Dialog.Content.Find("server id");
            serverID.gameObject.SetActive(hasID);
            if (hasID)
            {
                serverID.transform.Find("id").GetComponent<Text>().text = $"Server ID: {metadata.serverID} (Click to copy)";
                var serverIDClickable = serverID.GetComponent<Clickable>() ?? serverID.gameObject.AddComponent<Clickable>();
                serverIDClickable.onClick = eventData =>
                {
                    LSText.CopyToClipboard(metadata.serverID);
                    EditorManager.inst.DisplayNotification($"Copied ID: {metadata.serverID} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
                };
            }

            // Changed revisions to modded display.
            Dialog.Content.Find("id/revisions").GetComponent<Text>().text = $"Modded: {(GameData.Current.Modded ? "Yes" : "No")}";

            var uploadText = Dialog.Content.Find("submit/upload/Text").GetComponent<Text>();
            uploadText.text = hasID ? "Update" : "Upload";

            var submitBase = Dialog.Content.Find("submit");

            var convert = submitBase.Find("convert").GetComponent<Button>();
            convert.onClick.ClearAll();
            convert.onClick.AddListener(ConvertLevel);

            var upload = submitBase.Find("upload").GetComponent<Button>();
            upload.onClick.ClearAll();
            upload.onClick.AddListener(UploadLevel);

            var delete = submitBase.Find("delete").gameObject;
            delete.SetActive(hasID);
            if (hasID)
            {
                var deleteButton = submitBase.Find("delete").GetComponent<Button>();
                deleteButton.onClick.ClearAll();
                deleteButton.onClick.AddListener(DeleteLevel);
            }
        }

        public void RenderDifficulty(MetaData metadata)
        {
            var content = Dialog.GameObject.transform.Find("Scroll View/Viewport/Content");
            var toggles = content.Find("song/difficulty/toggles");
            LSHelpers.DeleteChildren(toggles);

            var values = CustomEnumHelper.GetValues<DifficultyType>();
            var count = values.Length - 1;

            foreach (var difficulty in values)
            {
                if (difficulty.Ordinal < 0) // skip unknown difficulty
                    continue;

                var gameObject = difficultyToggle.Duplicate(toggles, difficulty.DisplayName.ToLower(), difficulty == count - 1 ? 0 : difficulty + 1);
                gameObject.transform.localScale = Vector3.one;

                gameObject.transform.AsRT().sizeDelta = new Vector2(69f, 32f);

                var text = gameObject.transform.Find("Background/Text").GetComponent<Text>();
                text.color = LSColors.ContrastColor(difficulty.Color);
                text.text = difficulty == count - 1 ? "Anim" : difficulty.DisplayName;
                text.fontSize = 17;
                var toggle = gameObject.GetComponent<Toggle>();
                toggle.image.color = difficulty.Color;
                toggle.group = null;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = metadata.song.DifficultyType == difficulty;
                toggle.onValueChanged.AddListener(_val =>
                {
                    metadata.song.DifficultyType = difficulty;
                    RenderDifficulty(metadata);
                });

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_1);
            }
        }

        public void RenderTags(MetaData metadata)
        {
            LSHelpers.DeleteChildren(Dialog.TagsContent);

            for (int i = 0; i < metadata.song.tags.Length; i++)
            {
                int index = i;
                var tag = metadata.song.tags[i];
                var gameObject = EditorPrefabHolder.Instance.Tag.Duplicate(Dialog.TagsContent, index.ToString());
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.text = tag;
                input.onValueChanged.AddListener(_val =>
                {
                    _val = RTString.ReplaceSpace(_val);
                    var oldVal = metadata.song.tags[index];
                    metadata.song.tags[index] = _val;

                    EditorManager.inst.history.Add(new History.Command("Change MetaData Tag", () =>
                    {
                        metadata.song.tags[index] = _val;
                        MetadataEditor.inst.OpenDialog();
                    }, () =>
                    {
                        metadata.song.tags[index] = oldVal;
                        MetadataEditor.inst.OpenDialog();
                    }));
                });

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(() =>
                {
                    var list = metadata.song.tags.ToList();
                    var oldTag = list[index];
                    list.RemoveAt(index);
                    metadata.song.tags = list.ToArray();
                    RenderTags(metadata);

                    EditorManager.inst.history.Add(new History.Command("Delete MetaData Tag", () =>
                    {
                        var list = metadata.song.tags.ToList();
                        list.RemoveAt(index);
                        metadata.song.tags = list.ToArray();
                        MetadataEditor.inst.OpenDialog();
                    }, () =>
                    {
                        var list = metadata.song.tags.ToList();
                        list.Insert(index, oldTag);
                        metadata.song.tags = list.ToArray();
                        MetadataEditor.inst.OpenDialog();
                    }));
                });

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Input_Field, true);

                EditorThemeManager.ApplyInputField(input);

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);
            }

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(Dialog.TagsContent, "Add");
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Tag";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(() =>
            {
                var list = metadata.song.tags.ToList();
                list.Add("New Tag");
                metadata.song.tags = list.ToArray();
                RenderTags(metadata);

                EditorManager.inst.history.Add(new History.Command("Add MetaData Tag", () =>
                {
                    var list = metadata.song.tags.ToList();
                    list.Add("New Tag");
                    metadata.song.tags = list.ToArray();
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    var list = metadata.song.tags.ToList();
                    list.RemoveAt(list.Count - 1);
                    metadata.song.tags = list.ToArray();
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);
        }

        #endregion

        #region Functions

        public bool VerifyFile(string file) => !file.Contains("autosave") && !file.Contains("backup") && !file.Contains("level-previous") && file != Level.EDITOR_LSE && !file.Contains("waveform-") &&
            RTFile.FileIsFormat(file, FileFormat.LSB, FileFormat.LSA, FileFormat.JPG, FileFormat.PNG, FileFormat.OGG, FileFormat.WAV, FileFormat.MP3, FileFormat.MP4);

        public void SetLevelCover(Sprite sprite)
        {
            if (EditorLevelManager.inst.CurrentLevel)
            {
                EditorLevelManager.inst.CurrentLevel.icon = sprite;
                Dialog.IconImage.sprite = EditorLevelManager.inst.CurrentLevel.icon;
            }
        }

        public void VerifyLevelIsOnServer()
        {
            if (!EditorManager.inst.hasLoadedLevel || !MetaData.IsValid)
                return;

            if (uploading)
            {
                EditorManager.inst.DisplayNotification("Please wait until upload / delete process is finished!", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            var serverID = MetaData.Current.serverID;

            if (string.IsNullOrEmpty(serverID))
            {
                EditorManager.inst.DisplayNotification("Server ID was not assigned, so the level probably wasn't on the server.", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            var headers = new Dictionary<string, string>();
            if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile($"{AlephNetwork.ARCADE_SERVER_URL}api/level/{serverID}", json =>
            {
                EditorManager.inst.DisplayNotification($"Level is on server! {serverID}", 3f, EditorManager.NotificationType.Success);
            }, (string onError, long responseCode, string errorMsg) =>
            {
                switch (responseCode)
                {
                    case 404: {
                            EditorManager.inst.DisplayNotification("404 not found.", 2f, EditorManager.NotificationType.Error);
                            RTEditor.inst.ShowWarningPopup("Level was not found on the server. Do you want to remove the server ID?", () =>
                            {
                                MetaData.Current.serverID = null;
                                MetaData.Current.beatmap.date_published = "";
                                var jn = MetaData.Current.ToJSON();
                                RTFile.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB), jn.ToString());

                                RTEditor.inst.HideWarningPopup();
                            }, RTEditor.inst.HideWarningPopup);

                            return;
                        }
                    case 401: {
                            if (authData != null && authData["access_token"] != null && authData["refresh_token"] != null)
                            {
                                CoroutineHelper.StartCoroutine(RefreshTokens(VerifyLevelIsOnServer));
                                return;
                            }
                            ShowLoginPopup(VerifyLevelIsOnServer);
                            break;
                        }
                    default: {
                            EditorManager.inst.DisplayNotification($"Verify failed. Error code: {onError}", 2f, EditorManager.NotificationType.Error);
                            RTEditor.inst.ShowWarningPopup("Verification failed. In case the level is not on the server, do you want to remove the server ID?", () =>
                            {
                                MetaData.Current.serverID = null;
                                MetaData.Current.beatmap.date_published = "";
                                var jn = MetaData.Current.ToJSON();
                                RTFile.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB), jn.ToString());

                                RTEditor.inst.HideWarningPopup();
                            }, RTEditor.inst.HideWarningPopup);

                            break;
                        }
                }

                if (errorMsg != null)
                    CoreHelper.LogError($"Error Message: {errorMsg}");
            }, headers));
        }

        public void ConvertLevel()
        {
            var exportPath = EditorConfig.Instance.ConvertLevelLSToVGExportPath.Value;

            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, RTEditor.DEFAULT_EXPORTS_PATH);
                RTFile.CreateDirectory(exportPath);
            }

            exportPath = RTFile.AppendEndSlash(exportPath);

            if (!RTFile.DirectoryExists(RTFile.RemoveEndSlash(exportPath)))
            {
                EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var vg = GameData.Current.ToJSONVG();

            var metadata = MetaData.Current.ToJSONVG();

            var path = RTFile.CombinePaths(exportPath, EditorManager.inst.currentLoadedLevel);

            RTFile.CreateDirectory(path);

            var ogPath = RTFile.BasePath;

            RTFile.CopyFile(RTFile.CombinePaths(ogPath, Level.LEVEL_OGG), RTFile.CombinePaths(path, Level.AUDIO_OGG));
            RTFile.CopyFile(RTFile.CombinePaths(ogPath, Level.LEVEL_WAV), RTFile.CombinePaths(path, Level.AUDIO_WAV));
            RTFile.CopyFile(RTFile.CombinePaths(ogPath, Level.LEVEL_MP3), RTFile.CombinePaths(path, Level.AUDIO_MP3));
            RTFile.CopyFile(RTFile.CombinePaths(ogPath, Level.LEVEL_JPG), RTFile.CombinePaths(path, Level.COVER_JPG));

            try
            {
                RTFile.WriteToFile(RTFile.CombinePaths(path, Level.METADATA_VGM), metadata.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"{MetadataEditor.inst.className}Convert to VG error (MetaData) {ex}");
                EditorManager.inst.DisplayNotification($"Convert to VG error (MetaData)", 4f, EditorManager.NotificationType.Error);
            }

            try
            {
                RTFile.WriteToFile(RTFile.CombinePaths(path, Level.LEVEL_VGD), vg.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"{MetadataEditor.inst.className}Convert to VG error (GameData) {ex}");
                EditorManager.inst.DisplayNotification($"Convert to VG error (GameData)", 4f, EditorManager.NotificationType.Error);
            }

            EditorManager.inst.DisplayNotification($"Converted Level \"{EditorManager.inst.currentLoadedLevel}\" from LS format to VG format and saved to {Path.GetFileName(path)}.", 4f,
                EditorManager.NotificationType.Success);

            AchievementManager.inst.UnlockAchievement("time_machine");
        }

        public void UploadLevel()
        {
            if (uploading)
            {
                EditorManager.inst.DisplayNotification("Please wait until upload / delete process is finished!", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            uploading = true;

            EditorManager.inst.DisplayNotification("Attempting to upload to the server... please wait.", 3f, EditorManager.NotificationType.Warning);

            var exportPath = EditorConfig.Instance.ZIPLevelExportPath.Value;

            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, RTEditor.DEFAULT_EXPORTS_PATH);
                RTFile.CreateDirectory(exportPath);
            }

            exportPath = RTFile.AppendEndSlash(exportPath);

            if (!RTFile.DirectoryExists(RTFile.RemoveEndSlash(exportPath)))
            {
                EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var path = RTFile.CombinePaths(exportPath, EditorManager.inst.currentLoadedLevel + "-server-upload.zip");

            try
            {
                MetaData.Current.beatmap.date_published = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
                MetaData.Current.beatmap.version_number++;
                if (authData != null && authData["id"] != null)
                    MetaData.Current.uploaderID = authData["id"];

                var jn = MetaData.Current.ToJSON();
                RTFile.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB), jn.ToString());

                RTFile.DeleteFile(path);

                // here we setup a temporary upload folder that has no editor files, which we then zip and delete the directory.
                var tempDirectory = RTFile.CombinePaths(exportPath, EditorManager.inst.currentLoadedLevel + "-temp/");
                RTFile.CreateDirectory(tempDirectory);
                var directory = RTFile.BasePath;
                var files = Directory.GetFiles(directory);
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    if (!VerifyFile(Path.GetFileName(file)))
                        continue;

                    var copyTo = file.Replace(directory, tempDirectory);

                    var dir = RTFile.GetDirectory(copyTo);

                    RTFile.CreateDirectory(dir);
                    RTFile.CopyFile(file, copyTo);
                }

                ZipFile.CreateFromDirectory(tempDirectory, path);
                RTFile.DeleteDirectory(tempDirectory);

                var headers = new Dictionary<string, string>();
                if (authData != null && authData["access_token"] != null)
                    headers["Authorization"] = $"Bearer {authData["access_token"].Value}";

                CoroutineHelper.StartCoroutine(AlephNetwork.UploadBytes($"{AlephNetwork.ARCADE_SERVER_URL}api/level", File.ReadAllBytes(path), id =>
                {
                    uploading = false;
                    MetaData.Current.serverID = id;

                    var jn = MetaData.Current.ToJSON();
                    RTFile.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB), jn.ToString());
                    RTFile.DeleteFile(path);

                    EditorManager.inst.DisplayNotification($"Level uploaded! ID: {id}", 3f, EditorManager.NotificationType.Success);
                    RenderDialog();

                    AchievementManager.inst.UnlockAchievement("upload_level");
                }, (string onError, long responseCode, string errorMsg) =>
                {
                    uploading = false;
                    // Only downgrade if server ID wasn't already assigned.
                    if (string.IsNullOrEmpty(MetaData.Current.serverID))
                    {
                        MetaData.Current.uploaderID = null;
                        MetaData.Current.beatmap.date_published = "";
                        MetaData.Current.beatmap.version_number--;
                        var jn = MetaData.Current.ToJSON();
                        RTFile.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB), jn.ToString());
                    }

                    RTFile.DeleteFile(path);

                    switch (responseCode)
                    {
                        case 404: {
                                EditorManager.inst.DisplayNotification("404 not found.", 2f, EditorManager.NotificationType.Error);
                                return;
                            }
                        case 401: {
                                if (authData != null && authData["access_token"] != null && authData["refresh_token"] != null)
                                {
                                    CoroutineHelper.StartCoroutine(RefreshTokens(UploadLevel));
                                    return;
                                }
                                ShowLoginPopup(UploadLevel);
                                break;
                            }
                        default: {
                                EditorManager.inst.DisplayNotification($"Upload failed. Error code: {onError}", 2f, EditorManager.NotificationType.Error);
                                break;
                            }
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
            if (uploading)
            {
                EditorManager.inst.DisplayNotification("Please wait until upload / delete process is finished!", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            uploading = true;

            RTEditor.inst.ShowWarningPopup("Are you sure you want to remove this level from the Arcade server? This cannot be undone!", () =>
            {
                try
                {
                    EditorManager.inst.DisplayNotification("Attempting to delete level from the server... please wait.", 3f, EditorManager.NotificationType.Warning);

                    var id = MetaData.Current.serverID;

                    var headers = new Dictionary<string, string>();
                    if (authData != null && authData["access_token"] != null)
                        headers["Authorization"] = $"Bearer {authData["access_token"].Value}";

                    CoroutineHelper.StartCoroutine(AlephNetwork.Delete($"{AlephNetwork.ARCADE_SERVER_URL}api/level/{id}", () =>
                    {
                        uploading = false;
                        MetaData.Current.beatmap.date_published = "";
                        MetaData.Current.serverID = null;
                        var jn = MetaData.Current.ToJSON();
                        RTFile.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB), jn.ToString());

                        EditorManager.inst.DisplayNotification($"Successfully deleted level off the Arcade server.", 2.5f, EditorManager.NotificationType.Success);
                        RenderDialog();
                        RTEditor.inst.HideWarningPopup();
                    }, (string onError, long responseCode) =>
                    {
                        uploading = false;
                        switch (responseCode)
                        {
                            case 404: {
                                    EditorManager.inst.DisplayNotification("404 not found.", 2f, EditorManager.NotificationType.Error);
                                    RTEditor.inst.HideWarningPopup();
                                    return;
                                }
                            case 401: {
                                    if (authData != null && authData["access_token"] != null && authData["refresh_token"] != null)
                                    {
                                        CoroutineHelper.StartCoroutine(RefreshTokens(DeleteLevel));
                                        return;
                                    }
                                    ShowLoginPopup(DeleteLevel);
                                    break;
                                }
                            default: {
                                    EditorManager.inst.DisplayNotification($"Delete failed. Error code: {onError}", 2f, EditorManager.NotificationType.Error);
                                    RTEditor.inst.HideWarningPopup();
                                    break;
                                }
                        }
                    }, headers));
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Had an exception in deleting the level.\nException: {ex}");
                }
            }, RTEditor.inst.HideWarningPopup);
        }

        public void PullLevel()
        {
            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Cannot pull level from the Arcade server because the level is saving!", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            var serverID = MetaData.Current.serverID;

            if (string.IsNullOrEmpty(serverID))
            {
                EditorManager.inst.DisplayNotification("Server ID was not assigned, so the level probably wasn't on the server.", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            var headers = new Dictionary<string, string>();
            if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile($"{AlephNetwork.ARCADE_SERVER_URL}api/level/{serverID}", json =>
            {
                var jn = JSON.Parse(json);

                if (GameData.Current)
                    GameData.Current.SaveData(RTFile.CombinePaths(EditorLevelManager.inst.CurrentLevel.path, "reload-level-backup.lsb"));

                UploadedLevelsManager.inst.DownloadLevel(jn["id"], RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path), jn["name"], EditorLevelManager.inst.ILoadLevel(EditorLevelManager.inst.CurrentLevel).Start);
            }, (string onError, long responseCode, string errorMsg) =>
            {
                switch (responseCode)
                {
                    case 404: {
                            EditorManager.inst.DisplayNotification("404 not found.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }
                    case 401: {
                            if (authData != null && authData["access_token"] != null && authData["refresh_token"] != null)
                            {
                                CoroutineHelper.StartCoroutine(RefreshTokens(PullLevel));
                                return;
                            }
                            ShowLoginPopup(PullLevel);
                            break;
                        }
                    default: {
                            EditorManager.inst.DisplayNotification($"Pull failed. Error code: {onError}", 2f, EditorManager.NotificationType.Error);
                            break;
                        }
                }

                if (errorMsg != null)
                    CoreHelper.LogError($"Error Message: {errorMsg}");
            }, headers));
        }

        #endregion

        #region Login

        public void ShowLoginPopup(Action onLogin)
        {
            RTEditor.inst.ShowWarningPopup("You are not logged in.", () =>
            {
                Application.OpenURL($"{AlephNetwork.ARCADE_SERVER_URL}api/auth/login");
                CreateLoginListener(onLogin);
                RTEditor.inst.HideWarningPopup();
            }, RTEditor.inst.HideWarningPopup, "Login", "Cancel");
        }

        public IEnumerator RefreshTokens(Action onRefreshed)
        {
            EditorManager.inst.DisplayNotification("Access token expired. Refreshing...", 5f, EditorManager.NotificationType.Warning);

            var form = new WWWForm();
            form.AddField("AccessToken", authData["access_token"].Value);
            form.AddField("RefreshToken", authData["refresh_token"].Value);

            using var www = UnityWebRequest.Post($"{AlephNetwork.ARCADE_SERVER_URL}api/auth/refresh", form);
            www.certificateHandler = new AlephNetwork.ForceAcceptAll();
            yield return www.SendWebRequest();

            if (www.isNetworkError)
            {
                EditorManager.inst.DisplayNotification($"Login failed due to a network error. Message: {www.error}", 5f, EditorManager.NotificationType.Error);
                if (www.downloadHandler != null)
                    CoreHelper.Log(www.downloadHandler.text);
                yield break;
            }

            if (www.isHttpError)
            {
                EditorManager.inst.DisplayNotification($"Login failed due to a HTTP error. Message: {www.error}", 5f, EditorManager.NotificationType.Error);
                if (www.downloadHandler != null)
                    CoreHelper.Log(www.downloadHandler.text);
                ShowLoginPopup(onRefreshed);
                yield break;
            }

            var jn = JSON.Parse(www.downloadHandler.text);
            authData["access_token"] = jn["accessToken"].Value;
            authData["refresh_token"] = jn["refreshToken"].Value;
            authData["access_token_expiry_time"] = jn["accessTokenExpiryTime"].Value;

            RTFile.WriteToFile(Path.Combine(Application.persistentDataPath, "auth.json"), authData.ToString());
            EditorManager.inst.DisplayNotification("Refreshed tokens! Uploading...", 5f, EditorManager.NotificationType.Success);
            if (EditorConfig.Instance.UploadDeleteOnLogin.Value)
                onRefreshed?.Invoke();
        }

        void CreateLoginListener(Action onLogin)
        {
            if (_listener == null)
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://localhost:1234/");
                _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                _listener.Start();
            }

            CoroutineHelper.StartCoroutine(StartListenerCoroutine(onLogin));
        }

        IEnumerator StartListenerCoroutine(Action onLogin)
        {
            while (_listener.IsListening)
            {
                var task = _listener.GetContextAsync();
                yield return CoroutineHelper.Until(() => task.IsCompleted);
                ProcessRequest(task.Result, onLogin);
            }
        }

        void ProcessRequest(HttpListenerContext context, Action onLogin)
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
            LegacyPlugin.authData = authData;

            RTFile.WriteToFile(Path.Combine(Application.persistentDataPath, "auth.json"), authData.ToString());
            EditorManager.inst.DisplayNotification($"Successfully logged in as {username}!", 8f, EditorManager.NotificationType.Success);
            SendResponse(context.Response, HttpStatusCode.OK, "Success! You can close this page and go back to the game now.");

            if (EditorConfig.Instance.UploadDeleteOnLogin.Value)
                onLogin?.Invoke();
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

        #endregion
    }
}
