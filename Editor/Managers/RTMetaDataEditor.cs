using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;
using Crosstales.FB;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    public class RTMetaDataEditor : MonoBehaviour
    {
        public static RTMetaDataEditor inst;

        #region Variables

        bool uploading;

        public GameObject difficultyToggle;
        
        HttpListener _listener;

        public MetaDataEditorDialog Dialog { get; set; }

        public ContentPopup TagPopup { get; set; }

        public bool CollapseIcon { get; set; } = true;

        #endregion

        #region Init

        public static void Init() => MetadataEditor.inst?.gameObject?.AddComponent<RTMetaDataEditor>();

        void Awake()
        {
            inst = this;

            try
            {
                Dialog = new MetaDataEditorDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog

            try
            {
                TagPopup = RTEditor.inst.GeneratePopup(EditorPopup.DEFAULT_TAGS_POPUP, "Add a default tag",
                    refreshSearch: _val => { });
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

            if (!Dialog || !Dialog.Setup)
                return;

            if (EditorLevelManager.inst.CurrentLevel && Dialog.IconImage)
                Dialog.IconImage.sprite = EditorLevelManager.inst.CurrentLevel.icon;

            Dialog.CollapseIcon(CollapseIcon);
            Dialog.SelectIconButton.onClick.NewListener(OpenIconSelector);
            Dialog.CollapseToggle.SetIsOnWithoutNotify(CollapseIcon);
            Dialog.CollapseToggle.onValueChanged.NewListener(_val =>
            {
                Dialog.CollapseIcon(_val);
                CollapseIcon = _val;
            });

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
            Dialog.ArtistNameField.SetTextWithoutNotify(metadata.artist.name);
            Dialog.ArtistNameField.onEndEdit.NewListener(_val =>
            {
                string oldVal = metadata.artist.name;
                metadata.artist.name = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Name", () =>
                {
                    metadata.artist.name = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.artist.name = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            Dialog.OpenArtistURLButton.onClick.NewListener(() =>
            {
                if (!string.IsNullOrEmpty(metadata.artist.URL))
                    Application.OpenURL(metadata.artist.URL);
            });

            Dialog.ArtistLinkField.SetTextWithoutNotify(metadata.artist.link);
            Dialog.ArtistLinkField.onEndEdit.NewListener(_val =>
            {
                string oldVal = metadata.artist.link;
                metadata.artist.link = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", () =>
                {
                    metadata.artist.link = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.artist.link = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            Dialog.ArtistLinkTypeDropdown.options = AlephNetwork.ArtistLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
            Dialog.ArtistLinkTypeDropdown.SetValueWithoutNotify(metadata.artist.linkType);
            Dialog.ArtistLinkTypeDropdown.onValueChanged.NewListener(_val =>
            {
                int oldVal = metadata.artist.linkType;
                metadata.artist.linkType = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", () =>
                {
                    metadata.artist.linkType = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.artist.linkType = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });
        }

        public void RenderCreator(MetaData metadata)
        {
            if (Dialog.UploaderNameField)
            {
                Dialog.UploaderNameField.SetTextWithoutNotify(metadata.uploaderName);
                Dialog.UploaderNameField.onValueChanged.NewListener(_val => metadata.uploaderName = _val);
            }

            Dialog.CreatorNameField.SetTextWithoutNotify(metadata.creator.name);
            Dialog.CreatorNameField.onValueChanged.NewListener(_val => metadata.creator.name = _val);

            Dialog.OpenCreatorURLButton.onClick.NewListener(() =>
            {
                if (!string.IsNullOrEmpty(metadata.creator.URL))
                    Application.OpenURL(metadata.creator.URL);
            });

            Dialog.CreatorLinkField.SetTextWithoutNotify(metadata.creator.link);
            Dialog.CreatorLinkField.onEndEdit.NewListener(_val =>
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

            Dialog.CreatorLinkTypeDropdown.options = AlephNetwork.CreatorLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
            Dialog.CreatorLinkTypeDropdown.SetValueWithoutNotify(metadata.creator.linkType);
            Dialog.CreatorLinkTypeDropdown.onValueChanged.NewListener(_val =>
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
            Dialog.SongTitleField.SetTextWithoutNotify(metadata.song.title);
            Dialog.SongTitleField.onValueChanged.NewListener(_val => metadata.song.title = _val);

            Dialog.OpenSongURLButton.onClick.NewListener(() =>
            {
                if (!string.IsNullOrEmpty(metadata.SongURL))
                    Application.OpenURL(metadata.SongURL);
            });

            Dialog.SongLinkField.SetTextWithoutNotify(metadata.song.link);
            Dialog.SongLinkField.onEndEdit.NewListener(_val =>
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

            Dialog.SongLinkTypeDropdown.options = AlephNetwork.SongLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
            Dialog.SongLinkTypeDropdown.SetValueWithoutNotify(metadata.song.linkType);
            Dialog.SongLinkTypeDropdown.onValueChanged.NewListener(_val =>
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
            Dialog.LevelNameField.SetTextWithoutNotify(metadata.beatmap.name);
            Dialog.LevelNameField.onValueChanged.NewListener(_val => metadata.beatmap.name = _val);

            Dialog.DescriptionField.SetTextWithoutNotify(metadata.song.description);
            Dialog.DescriptionField.onValueChanged.NewListener(_val => metadata.song.description = _val);
        }

        public void RenderSettings(MetaData metadata)
        {
            Dialog.IsHubLevelToggle.SetIsOnWithoutNotify(metadata.isHubLevel);
            Dialog.IsHubLevelToggle.onValueChanged.NewListener(_val => metadata.isHubLevel = _val);

            Dialog.UnlockRequiredToggle.SetIsOnWithoutNotify(metadata.requireUnlock);
            Dialog.UnlockRequiredToggle.onValueChanged.NewListener(_val => metadata.requireUnlock = _val);

            Dialog.UnlockCompletedToggle.SetIsOnWithoutNotify(metadata.unlockAfterCompletion);
            Dialog.UnlockCompletedToggle.onValueChanged.NewListener(_val => metadata.unlockAfterCompletion = _val);

            var levelData = GameData.Current?.data?.level;
            if (!levelData)
                return;

            Dialog.HideIntroToggle.SetIsOnWithoutNotify(levelData.hideIntro);
            Dialog.HideIntroToggle.onValueChanged.NewListener(_val =>
            {
                if (GameData.Current && GameData.Current.data && GameData.Current.data.level is LevelData levelData)
                    levelData.hideIntro = _val;
            });

            Dialog.ReplayEndLevelOffToggle.SetIsOnWithoutNotify(levelData.forceReplayLevelOff);
            Dialog.ReplayEndLevelOffToggle.onValueChanged.NewListener(_val =>
            {
                if (GameData.Current && GameData.Current.data && GameData.Current.data.level is LevelData levelData)
                    levelData.forceReplayLevelOff = _val;
            });

            Dialog.PreferredPlayerCountDropdown.SetValueWithoutNotify((int)metadata.beatmap.preferredPlayerCount);
            Dialog.PreferredPlayerCountDropdown.onValueChanged.NewListener(_val => metadata.beatmap.preferredPlayerCount = (BeatmapMetaData.PreferredPlayerCount)_val);
            
            Dialog.PreferredControlTypeDropdown.SetValueWithoutNotify((int)metadata.beatmap.preferredControlType);
            Dialog.PreferredControlTypeDropdown.onValueChanged.NewListener(_val => metadata.beatmap.preferredControlType = (BeatmapMetaData.PreferredControlType)_val);

            Dialog.RequireVersion.SetIsOnWithoutNotify(metadata.requireVersion);
            Dialog.RequireVersion.onValueChanged.NewListener(_val => metadata.requireVersion = _val);

            Dialog.VersionComparison.options = CoreHelper.ToOptionData<DataManager.VersionComparison>();
            Dialog.VersionComparison.SetValueWithoutNotify((int)metadata.versionRange);
            Dialog.VersionComparison.onValueChanged.NewListener(_val => metadata.versionRange = (DataManager.VersionComparison)_val);
        }

        public void RenderServer(MetaData metadata)
        {
            Dialog.ServerVisibilityDropdown.options = CoreHelper.StringToOptionData("Public", "Unlisted", "Private");
            Dialog.ServerVisibilityDropdown.SetValueWithoutNotify((int)metadata.visibility);
            Dialog.ServerVisibilityDropdown.onValueChanged.NewListener(_val => metadata.visibility = (ServerVisibility)_val);

            CoreHelper.DestroyChildren(Dialog.CollaboratorsContent);
            for (int i = 0; i < metadata.uploaders.Count; i++)
            {
                int index = i;
                var tag = metadata.uploaders[i];
                var gameObject = EditorPrefabHolder.Instance.Tag.Duplicate(Dialog.CollaboratorsContent, index.ToString());
                gameObject.transform.AsRT().sizeDelta = new Vector2(717f, 32f);
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.transform.AsRT().sizeDelta = new Vector2(682, 32f);
                input.SetTextWithoutNotify(tag);
                input.onValueChanged.NewListener(_val =>
                {
                    _val = RTString.ReplaceSpace(_val);
                    var oldVal = metadata.uploaders[index];
                    metadata.uploaders[index] = _val;

                    EditorManager.inst.history.Add(new History.Command("Change MetaData Uploader", () =>
                    {
                        metadata.uploaders[index] = _val;
                        MetadataEditor.inst.OpenDialog();
                    }, () =>
                    {
                        metadata.uploaders[index] = oldVal;
                        MetadataEditor.inst.OpenDialog();
                    }));
                });

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.NewListener(() =>
                {
                    var oldTag = metadata.uploaders[index];
                    metadata.uploaders.RemoveAt(index);
                    RenderServer(metadata);

                    EditorManager.inst.history.Add(new History.Command("Delete MetaData Tag", () =>
                    {
                        if (metadata.uploaders == null)
                            return;
                        metadata.uploaders.RemoveAt(index);
                        MetadataEditor.inst.OpenDialog();
                    }, () =>
                    {
                        if (metadata.uploaders == null)
                            metadata.uploaders = new List<string>();
                        metadata.uploaders.Insert(index, oldTag);
                        MetadataEditor.inst.OpenDialog();
                    }));
                });

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Input_Field, true);

                EditorThemeManager.ApplyInputField(input);

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);
            }

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(Dialog.CollaboratorsContent, "Add");
            add.transform.AsRT().sizeDelta = new Vector2(717f, 32f);
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Collaborator";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            var contextClickable = add.GetOrAddComponent<ContextClickable>();
            contextClickable.onClick = pointerEventData =>
            {
                if (metadata.uploaders == null)
                    metadata.uploaders = new List<string>();
                metadata.uploaders.Add(string.Empty);
                RenderServer(metadata);

                EditorManager.inst.history.Add(new History.Command("Add MetaData Collaborator",
                    () =>
                    {
                        if (metadata.uploaders == null)
                            metadata.uploaders = new List<string>();
                        metadata.uploaders.Add(string.Empty);
                        MetadataEditor.inst.OpenDialog();
                    },
                    () =>
                    {
                        if (metadata.uploaders == null)
                            return;
                        metadata.uploaders.RemoveAt(metadata.uploaders.Count - 1);
                        MetadataEditor.inst.OpenDialog();
                    }));
            };

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);

            bool hasID = !string.IsNullOrEmpty(metadata.serverID); // Only check for server id.

            Dialog.ShowChangelog(hasID);
            if (hasID)
            {
                Dialog.ChangelogField.SetTextWithoutNotify(metadata.changelog);
                Dialog.ChangelogField.onValueChanged.NewListener(_val => metadata.changelog = _val);
            }

            Dialog.ArcadeIDText.text = !string.IsNullOrEmpty(metadata.ID) ? $"Arcade ID: {metadata.arcadeID} (Click to copy)" : "Arcade ID: No ID";
            Dialog.ArcadeIDContextMenu.onClick = eventData =>
            {
                if (string.IsNullOrEmpty(metadata.arcadeID))
                {
                    EditorManager.inst.DisplayNotification($"No ID assigned. This shouldn't happen. Did something break?", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                LSText.CopyToClipboard(metadata.arcadeID);
                EditorManager.inst.DisplayNotification($"Copied ID: {metadata.arcadeID} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
            };

            Dialog.ServerIDText.text = !string.IsNullOrEmpty(metadata.serverID) ? $"Server ID: {metadata.serverID} (Click to copy)" : "Server ID: No ID";
            Dialog.ServerIDContextMenu.onClick = eventData =>
            {
                if (string.IsNullOrEmpty(metadata.serverID))
                {
                    EditorManager.inst.DisplayNotification($"Upload the level first before trying to copy the server ID.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                LSText.CopyToClipboard(metadata.serverID);
                EditorManager.inst.DisplayNotification($"Copied ID: {metadata.serverID} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
            };

            Dialog.UserIDText.text = !string.IsNullOrEmpty(LegacyPlugin.UserID) ? $"User ID: {LegacyPlugin.UserID} (Click to copy)" : "User ID: No ID";
            Dialog.UserIDContextMenu.onClick = eventData =>
            {
                if (string.IsNullOrEmpty(LegacyPlugin.UserID))
                {
                    EditorManager.inst.DisplayNotification($"Login first before trying to copy the user ID.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                LSText.CopyToClipboard(LegacyPlugin.UserID);
                EditorManager.inst.DisplayNotification($"Copied ID: {LegacyPlugin.UserID} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
            };

            // Changed revisions to modded display.
            Dialog.ModdedDisplayText.text = $"Modded: {(GameData.Current.Modded ? "Yes" : "No")}";

            Dialog.UploadButtonText.text = hasID ? "Update" : "Upload";
            Dialog.UploadContextMenu.onClick = eventData =>
            {
                if (eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Upload / Update", UploadLevel),
                    new ButtonFunction("Verify Level is on Server", () => RTEditor.inst.ShowWarningPopup("Do you want to verify that the level is on the Arcade server?", () =>
                    {
                        RTEditor.inst.HideWarningPopup();
                        EditorManager.inst.DisplayNotification("Verifying...", 1.5f, EditorManager.NotificationType.Info);
                        VerifyLevelIsOnServer();
                    }, RTEditor.inst.HideWarningPopup)),
                    new ButtonFunction("Pull Changes from Server", () => RTEditor.inst.ShowWarningPopup("Do you want to pull the level from the Arcade server?", () =>
                    {
                        RTEditor.inst.HideWarningPopup();
                        EditorManager.inst.DisplayNotification("Pulling level...", 1.5f, EditorManager.NotificationType.Info);
                        PullLevel();
                    }, RTEditor.inst.HideWarningPopup)),
                    new ButtonFunction(true),
                    new ButtonFunction("Guidelines", () => EditorDocumentation.inst.OpenDocument("Uploading a Level"))
                    );
            };

            Dialog.PullButton.gameObject.SetActive(hasID);
            Dialog.DeleteButton.gameObject.SetActive(hasID);

            Dialog.ConvertButton.onClick.NewListener(ConvertLevel);
            Dialog.UploadButton.onClick.NewListener(UploadLevel);

            if (!hasID)
                return;

            Dialog.PullButton.onClick.NewListener(PullLevel);
            Dialog.DeleteButton.onClick.NewListener(DeleteLevel);
        }

        static Vector2 difficultySize = new Vector2(100f, 32f);
        public void RenderDifficulty(MetaData metadata)
        {
            LSHelpers.DeleteChildren(Dialog.DifficultyContent);

            var values = CustomEnumHelper.GetValues<DifficultyType>();
            var count = values.Length - 1;

            foreach (var difficulty in values)
            {
                if (difficulty.Ordinal < 0) // skip unknown difficulty
                    continue;

                var gameObject = difficultyToggle.Duplicate(Dialog.DifficultyContent, difficulty.DisplayName.ToLower(), difficulty == count - 1 ? 0 : difficulty + 1);
                gameObject.transform.localScale = Vector3.one;

                gameObject.transform.AsRT().sizeDelta = difficultySize;

                var text = gameObject.transform.Find("Background/Text").GetComponent<Text>();
                text.color = LSColors.ContrastColor(difficulty.Color);
                text.text = difficulty == count - 1 ? "Anim" : difficulty.DisplayName;
                text.fontSize = 17;
                var toggle = gameObject.GetComponent<Toggle>();
                toggle.image.color = difficulty.Color;
                toggle.group = null;
                toggle.SetIsOnWithoutNotify(metadata.song.DifficultyType == difficulty);
                toggle.onValueChanged.NewListener(_val =>
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

            if (metadata.tags != null)
            {
                for (int i = 0; i < metadata.tags.Count; i++)
                {
                    int index = i;
                    var tag = metadata.tags[i];
                    var gameObject = EditorPrefabHolder.Instance.Tag.Duplicate(Dialog.TagsContent, index.ToString());
                    var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                    input.SetTextWithoutNotify(tag);
                    input.onValueChanged.NewListener(_val =>
                    {
                        _val = RTString.ReplaceSpace(_val);
                        var oldVal = metadata.tags[index];
                        metadata.tags[index] = _val;

                        EditorManager.inst.history.Add(new History.Command("Change MetaData Tag", () =>
                        {
                            metadata.tags[index] = _val;
                            MetadataEditor.inst.OpenDialog();
                        }, () =>
                        {
                            metadata.tags[index] = oldVal;
                            MetadataEditor.inst.OpenDialog();
                        }));
                    });

                    var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                    deleteStorage.button.onClick.NewListener(() =>
                    {
                        var oldTag = metadata.tags[index];
                        metadata.tags.RemoveAt(index);
                        RenderTags(metadata);

                        EditorManager.inst.history.Add(new History.Command("Delete MetaData Tag", () =>
                        {
                            if (metadata.tags == null)
                                return;
                            metadata.tags.RemoveAt(index);
                            MetadataEditor.inst.OpenDialog();
                        }, () =>
                        {
                            if (metadata.tags == null)
                                metadata.tags = new List<string>();
                            metadata.tags.Insert(index, oldTag);
                            MetadataEditor.inst.OpenDialog();
                        }));
                    });

                    EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Input_Field, true);

                    EditorThemeManager.ApplyInputField(input);

                    EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                    EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);
                }
            }

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(Dialog.TagsContent, "Add");
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Tag";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            var contextClickable = add.GetOrAddComponent<ContextClickable>();
            contextClickable.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                {
                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Add a Default Tag", () =>
                        {
                            TagPopup.Open();
                            RenderTagPopup(tag =>
                            {
                                var metadata = MetaData.Current;
                                if (metadata.tags == null)
                                    metadata.tags = new List<string>();
                                metadata.tags.Add(tag);
                                RenderTags(metadata);
                            });
                        }),
                        new ButtonFunction("Clear Tags", () =>
                        {
                            metadata.tags?.Clear();
                            RenderTags(metadata);
                        }));
                    return;
                }

                if (metadata.tags == null)
                    metadata.tags = new List<string>();
                metadata.tags.Add(DEFAULT_NEW_TAG);
                RenderTags(metadata);

                EditorManager.inst.history.Add(new History.Command("Add MetaData Tag",
                    () =>
                    {
                        if (metadata.tags == null)
                            metadata.tags = new List<string>();
                        metadata.tags.Add(DEFAULT_NEW_TAG);
                        MetadataEditor.inst.OpenDialog();
                    },
                    () =>
                    {
                        if (metadata.tags == null)
                            return;
                        metadata.tags.RemoveAt(metadata.tags.Count - 1);
                        MetadataEditor.inst.OpenDialog();
                    }));
            };

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);
        }

        #endregion

        #region Tags

        public const string DEFAULT_NEW_TAG = "new_tag";

        public List<string> defaultTags = new List<string>()
        {
            "boss_level",
            "joke_level",
            "meme",
            "flashing_lights",
            "high_detail",
            "reupload",
            "remake",
            "wip",
            "old",
            "experimental",
            "feature_window",
            "feature_video_bg",
            "story",
            "horror",
        };

        public void RenderTagPopup(Action<string> onTagSelected)
        {
            TagPopup.SearchField.onValueChanged.NewListener(_val => RenderTagPopup(onTagSelected));

            TagPopup.ClearContent();
            foreach (var tag in defaultTags)
            {
                if (!RTString.SearchString(TagPopup.SearchTerm, tag))
                    continue;

                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(TagPopup.Content, "tag");
                var storage = gameObject.GetComponent<FunctionButtonStorage>();

                storage.label.text = tag;
                storage.button.onClick.NewListener(() => onTagSelected?.Invoke(tag));

                EditorThemeManager.ApplyLightText(storage.label);
                EditorThemeManager.ApplySelectable(storage.button, ThemeGroup.List_Button_1);
            }
        }

        #endregion

        #region Functions

        public bool VerifyFile(string file) => !file.Contains("autosave") && !file.Contains("backup") && !file.Contains("level-previous") && file != Level.EDITOR_LSE && !file.Contains("waveform-") &&
            RTFile.FileIsFormat(file, FileFormat.LSB, FileFormat.LSA, FileFormat.JPG, FileFormat.PNG, FileFormat.OGG, FileFormat.WAV, FileFormat.MP3, FileFormat.MP4);

        public void OpenIconSelector()
        {
            string jpgFile = FileBrowser.OpenSingleFile("jpg");
            CoreHelper.Log("Selected file: " + jpgFile);
            if (string.IsNullOrEmpty(jpgFile))
                return;

            string jpgFileLocation = EditorLevelManager.inst.CurrentLevel.GetFile(Level.LEVEL_JPG);
            CoroutineHelper.StartCoroutine(EditorManager.inst.GetSprite(jpgFile, new EditorManager.SpriteLimits(new Vector2(512f, 512f)), cover =>
            {
                RTFile.CopyFile(jpgFile, jpgFileLocation);
                RTMetaDataEditor.inst.SetLevelCover(cover);
            }, errorFile => EditorManager.inst.DisplayNotification("Please resize your image to be less than or equal to 512 x 512 pixels. It must also be a jpg.", 2f, EditorManager.NotificationType.Error)));
        }

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
            if (!EditorManager.inst.hasLoadedLevel || !MetaData.Current)
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

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile($"{AlephNetwork.ArcadeServerURL}api/level/{serverID}", json =>
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
                                MetaData.Current.beatmap.datePublished = string.Empty;
                                var jn = MetaData.Current.ToJSON();
                                RTFile.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB), jn.ToString());

                                RTEditor.inst.HideWarningPopup();
                            }, RTEditor.inst.HideWarningPopup);

                            return;
                        }
                    case 401: {
                            if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null && LegacyPlugin.authData["refresh_token"] != null)
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
                                MetaData.Current.beatmap.datePublished = string.Empty;
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
                MetaData.Current.beatmap.datePublished = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
                MetaData.Current.beatmap.versionNumber++;
                MetaData.Current.uploaderID = LegacyPlugin.UserID;

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
                if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                    headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

                CoroutineHelper.StartCoroutine(AlephNetwork.UploadBytes($"{AlephNetwork.ArcadeServerURL}api/level", File.ReadAllBytes(path), id =>
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
                        MetaData.Current.beatmap.datePublished = string.Empty;
                        MetaData.Current.beatmap.versionNumber--;
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
                                if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null && LegacyPlugin.authData["refresh_token"] != null)
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
                    if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                        headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

                    CoroutineHelper.StartCoroutine(AlephNetwork.Delete($"{AlephNetwork.ArcadeServerURL}api/level/{id}", () =>
                    {
                        uploading = false;
                        MetaData.Current.beatmap.datePublished = string.Empty;
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
                                    if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null && LegacyPlugin.authData["refresh_token"] != null)
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

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile($"{AlephNetwork.ArcadeServerURL}api/level/{serverID}", json =>
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
                            if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null && LegacyPlugin.authData["refresh_token"] != null)
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
                Application.OpenURL($"{AlephNetwork.ArcadeServerURL}api/auth/login");
                CreateLoginListener(onLogin);
                RTEditor.inst.HideWarningPopup();
            }, RTEditor.inst.HideWarningPopup, "Login", "Cancel");
        }

        public IEnumerator RefreshTokens(Action onRefreshed)
        {
            EditorManager.inst.DisplayNotification("Access token expired. Refreshing...", 5f, EditorManager.NotificationType.Warning);

            var form = new WWWForm();
            form.AddField("AccessToken", LegacyPlugin.authData["access_token"].Value);
            form.AddField("RefreshToken", LegacyPlugin.authData["refresh_token"].Value);

            using var www = UnityWebRequest.Post($"{AlephNetwork.ArcadeServerURL}api/auth/refresh", form);
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
            LegacyPlugin.authData["access_token"] = jn["accessToken"].Value;
            LegacyPlugin.authData["refresh_token"] = jn["refreshToken"].Value;
            LegacyPlugin.authData["access_token_expiry_time"] = jn["accessTokenExpiryTime"].Value;

            RTFile.WriteToFile(Path.Combine(Application.persistentDataPath, "auth.json"), LegacyPlugin.authData.ToString());
            EditorManager.inst.DisplayNotification("Refreshed tokens!", 5f, EditorManager.NotificationType.Success);
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

            LegacyPlugin.authData = new JSONObject
            {
                ["id"] = id,
                ["username"] = username,
                ["steam_id"] = steamId,
                ["access_token"] = accessToken,
                ["refresh_token"] = refreshToken,
                ["access_token_expiry_time"] = accessTokenExpiryTime
            };

            RTFile.WriteToFile(Path.Combine(Application.persistentDataPath, "auth.json"), LegacyPlugin.authData.ToString());
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
