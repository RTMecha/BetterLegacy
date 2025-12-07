using System;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using Crosstales.FB;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Managers.Settings;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages editing level metadata.
    /// <br></br>Wraps <see cref="MetadataEditor"/>.
    /// </summary>
    public class RTMetaDataEditor : BaseEditor<RTMetaDataEditor, RTMetaDataEditorSettings, MetadataEditor>
    {
        #region Values

        public override MetadataEditor BaseInstance { get => MetadataEditor.inst; set => MetadataEditor.inst = value; }

        /// <summary>
        /// The MetaData editor dialog.
        /// </summary>
        public MetaDataEditorDialog Dialog { get; set; }

        /// <summary>
        /// If the icon should be collapsed.
        /// </summary>
        public bool CollapseIcon { get; set; } = true;

        /// <summary>
        /// Difficulty toggle prefab.
        /// </summary>
        public GameObject difficultyToggle;

        #endregion

        #region Functions

        public override void OnInit()
        {
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
            EditorServerManager.inst.RenderTagDialog(metadata, Dialog, EditorServerManager.DefaultTagRelation.Level);
            RenderSettings(metadata);

            EditorServerManager.inst.RenderServerDialog(
                uploadable: metadata,
                dialog: Dialog, 
                upload: UploadLevel,
                pull: PullLevel,
                delete: DeleteLevel,
                verify: VerifyLevelIsOnServer);

            // Changed revisions to modded display.
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
            Dialog.ModdedDisplayText.text = $"Modded: {(GameData.Current.Modded ? "Yes" : "No")}";
            Dialog.ConvertButton.onClick.NewListener(ConvertLevel);
        }

        /// <summary>
        /// Renders the artist section.
        /// </summary>
        /// <param name="metadata">Metadata to render.</param>
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

        /// <summary>
        /// Renders the creator section.
        /// </summary>
        /// <param name="metadata">Metadata to render.</param>
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

        /// <summary>
        /// Renders the song section.
        /// </summary>
        /// <param name="metadata">Metadata to render.</param>
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

        /// <summary>
        /// Renders the level section.
        /// </summary>
        /// <param name="metadata">Metadata to render.</param>
        public void RenderLevel(MetaData metadata)
        {
            Dialog.LevelNameField.SetTextWithoutNotify(metadata.beatmap.name);
            Dialog.LevelNameField.onValueChanged.NewListener(_val => metadata.beatmap.name = _val);

            Dialog.DescriptionField.SetTextWithoutNotify(metadata.song.description);
            Dialog.DescriptionField.onValueChanged.NewListener(_val => metadata.song.description = _val);

            Dialog.OpenVideoURLButton.onClick.NewListener(() =>
            {
                if (!string.IsNullOrEmpty(metadata.beatmap.VideoURL))
                    Application.OpenURL(metadata.beatmap.VideoURL);
            });

            Dialog.VideoLinkField.SetTextWithoutNotify(metadata.beatmap.videoLink);
            Dialog.VideoLinkField.onEndEdit.NewListener(_val =>
            {
                string oldVal = metadata.beatmap.videoLink;
                metadata.beatmap.videoLink = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", () =>
                {
                    metadata.beatmap.videoLink = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.beatmap.videoLink = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            Dialog.VideoLinkTypeDropdown.options = AlephNetwork.VideoLinks.Select(x => new Dropdown.OptionData(x.name)).ToList();
            Dialog.VideoLinkTypeDropdown.SetValueWithoutNotify(metadata.beatmap.videoLinkType);
            Dialog.VideoLinkTypeDropdown.onValueChanged.NewListener(_val =>
            {
                int oldVal = metadata.beatmap.videoLinkType;
                metadata.beatmap.videoLinkType = _val;
                EditorManager.inst.history.Add(new History.Command("Change Artist Link", () =>
                {
                    metadata.beatmap.videoLinkType = _val;
                    MetadataEditor.inst.OpenDialog();
                }, () =>
                {
                    metadata.beatmap.videoLinkType = oldVal;
                    MetadataEditor.inst.OpenDialog();
                }));
            });

            Dialog.VersionField.SetTextWithoutNotify(metadata.ObjectVersion);
            Dialog.VersionField.onValueChanged.NewListener(_val => metadata.ObjectVersion = _val);
            Dialog.VersionField.onEndEdit.NewListener(_val => RenderLevel(metadata));
            EditorContextMenu.AddContextMenu(Dialog.VersionField.gameObject, EditorContextMenu.GetObjectVersionFunctions(metadata, () => RenderLevel(metadata)));
        }

        /// <summary>
        /// Renders the settings section.
        /// </summary>
        /// <param name="metadata">Metadata to render.</param>
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

        /// <summary>
        /// Renders the difficulty section.
        /// </summary>
        /// <param name="metadata">Metadata to render.</param>
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

                gameObject.transform.AsRT().sizeDelta = new Vector2(100f, 32f);

                var text = gameObject.transform.Find("Background/Text").GetComponent<Text>();
                text.color = LSColors.ContrastColor(difficulty.Color);
                text.text = difficulty == count - 1 ? "Anim" : difficulty.DisplayName;
                text.fontSize = 17;
                var toggle = gameObject.GetComponent<Toggle>();
                toggle.image.color = difficulty.Color;
                toggle.group = null;
                toggle.SetIsOnWithoutNotify(metadata.song.Difficulty == difficulty);
                toggle.onValueChanged.NewListener(_val =>
                {
                    metadata.song.Difficulty = difficulty;
                    RenderDifficulty(metadata);
                });

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_1);
            }
        }

        #endregion

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

        /// <summary>
        /// Sets the current levels' icon.
        /// </summary>
        /// <param name="sprite">Icon to set.</param>
        public void SetLevelCover(Sprite sprite)
        {
            if (EditorLevelManager.inst.CurrentLevel)
            {
                EditorLevelManager.inst.CurrentLevel.icon = sprite;
                Dialog.IconImage.sprite = EditorLevelManager.inst.CurrentLevel.icon;
            }
        }

        /// <summary>
        /// Verifies the current level is on the Arcade server.
        /// </summary>
        public void VerifyLevelIsOnServer() => EditorServerManager.inst.Verify(
                url: AlephNetwork.LevelURL,
                uploadable: MetaData.Current,
                saveFile: () =>
                {
                    var jn = MetaData.Current.ToJSON();
                    RTFile.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB), jn.ToString());
                });

        /// <summary>
        /// Converts the current level to the VG format.
        /// </summary>
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

        /// <summary>
        /// Uploads the current level to the Arcade server.
        /// </summary>
        public void UploadLevel() => EditorServerManager.inst.Upload(
            url: $"{AlephNetwork.ArcadeServerURL}api/level",
            fileName: EditorManager.inst.currentLoadedLevel,
            uploadable: MetaData.Current,
            transfer: tempDirectory =>
            {
                var directory = RTFile.BasePath;
                var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    if (!EditorServerManager.inst.VerifyFile(Path.GetFileName(file)))
                        continue;

                    var copyTo = file.Replace(directory, tempDirectory);

                    var dir = RTFile.GetDirectory(copyTo);

                    RTFile.CreateDirectory(dir);
                    RTFile.CopyFile(file, copyTo);
                }
            },
            saveFile: () =>
            {
                var jn = MetaData.Current.ToJSON();
                RTFile.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB), jn.ToString());
            },
            onUpload: RenderDialog);

        /// <summary>
        /// Removes the current level from the Arcade server.
        /// </summary>
        public void DeleteLevel() => EditorServerManager.inst.Delete(
                url: AlephNetwork.LevelURL,
                uploadable: MetaData.Current,
                saveFile: () =>
                {
                    var jn = MetaData.Current.ToJSON();
                    RTFile.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB), jn.ToString());
                },
                onDelete: RenderDialog);

        /// <summary>
        /// Pulls the current level's file from the Arcade server.
        /// </summary>
        public void PullLevel()
        {
            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Cannot pull level from the Arcade server because the level is saving!", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            EditorServerManager.inst.Pull(
                url: AlephNetwork.LevelURL,
                uploadable: MetaData.Current,
                pull: jn =>
                {
                    GameData.Current?.SaveData(RTFile.CombinePaths(EditorLevelManager.inst.CurrentLevel.path, "reload-level-backup.lsb"));
                    EditorServerManager.inst.DownloadLevel(jn["id"], RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path), jn["name"], EditorLevelManager.inst.ILoadLevel(EditorLevelManager.inst.CurrentLevel).Start);
                });
        }

        #endregion
    }
}
