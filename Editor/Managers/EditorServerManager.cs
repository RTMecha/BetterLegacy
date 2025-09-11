using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    public class EditorServerManager : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="EditorServerManager"/> global instance reference.
        /// </summary>
        public static EditorServerManager inst;

        /// <summary>
        /// Initializes <see cref="EditorServerManager"/>.
        /// </summary>
        public static void Init() => EditorManager.inst.gameObject.AddComponent<EditorServerManager>();

        void Awake()
        {
            inst = this;
            CoroutineHelper.StartCoroutine(Setup());
        }

        IEnumerator Setup()
        {
            while (!RTEditor.inst)
                yield return null;

            try
            {
                Dialog = new UploadedLevelsDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            try
            {
                LoadDefaultTags();
                TagPopup = RTEditor.inst.GeneratePopup(EditorPopup.DEFAULT_TAGS_POPUP, "Add a default tag",
                    refreshSearch: _val => { });
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init tags
        }

        #endregion

        #region Values

        public UploadedLevelsDialog Dialog { get; set; }

        public ContentPopup TagPopup { get; set; }

        public bool uploading;

        #region Tags

        /// <summary>
        /// Default tag to add.
        /// </summary>
        public const string DEFAULT_NEW_TAG = "new_tag";

        /// <summary>
        /// Level default tags.
        /// </summary>
        public List<string> defaultTags = new List<string>();

        /// <summary>
        /// Prefab default tags.
        /// </summary>
        public List<string> defaultPrefabTags = new List<string>();

        /// <summary>
        /// Level custom default tags.
        /// </summary>
        public List<string> customDefaultTags = new List<string>();

        /// <summary>
        /// Prefab custom default tags.
        /// </summary>
        public List<string> customDefaultPrefabTags = new List<string>();

        /// <summary>
        /// Indicates a default tag's relation to an object.
        /// </summary>
        public enum DefaultTagRelation
        {
            /// <summary>
            /// Default tag is for anything to do with Levels.
            /// </summary>
            Level,
            /// <summary>
            /// Default tag is for anything to do with Prefabs.
            /// </summary>
            Prefab,
        }

        #endregion

        #region Search

        public int itemCount;

        bool loadingOnlineLevels;

        public Tab tab;
        public enum Tab
        {
            Levels,
            LevelCollections,
            Prefabs,
        }

        public Dictionary<Tab, TabSettings> tabSettings = new Dictionary<Tab, TabSettings>
        {
            { Tab.Levels, new TabSettings() { sort = (int)OnlineLevelSort.DatePublished, ascend = true, } },
            { Tab.LevelCollections, new TabSettings() { sort = (int)OnlineLevelCollectionSort.DatePublished, ascend = true, } },
            { Tab.Prefabs, new TabSettings() { sort = (int)OnlinePrefabSort.DatePublished, ascend = true, } },
        };

        public TabSettings CurrentTabSettings
        {
            get
            {
                if (!this.tabSettings.TryGetValue(tab, out TabSettings tabSettings))
                {
                    tabSettings = new TabSettings();
                    this.tabSettings[tab] = tabSettings;
                }

                return tabSettings;
            }
        }

        public class TabSettings
        {
            public int page;
            public int sort;
            public bool ascend;
            public bool uploaded = true;
        }

        public string SearchURL => RTFile.CombinePaths(tab switch
        {
            Tab.Levels => AlephNetwork.LevelURL,
            Tab.LevelCollections => AlephNetwork.LevelCollectionURL,
            Tab.Prefabs => AlephNetwork.PrefabURL,
            _ => null,
        }, tab != Tab.Prefabs || CurrentTabSettings.uploaded ? "uploaded" : "search");

        public static Dictionary<string, Sprite> OnlineLevelIcons { get; set; } = new Dictionary<string, Sprite>();

        #endregion

        HttpListener _listener;

        #endregion

        #region Methods

        /// <summary>
        /// Uploads an object to the server.
        /// </summary>
        /// <param name="url">URL to post to.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="uploadable">Uploadable object.</param>
        /// <param name="transfer">Function to run when preparing the files for uploading. Transfer the files that should be uploaded to the passed "tempDirectory parameter".</param>
        /// <param name="saveFile">Function to run when changes to the file are made, prompting it to be saved.</param>
        /// <param name="onUpload">Function to run when upload is successful.</param>
        public void Upload(string url, string fileName, IUploadable uploadable, Action<string> transfer, Action saveFile, Action onUpload)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (transfer == null)
                throw new ArgumentNullException(nameof(transfer));

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

            var path = RTFile.CombinePaths(exportPath, $"{fileName}-server-upload{FileFormat.ZIP.Dot()}");

            try
            {
                uploadable.DatePublished = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
                uploadable.VersionNumber++;
                uploadable.UploaderID = LegacyPlugin.UserID;
                saveFile?.Invoke();

                RTFile.DeleteFile(path);

                // here we setup a temporary upload folder that has no editor files, which we then zip and delete the directory.
                var tempDirectory = RTFile.CombinePaths(exportPath, fileName + "-temp/");
                RTFile.CreateDirectory(tempDirectory);
                transfer.Invoke(tempDirectory);

                ZipFile.CreateFromDirectory(tempDirectory, path);
                RTFile.DeleteDirectory(tempDirectory);

                var headers = new Dictionary<string, string>();
                if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                    headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

                RTEditor.inst.ProgressPopup.Text = "Uploading item to the server, please wait...";
                RTEditor.inst.ProgressPopup.Open();

                CoroutineHelper.StartCoroutine(AlephNetwork.UploadBytes(url, File.ReadAllBytes(path), RTEditor.inst.ProgressPopup.UpdateProgress,
                    id =>
                    {
                        RTEditor.inst.ProgressPopup.Close();
                        uploading = false;
                        uploadable.ServerID = id;
                        saveFile?.Invoke();

                        RTFile.DeleteFile(path);

                        EditorManager.inst.DisplayNotification($"Item uploaded! ID: {id}", 3f, EditorManager.NotificationType.Success);
                        onUpload?.Invoke();

                        AchievementManager.inst.UnlockAchievement("upload_level");
                    },
                    (string onError, long responseCode, string errorMsg) =>
                    {
                        RTEditor.inst.ProgressPopup.Close();
                        uploading = false;
                        // Only downgrade if server ID wasn't already assigned.
                        if (string.IsNullOrEmpty(uploadable.ServerID))
                        {
                            uploadable.UploaderID = null;
                            uploadable.DatePublished = string.Empty;
                            uploadable.VersionNumber--;
                            saveFile?.Invoke();
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
                                        CoroutineHelper.StartCoroutine(RefreshTokens(() => Upload(url, fileName, uploadable, transfer, saveFile, onUpload)));
                                        return;
                                    }
                                    ShowLoginPopup(() => Upload(url, fileName, uploadable, transfer, saveFile, onUpload));
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

        /// <summary>
        /// Removes an object from the server.
        /// </summary>
        /// <param name="url">URL to delete from.</param>
        /// <param name="uploadable">Uploadable object.</param>
        /// <param name="saveFile">Function to run when changes to the file are made, prompting it to be saved.</param>
        /// <param name="onUpload">Function to run when delete is successful.</param>
        public void Delete(string url, IUploadable uploadable, Action saveFile, Action onDelete)
        {
            if (uploading)
            {
                EditorManager.inst.DisplayNotification("Please wait until upload / delete process is finished!", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            uploading = true;

            RTEditor.inst.ShowWarningPopup("Are you sure you want to remove this item from the Arcade server? This cannot be undone!", () =>
            {
                try
                {
                    EditorManager.inst.DisplayNotification("Attempting to delete item from the server... please wait.", 3f, EditorManager.NotificationType.Warning);

                    var id = uploadable.ServerID;

                    var headers = new Dictionary<string, string>();
                    if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                        headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

                    RTEditor.inst.ProgressPopup.Text = $"Removing {id} from the server, please wait...";
                    RTEditor.inst.ProgressPopup.Open();

                    CoroutineHelper.StartCoroutine(AlephNetwork.Delete(RTFile.CombinePaths(url, id), RTEditor.inst.ProgressPopup.UpdateProgress,
                        () =>
                        {
                            RTEditor.inst.ProgressPopup.Close();
                            uploading = false;
                            uploadable.DatePublished = string.Empty;
                            uploadable.ServerID = null;
                            saveFile?.Invoke();

                            EditorManager.inst.DisplayNotification($"Successfully deleted item off the Arcade server.", 2.5f, EditorManager.NotificationType.Success);
                            onDelete?.Invoke();
                            RTEditor.inst.HideWarningPopup();
                        },
                        (string onError, long responseCode, string errorMsg) =>
                        {
                            RTEditor.inst.ProgressPopup.Close();
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
                                            CoroutineHelper.StartCoroutine(RefreshTokens(() => Delete(url, uploadable, saveFile, onDelete)));
                                            return;
                                        }
                                        ShowLoginPopup(() => Delete(url, uploadable, saveFile, onDelete));
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
                    CoreHelper.LogError($"Had an exception in deleting the item.\nException: {ex}");
                }
            }, RTEditor.inst.HideWarningPopup);
        }

        /// <summary>
        /// Verifies an item is on the server.
        /// </summary>
        /// <param name="url">URL to verify from.</param>
        /// <param name="uploadable">Uploadable object.</param>
        /// <param name="saveFile">Function to run when changes to the file are made, prompting it to be saved.</param>
        /// <param name="onVerify">Function to run when the function is finished. The passed parameter is true if the item is on the server, otherwise it is false..</param>
        public void Verify(string url, IUploadable uploadable, Action saveFile, Action<bool> onVerify = null)
        {
            if (!EditorManager.inst.hasLoadedLevel || uploadable == null)
                return;

            if (uploading)
            {
                EditorManager.inst.DisplayNotification("Please wait until upload / delete process is finished!", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            var serverID = uploadable.ServerID;

            if (string.IsNullOrEmpty(serverID))
            {
                EditorManager.inst.DisplayNotification("Server ID was not assigned, so the item probably wasn't on the server.", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            var headers = new Dictionary<string, string>();
            if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

            RTEditor.inst.ProgressPopup.Text = $"Verifying {serverID} is on the server, please wait...";
            RTEditor.inst.ProgressPopup.Open();

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile(RTFile.CombinePaths(url, serverID), RTEditor.inst.ProgressPopup.UpdateProgress,
                json =>
                {
                    RTEditor.inst.ProgressPopup.Close();
                    EditorManager.inst.DisplayNotification($"Item is on server! {serverID}", 3f, EditorManager.NotificationType.Success);
                    onVerify?.Invoke(true);
                },
                (string onError, long responseCode, string errorMsg) =>
                {
                    RTEditor.inst.ProgressPopup.Close();
                    switch (responseCode)
                    {
                        case 404: {
                                EditorManager.inst.DisplayNotification("404 not found.", 2f, EditorManager.NotificationType.Error);
                                RTEditor.inst.ShowWarningPopup("Item was not found on the server. Do you want to remove the server ID?", () =>
                                {
                                    uploadable.ServerID = null;
                                    uploadable.DatePublished = string.Empty;
                                    saveFile?.Invoke();
                                    onVerify?.Invoke(false);

                                    RTEditor.inst.HideWarningPopup();
                                }, RTEditor.inst.HideWarningPopup);

                                return;
                            }
                        case 401: {
                                if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null && LegacyPlugin.authData["refresh_token"] != null)
                                {
                                    CoroutineHelper.StartCoroutine(RefreshTokens(() => Verify(url, uploadable, saveFile, onVerify)));
                                    return;
                                }
                                ShowLoginPopup(() => Verify(url, uploadable, saveFile, onVerify));
                                break;
                            }
                        default: {
                                EditorManager.inst.DisplayNotification($"Verify failed. Error code: {onError}", 2f, EditorManager.NotificationType.Error);
                                RTEditor.inst.ShowWarningPopup("Verification failed. In case the item is not on the server, do you want to remove the server ID?", () =>
                                {
                                    uploadable.ServerID = null;
                                    uploadable.DatePublished = string.Empty;
                                    saveFile?.Invoke();
                                    onVerify?.Invoke(false);

                                    RTEditor.inst.HideWarningPopup();
                                }, RTEditor.inst.HideWarningPopup);

                                break;
                            }
                    }

                    if (errorMsg != null)
                        CoreHelper.LogError($"Error Message: {errorMsg}");
                }, headers));
        }

        /// <summary>
        /// Pulls an object from the server.
        /// </summary>
        /// <param name="url">URL to pull from.</param>
        /// <param name="uploadable">Uploadable object.</param>
        /// <param name="pull">Function to run when the item is ready to be pulled.</param>
        public void Pull(string url, IUploadable uploadable, Action<JSONNode> pull)
        {
            var serverID = uploadable.ServerID;

            if (string.IsNullOrEmpty(serverID))
            {
                EditorManager.inst.DisplayNotification("Server ID was not assigned, so the item probably wasn't on the server.", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            var headers = new Dictionary<string, string>();
            if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

            RTEditor.inst.ProgressPopup.Text = $"Pulling {serverID} from the server, please wait...";
            RTEditor.inst.ProgressPopup.Open();

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile(RTFile.CombinePaths(url, serverID), RTEditor.inst.ProgressPopup.UpdateProgress,
                json =>
                {
                    pull.Invoke(JSON.Parse(json));
                },
                (string onError, long responseCode, string errorMsg) =>
                {
                    RTEditor.inst.ProgressPopup.Close();
                    switch (responseCode)
                    {
                        case 404: {
                                EditorManager.inst.DisplayNotification("404 not found.", 2f, EditorManager.NotificationType.Error);
                                return;
                            }
                        case 401: {
                                if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null && LegacyPlugin.authData["refresh_token"] != null)
                                {
                                    CoroutineHelper.StartCoroutine(RefreshTokens(() => Pull(url, uploadable, pull)));
                                    return;
                                }
                                ShowLoginPopup(() => Pull(url, uploadable, pull));
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

        /// <summary>
        /// Renders a server section of an Editor Dialog.
        /// </summary>
        /// <param name="uploadable">Uploadable object.</param>
        /// <param name="dialog">Dialog to render.</param>
        /// <param name="upload">Upload function.</param>
        /// <param name="pull">Pull function.</param>
        /// <param name="delete">Delete function.</param>
        /// <param name="verify">Verify function.</param>
        public void RenderServerDialog(IUploadable uploadable, IServerDialog dialog, Action upload, Action pull, Action delete, Action verify)
        {
            dialog.ServerVisibilityDropdown.options = CoreHelper.ToOptionData<ServerVisibility>();
            dialog.ServerVisibilityDropdown.SetValueWithoutNotify((int)uploadable.Visibility);
            dialog.ServerVisibilityDropdown.onValueChanged.NewListener(_val => uploadable.Visibility = (ServerVisibility)_val);

            CoreHelper.DestroyChildren(dialog.CollaboratorsContent);
            for (int i = 0; i < uploadable.Uploaders.Count; i++)
            {
                int index = i;
                var tag = uploadable.Uploaders[i];
                var gameObject = EditorPrefabHolder.Instance.Tag.Duplicate(dialog.CollaboratorsContent, index.ToString());
                gameObject.transform.AsRT().sizeDelta = new Vector2(717f, 32f);
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.transform.AsRT().sizeDelta = new Vector2(682, 32f);
                input.SetTextWithoutNotify(tag);
                input.onValueChanged.NewListener(_val =>
                {
                    _val = RTString.ReplaceSpace(_val);
                    var oldVal = uploadable.Uploaders[index];
                    uploadable.Uploaders[index] = _val;

                    EditorManager.inst.history.Add(new History.Command("Change Uploader", () =>
                    {
                        uploadable.Uploaders[index] = _val;
                        dialog.Open();
                        RenderServerDialog(uploadable, dialog, upload, pull, delete, verify);
                    }, () =>
                    {
                        uploadable.Uploaders[index] = oldVal;
                        dialog.Open();
                        RenderServerDialog(uploadable, dialog, upload, pull, delete, verify);
                    }));
                });

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.NewListener(() =>
                {
                    var oldUploader = uploadable.Uploaders[index];
                    uploadable.Uploaders.RemoveAt(index);
                    RenderServerDialog(uploadable, dialog, upload, pull, delete, verify);

                    EditorManager.inst.history.Add(new History.Command("Delete Uploader", () =>
                    {
                        if (uploadable.Uploaders == null)
                            return;
                        uploadable.Uploaders.RemoveAt(index);
                        dialog.Open();
                        RenderServerDialog(uploadable, dialog, upload, pull, delete, verify);
                    }, () =>
                    {
                        if (uploadable.Uploaders == null)
                            uploadable.Uploaders = new List<string>();
                        uploadable.Uploaders.Insert(index, oldUploader);
                        dialog.Open();
                        RenderServerDialog(uploadable, dialog, upload, pull, delete, verify);
                    }));
                });

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Input_Field, true);

                EditorThemeManager.ApplyInputField(input);

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);
            }

            var add = EditorPrefabHolder.Instance.CreateAddButton(dialog.CollaboratorsContent);
            add.Text = "Add Collaborator";
            add.OnClick.ClearAll();

            var contextClickable = add.gameObject.GetOrAddComponent<ContextClickable>();
            contextClickable.onClick = pointerEventData =>
            {
                if (uploadable.Uploaders == null)
                    uploadable.Uploaders = new List<string>();
                uploadable.Uploaders.Add(string.Empty);
                RenderServerDialog(uploadable, dialog, upload, pull, delete, verify);

                EditorManager.inst.history.Add(new History.Command("Add Collaborator",
                    () =>
                    {
                        if (uploadable.Uploaders == null)
                            uploadable.Uploaders = new List<string>();
                        uploadable.Uploaders.Add(string.Empty);
                        RenderServerDialog(uploadable, dialog, upload, pull, delete, verify);
                    },
                    () =>
                    {
                        if (uploadable.Uploaders == null)
                            return;
                        uploadable.Uploaders.RemoveAt(uploadable.Uploaders.Count - 1);
                        RenderServerDialog(uploadable, dialog, upload, pull, delete, verify);
                    }));
            };

            bool hasID = !string.IsNullOrEmpty(uploadable.ServerID); // Only check for server id.

            dialog.ShowChangelog(hasID);
            if (hasID)
            {
                dialog.ChangelogField.SetTextWithoutNotify(uploadable.Changelog);
                dialog.ChangelogField.onValueChanged.NewListener(_val => uploadable.Changelog = _val);
            }

            dialog.ServerIDText.text = !string.IsNullOrEmpty(uploadable.ServerID) ? $"Server ID: {uploadable.ServerID} (Click to copy)" : "Server ID: No ID";
            dialog.ServerIDContextMenu.onClick = eventData =>
            {
                if (string.IsNullOrEmpty(uploadable.ServerID))
                {
                    EditorManager.inst.DisplayNotification($"Upload the item first before trying to copy the server ID.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                LSText.CopyToClipboard(uploadable.ServerID);
                EditorManager.inst.DisplayNotification($"Copied ID: {uploadable.ServerID} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
            };

            dialog.UserIDText.text = !string.IsNullOrEmpty(LegacyPlugin.UserID) ? $"User ID: {LegacyPlugin.UserID} (Click to copy)" : "User ID: No ID";
            dialog.UserIDContextMenu.onClick = eventData =>
            {
                if (string.IsNullOrEmpty(LegacyPlugin.UserID))
                {
                    EditorManager.inst.DisplayNotification($"Login first before trying to copy the user ID.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                LSText.CopyToClipboard(LegacyPlugin.UserID);
                EditorManager.inst.DisplayNotification($"Copied ID: {LegacyPlugin.UserID} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
            };

            dialog.UploadButtonText.text = hasID ? "Update" : "Upload";
            dialog.UploadContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                var buttonFunctions = new List<ButtonFunction>
                {
                    new ButtonFunction(hasID ? "Update" : "Upload", () => upload?.Invoke()),
                };

                if (pull != null)
                    buttonFunctions.Add(new ButtonFunction("Pull Changes from Server", () => RTEditor.inst.ShowWarningPopup("Do you want to pull the level from the Arcade server?", () =>
                    {
                        RTEditor.inst.HideWarningPopup();
                        EditorManager.inst.DisplayNotification("Pulling level...", 1.5f, EditorManager.NotificationType.Info);
                        pull.Invoke();
                    }, RTEditor.inst.HideWarningPopup)));
                
                if (verify != null)
                    buttonFunctions.Add(new ButtonFunction("Verify item is on Server", () => RTEditor.inst.ShowWarningPopup("Do you want to verify that the item is on the Arcade server?", () =>
                    {
                        RTEditor.inst.HideWarningPopup();
                        EditorManager.inst.DisplayNotification("Verifying...", 1.5f, EditorManager.NotificationType.Info);
                        verify.Invoke();
                    }, RTEditor.inst.HideWarningPopup)));

                buttonFunctions.AddRange(new List<ButtonFunction>
                {
                    new ButtonFunction(true),
                    new ButtonFunction("Guidelines", () => EditorDocumentation.inst.OpenDocument("Uploading a Level"))
                });

                EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
            };

            if (dialog.PullButton)
                dialog.PullButton.gameObject.SetActive(hasID && pull != null);
            dialog.DeleteButton.gameObject.SetActive(hasID);

            dialog.UploadButton.onClick.NewListener(() => upload?.Invoke());

            if (!hasID)
                return;

            if (dialog.PullButton)
                dialog.PullButton.onClick.NewListener(() => pull?.Invoke());
            dialog.DeleteButton.onClick.NewListener(() => delete?.Invoke());
        }

        /// <summary>
        /// Verifies a file can be included in a server item.
        /// </summary>
        /// <param name="file">File to verify.</param>
        /// <returns>Returns true if the file can be included, otherwise returns false.</returns>
        public bool VerifyFile(string file) => !file.Contains("autosave") && !file.Contains("backup") && !file.Contains("level-previous") && !file.Contains("waveform-") &&
            RTFile.FileIsFormat(file,
                FileFormat.LSB,
                FileFormat.LSA,
                FileFormat.LSE,
                FileFormat.LSCO,
                FileFormat.LSPO,
                FileFormat.LSP,
                FileFormat.LST,
                FileFormat.LSPL,
                FileFormat.JPG,
                FileFormat.PNG,
                FileFormat.OGG,
                FileFormat.WAV,
                FileFormat.MP3,
                FileFormat.MP4);
        
        #region Tags

        /// <summary>
        /// Gets a default tag list.
        /// </summary>
        /// <param name="relation">The related default tag list.</param>
        /// <returns>Returns the related default tag list.</returns>
        public List<string> GetDefaultTags(DefaultTagRelation relation) => relation switch
        {
            DefaultTagRelation.Level => defaultTags,
            DefaultTagRelation.Prefab => defaultPrefabTags,
            _ => null,
        };

        /// <summary>
        /// Gets a custom default tag list.
        /// </summary>
        /// <param name="relation">The related default tag list.</param>
        /// <returns>Returns the related default tag list.</returns>
        public List<string> GetCustomDefaultTags(DefaultTagRelation relation) => relation switch
        {
            DefaultTagRelation.Level => customDefaultTags,
            DefaultTagRelation.Prefab => customDefaultPrefabTags,
            _ => null,
        };

        /// <summary>
        /// Loads the default tag lists.
        /// </summary>
        public void LoadDefaultTags()
        {
            defaultTags.Clear();
            defaultPrefabTags.Clear();
            customDefaultTags.Clear();
            customDefaultPrefabTags.Clear();

            var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.GetAsset($"default_tags{FileFormat.JSON.Dot()}")));
            if (jn["level"] != null)
                for (int i = 0; i < jn["level"].Count; i++)
                    defaultTags.Add(jn["level"][i]);
            if (jn["prefab"] != null)
                for (int i = 0; i < jn["prefab"].Count; i++)
                    defaultPrefabTags.Add(jn["prefab"][i]);

            if (!RTFile.FileExists(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, $"default_tags{FileFormat.JSON.Dot()}")))
                return;

            jn = JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, $"default_tags{FileFormat.JSON.Dot()}")));
            if (jn["level"] != null)
                for (int i = 0; i < jn["level"].Count; i++)
                {
                    defaultTags.Add(jn["level"][i]);
                    customDefaultTags.Add(jn["level"][i]);
                }
            if (jn["prefab"] != null)
                for (int i = 0; i < jn["prefab"].Count; i++)
                {
                    defaultPrefabTags.Add(jn["prefab"][i]);
                    customDefaultPrefabTags.Add(jn["prefab"][i]);
                }
        }

        /// <summary>
        /// Saves the custom default tags to the default_tags.json file.
        /// </summary>
        public void SaveCustomDefaultTags()
        {
            var jn = Parser.NewJSONObject();
            for (int i = 0; i < customDefaultTags.Count; i++)
                jn["level"][i] = customDefaultTags[i] ?? string.Empty;
            for (int i = 0; i < customDefaultPrefabTags.Count; i++)
                jn["prefab"][i] = customDefaultPrefabTags[i] ?? string.Empty;
            RTFile.WriteToFile(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, $"default_tags{FileFormat.JSON.Dot()}"), jn.ToString(3));
        }

        /// <summary>
        /// Adds a tag to the custom default tag list.
        /// </summary>
        /// <param name="tag">Tag to add.</param>
        /// <param name="relation">The related default tag list.</param>
        public void AddCustomDefaultTag(string tag, DefaultTagRelation relation)
        {
            switch (relation)
            {
                case DefaultTagRelation.Level: {
                        defaultTags.Add(tag);
                        customDefaultTags.Add(tag);
                        break;
                    }
                case DefaultTagRelation.Prefab: {
                        defaultPrefabTags.Add(tag);
                        customDefaultPrefabTags.Add(tag);
                        break;
                    }
            }

            SaveCustomDefaultTags();
        }

        /// <summary>
        /// Opens the default tag popup.
        /// </summary>
        /// <param name="onTagSelected">Function to run when a default tag is selected.</param>
        /// <param name="relation">The related default tag list.</param>
        public void OpenTagPopup(Action<string> onTagSelected, DefaultTagRelation relation)
        {
            TagPopup.Open();
            RenderTagPopup(onTagSelected, relation);
        }

        /// <summary>
        /// Renders the default tag popup.
        /// </summary>
        /// <param name="onTagSelected">Function to run when a default tag is selected.</param>
        /// <param name="relation">Related default tag list.</param>
        public void RenderTagPopup(Action<string> onTagSelected, DefaultTagRelation relation)
        {
            TagPopup.SearchField.onValueChanged.NewListener(_val => RenderTagPopup(onTagSelected, relation));

            var defaultTags = GetDefaultTags(relation);
            if (defaultTags == null)
                return;

            TagPopup.ClearContent();
            foreach (var tag in defaultTags)
            {
                if (!RTString.SearchString(TagPopup.SearchTerm, tag))
                    continue;

                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(TagPopup.Content, "tag");
                var storage = gameObject.GetComponent<FunctionButtonStorage>();

                storage.Text = tag;
                storage.OnClick.NewListener(() => onTagSelected?.Invoke(tag));

                EditorContextMenu.inst.AddContextMenu(gameObject, () => { },
                    new ButtonFunction("Select Tag", () => onTagSelected?.Invoke(tag)),
                    new ButtonFunction("Remove Tag", () =>
                    {
                        var customDefaultTags = GetCustomDefaultTags(relation);
                        if (customDefaultTags == null)
                            return;

                        if (!customDefaultTags.Contains(tag))
                        {
                            EditorManager.inst.DisplayNotification("Tag is not a custom tag!", 2f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        defaultTags.Remove(tag);
                        customDefaultTags.Remove(tag);
                        SaveCustomDefaultTags();
                        RenderTagPopup(onTagSelected, relation);
                    }));

                EditorThemeManager.ApplyLightText(storage.label);
                EditorThemeManager.ApplySelectable(storage.button, ThemeGroup.List_Button_1);
            }
        }

        /// <summary>
        /// Opens a dialog with tag editing.
        /// </summary>
        /// <param name="uploadable">Uploadable object that contains tag information.</param>
        /// <param name="dialog">Editor Dialog with tag UI.</param>
        /// <param name="relation">Related tag type.</param>
        /// <param name="onUpdateTags">Function to run when updating the uploadable object.</param>
        public void OpenTagDialog(IUploadable uploadable, ITagDialog dialog, DefaultTagRelation relation, Action onUpdateTags = null)
        {
            dialog.Open();
            RenderTagDialog(uploadable, dialog, relation, onUpdateTags);
        }

        /// <summary>
        /// Renders the tag section of a dialog.
        /// </summary>
        /// <param name="uploadable">Uploadable object that contains tag information.</param>
        /// <param name="dialog">Editor Dialog with tag UI.</param>
        /// <param name="relation">Related tag type.</param>
        /// <param name="onUpdateTags">Function to run when updating the uploadable object.</param>
        public void RenderTagDialog(IUploadable uploadable, ITagDialog dialog, DefaultTagRelation relation, Action onUpdateTags = null)
        {
            LSHelpers.DeleteChildren(dialog.TagsContent);
            if (uploadable == null)
                return;

            for (int i = 0; i < uploadable.ArcadeTags.Count; i++)
            {
                int index = i;
                var tag = uploadable.ArcadeTags[i];
                var gameObject = EditorPrefabHolder.Instance.Tag.Duplicate(dialog.TagsContent, index.ToString());
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.SetTextWithoutNotify(tag);
                input.onValueChanged.NewListener(_val =>
                {
                    _val = RTString.ReplaceSpace(_val);
                    var oldTag = uploadable.ArcadeTags[index];
                    uploadable.ArcadeTags[index] = _val;

                    EditorManager.inst.history.Add(new History.Command("Set tag",
                        () =>
                        {
                            uploadable.ArcadeTags[index] = _val;
                            OpenTagDialog(uploadable, dialog, relation, onUpdateTags);
                        },
                        () =>
                        {
                            uploadable.ArcadeTags[index] = oldTag;
                            OpenTagDialog(uploadable, dialog, relation, onUpdateTags);
                        }));
                });
                input.onEndEdit.NewListener(_val => onUpdateTags?.Invoke());
                var inputContextClickable = input.gameObject.GetOrAddComponent<ContextClickable>();
                inputContextClickable.onClick = pointerEventData =>
                {
                    if (pointerEventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Add to Default Tags", () => AddCustomDefaultTag(uploadable.ArcadeTags[index], relation)),
                        new ButtonFunction("Copy to Clipboard", () => LSText.CopyToClipboard(uploadable.ArcadeTags[index])));
                };

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.NewListener(() =>
                {
                    var oldTag = uploadable.ArcadeTags[index];
                    uploadable.ArcadeTags.RemoveAt(index);
                    onUpdateTags?.Invoke();
                    RenderTagDialog(uploadable, dialog, relation, onUpdateTags);

                    EditorManager.inst.history.Add(new History.Command("Delete tag",
                        () =>
                        {
                            uploadable.ArcadeTags.RemoveAt(index);
                            OpenTagDialog(uploadable, dialog, relation, onUpdateTags);
                        },
                        () =>
                        {
                            uploadable.ArcadeTags.Insert(index, oldTag);
                            OpenTagDialog(uploadable, dialog, relation, onUpdateTags);
                        }));
                });

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Input_Field, true);
                EditorThemeManager.ApplyInputField(input);
                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);
            }

            var add = EditorPrefabHolder.Instance.CreateAddButton(dialog.TagsContent);
            add.Text = "Add Tag";
            add.OnClick.ClearAll();
            var contextClickable = add.gameObject.GetOrAddComponent<ContextClickable>();
            contextClickable.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                {
                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Add a Default Tag", () => OpenTagPopup(tag =>
                        {
                            var index = uploadable.ArcadeTags.Count;
                            uploadable.ArcadeTags.Add(tag);
                            onUpdateTags?.Invoke();
                            RenderTagDialog(uploadable, dialog, relation, onUpdateTags);

                            EditorManager.inst.history.Add(new History.Command("Add tag",
                                () =>
                                {
                                    uploadable.ArcadeTags.Add(tag);
                                    OpenTagDialog(uploadable, dialog, relation, onUpdateTags);
                                },
                                () =>
                                {
                                    uploadable.ArcadeTags.RemoveAt(index);
                                    OpenTagDialog(uploadable, dialog, relation, onUpdateTags);
                                }));
                        }, relation)),
                        new ButtonFunction("Clear Tags", () =>
                        {
                            var tags = new List<string>(uploadable.ArcadeTags);
                            uploadable.ArcadeTags.Clear();
                            onUpdateTags?.Invoke();
                            RenderTagDialog(uploadable, dialog, relation, onUpdateTags);

                            EditorManager.inst.history.Add(new History.Command("Clear tags",
                                () =>
                                {
                                    uploadable.ArcadeTags.Clear();
                                    OpenTagDialog(uploadable, dialog, relation, onUpdateTags);
                                },
                                () =>
                                {
                                    uploadable.ArcadeTags.AddRange(tags);
                                    OpenTagDialog(uploadable, dialog, relation, onUpdateTags);
                                }));
                        }));
                    return;
                }

                var index = uploadable.ArcadeTags.Count;
                uploadable.ArcadeTags.Add(DEFAULT_NEW_TAG);
                onUpdateTags?.Invoke();
                RenderTagDialog(uploadable, dialog, relation, onUpdateTags);

                EditorManager.inst.history.Add(new History.Command("Add tag",
                    () =>
                    {
                        uploadable.ArcadeTags.Add(DEFAULT_NEW_TAG);
                        OpenTagDialog(uploadable, dialog, relation, onUpdateTags);
                    },
                    () =>
                    {
                        uploadable.ArcadeTags.RemoveAt(index);
                        OpenTagDialog(uploadable, dialog, relation, onUpdateTags);
                    }));
            };
        }

        #endregion

        #region Search

        /// <summary>
        /// Searches the server.
        /// </summary>
        public void Search()
        {
            switch (tab)
            {
                case Tab.Levels: {
                        CoroutineHelper.StartCoroutine(GetLevels());
                        break;
                    }
                case Tab.LevelCollections: {
                        CoroutineHelper.StartCoroutine(GetLevelCollections());
                        break;
                    }
                case Tab.Prefabs: {
                        CoroutineHelper.StartCoroutine(GetPrefabs());
                        break;
                    }
            }
        }

		IEnumerator GetLevels()
        {
			if (loadingOnlineLevels)
				yield break;

			loadingOnlineLevels = true;

            Dialog.ClearContent();

            var page = tabSettings[Tab.Levels].page;
            int currentPage = page + 1;

            var search = Dialog.SearchTerm;

            string query = AlephNetwork.BuildQuery(SearchURL, search, page, CurrentTabSettings.sort, CurrentTabSettings.ascend);

            CoreHelper.Log($"Search query: {query}");

            var headers = new Dictionary<string, string>();
			if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
				headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

			yield return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile(query, json =>
			{
				try
				{
					var jn = JSON.Parse(json);

                    if (jn["items"] == null)
                        return;

					for (int i = 0; i < jn["items"].Count; i++)
                    {
						var item = jn["items"][i];

                        string id = item["id"];

                        string artist = item["artist"];
                        string title = item["title"];
                        string name = item["name"];
                        string creator = item["creator"];
                        string description = item["description"];
                        var difficulty = item["difficulty"].AsInt;

                        if (id == null || id == "0")
                            continue;

                        var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(Dialog.Content, $"Folder [{name}]");
                        var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                        var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();

                        folderButtonStorage.label.text =
                            $"<b>Name</b>: {name}\n" +
                            $"<b>Song</b>: {artist} - {title}\n" +
                            $"<b>Description</b>:\n{description}";
                        RectValues.FullAnchored.AnchorMin(0.15f, 0f).SizeDelta(-32f, -8f).AssignToRectTransform(folderButtonStorage.label.rectTransform);

                        gameObject.transform.AsRT().sizeDelta = new Vector2(0f, 200f);

                        //folderButtonStorage.text.horizontalOverflow = horizontalOverflow;
                        //folderButtonStorage.text.verticalOverflow = verticalOverflow;
                        //folderButtonStorage.text.fontSize = fontSize;

                        folderButtonStorage.button.onClick.ClearAll();
                        folderButtonFunction.onClick = pointerEventData =>
                        {
                            if (pointerEventData.button == PointerEventData.InputButton.Right)
                            {
                                EditorContextMenu.inst.ShowContextMenu(
                                    new ButtonFunction("Download", () => DownloadLevel(item)),
                                    new ButtonFunction(true),
                                    new ButtonFunction("Copy Server ID", () =>
                                    {
                                        LSText.CopyToClipboard(id);
                                        EditorManager.inst.DisplayNotification($"Copied ID: {id} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
                                    }));
                                return;
                            }

                            RTEditor.inst.ShowWarningPopup("Are you sure you want to download this Level to your editor folder?", () =>
                            {
                                RTEditor.inst.HideWarningPopup();
                                DownloadLevel(item);
                            }, RTEditor.inst.HideWarningPopup);
                        };

                        EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
                        EditorThemeManager.ApplyLightText(folderButtonStorage.label);

                        var iconBase = Creator.NewUIObject("icon base", gameObject.transform);
                        var iconBaseImage = iconBase.AddComponent<Image>();
                        iconBase.AddComponent<Mask>().showMaskGraphic = false;
                        iconBase.transform.AsRT().anchoredPosition = new Vector2(-300f, 32f);
                        iconBase.transform.AsRT().sizeDelta = new Vector2(90f, 90f);
                        EditorThemeManager.ApplyGraphic(iconBaseImage, ThemeGroup.Null, true);

                        var icon = Creator.NewUIObject("icon", iconBase.transform);
                        var iconImage = icon.AddComponent<Image>();

                        icon.transform.AsRT().anchoredPosition = Vector3.zero;
                        icon.transform.AsRT().sizeDelta = new Vector2(90f, 90f);

                        if (OnlineLevelIcons.TryGetValue(id, out Sprite sprite))
                            iconImage.sprite = sprite;
                        else
                        {
                            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{AlephNetwork.LevelCoverURL}{id}{FileFormat.JPG.Dot()}", bytes =>
                            {
                                var sprite = SpriteHelper.LoadSprite(bytes);
                                OnlineLevelIcons.Add(id, sprite);
                                if (iconImage)
                                    iconImage.sprite = sprite;
                            }, onError =>
                            {
                                var sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                                OnlineLevelIcons.Add(id, sprite);
                                if (iconImage)
                                    iconImage.sprite = sprite;
                            }));
                        }

                    }

					if (jn["count"] != null)
                        itemCount = jn["count"].AsInt;
				}
				catch (Exception ex)
				{
					CoreHelper.LogException(ex);
				}
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
                                CoroutineHelper.StartCoroutine(RefreshTokens(Search));
                                return;
                            }
                            ShowLoginPopup(Search);
                            break;
                        }
                    default: {
                            EditorManager.inst.DisplayNotification($"Level search failed. Error code: {onError}", 2f, EditorManager.NotificationType.Error);
                            break;
                        }
                }

                if (errorMsg != null)
                    CoreHelper.LogError($"Error Message: {errorMsg}");
            }, headers));

			loadingOnlineLevels = false;
		}

        public void DownloadLevel(JSONNode jn, Action onDownload = null)
        {
            var name = jn["name"].Value;
            EditorManager.inst.DisplayNotification($"Downloading {name}, please wait...", 3f, EditorManager.NotificationType.Success);
            name = RTString.ReplaceFormatting(name); // for cases where a user has used symbols not allowed.
            name = RTFile.ValidateDirectory(name);
            var directory = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, $"{name} [{jn["id"].Value}]");
            DownloadLevel(jn["id"], directory, name, onDownload);
        }

        public void DownloadLevel(string id, string directory, string name, Action onDownload = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                EditorManager.inst.DisplayNotification($"No server ID!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{AlephNetwork.LevelDownloadURL}{id}.zip", bytes =>
            {
                RTFile.DeleteDirectory(directory);
                Directory.CreateDirectory(directory);

                File.WriteAllBytes($"{directory}{FileFormat.ZIP.Dot()}", bytes);

                ZipFile.ExtractToDirectory($"{directory}{FileFormat.ZIP.Dot()}", directory);

                File.Delete($"{directory}{FileFormat.ZIP.Dot()}");

                EditorLevelManager.inst.LoadLevels();
                EditorManager.inst.DisplayNotification($"Downloaded {name}!", 1.5f, EditorManager.NotificationType.Success);

                onDownload?.Invoke();
                RTEditor.inst.ProgressPopup.Close();
            }, onError =>
            {
                EditorManager.inst.DisplayNotification($"Failed to download {name}.", 1.5f, EditorManager.NotificationType.Error);
                CoreHelper.LogError($"OnError: {onError}");
                RTEditor.inst.ProgressPopup.Close();
            }));
        }

        IEnumerator GetLevelCollections()
        {
			if (loadingOnlineLevels)
				yield break;

			loadingOnlineLevels = true;

            Dialog.ClearContent();

            var page = tabSettings[Tab.LevelCollections].page;
            int currentPage = page + 1;

            var search = Dialog.SearchTerm;

            string query = AlephNetwork.BuildQuery(SearchURL, search, page, CurrentTabSettings.sort, CurrentTabSettings.ascend);

            CoreHelper.Log($"Search query: {query}");

            var headers = new Dictionary<string, string>();
			if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
				headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

			yield return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile(query, json =>
			{
				try
				{
					var jn = JSON.Parse(json);

                    if (jn["items"] == null)
                        return;

					for (int i = 0; i < jn["items"].Count; i++)
                    {
						var item = jn["items"][i];

                        string id = item["id"];

                        string name = item["name"];
                        string creator = item["creator"];
                        string description = item["description"];
                        var difficulty = item["difficulty"].AsInt;

                        if (id == null || id == "0")
                            continue;

                        var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(Dialog.Content, $"Folder [{name}]");
                        var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                        var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();

                        folderButtonStorage.label.text =
                            $"<b>Name</b>: {name}\n" +
                            $"<b>Description</b>:\n{description}";
                        RectValues.FullAnchored.AnchorMin(0.15f, 0f).SizeDelta(-32f, -8f).AssignToRectTransform(folderButtonStorage.label.rectTransform);

                        gameObject.transform.AsRT().sizeDelta = new Vector2(0f, 200f);

                        //folderButtonStorage.text.horizontalOverflow = horizontalOverflow;
                        //folderButtonStorage.text.verticalOverflow = verticalOverflow;
                        //folderButtonStorage.text.fontSize = fontSize;

                        folderButtonStorage.button.onClick.ClearAll();
                        folderButtonFunction.onClick = pointerEventData =>
                        {
                            if (pointerEventData.button == PointerEventData.InputButton.Right)
                            {
                                EditorContextMenu.inst.ShowContextMenu(
                                    new ButtonFunction("Download", () => DownloadLevelCollection(item)),
                                    new ButtonFunction(true),
                                    new ButtonFunction("Copy Server ID", () =>
                                    {
                                        LSText.CopyToClipboard(id);
                                        EditorManager.inst.DisplayNotification($"Copied ID: {id} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
                                    }));
                                return;
                            }

                            RTEditor.inst.ShowWarningPopup("Are you sure you want to download this Level Collection to your editor folder?", () =>
                            {
                                RTEditor.inst.HideWarningPopup();
                                DownloadLevelCollection(item);
                            }, RTEditor.inst.HideWarningPopup);
                        };

                        EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
                        EditorThemeManager.ApplyLightText(folderButtonStorage.label);

                        var iconBase = Creator.NewUIObject("icon base", gameObject.transform);
                        var iconBaseImage = iconBase.AddComponent<Image>();
                        iconBase.AddComponent<Mask>().showMaskGraphic = false;
                        iconBase.transform.AsRT().anchoredPosition = new Vector2(-300f, 32f);
                        iconBase.transform.AsRT().sizeDelta = new Vector2(90f, 90f);
                        EditorThemeManager.ApplyGraphic(iconBaseImage, ThemeGroup.Null, true);

                        var icon = Creator.NewUIObject("icon", iconBase.transform);
                        var iconImage = icon.AddComponent<Image>();

                        icon.transform.AsRT().anchoredPosition = Vector3.zero;
                        icon.transform.AsRT().sizeDelta = new Vector2(90f, 90f);

                        if (OnlineLevelIcons.TryGetValue(id, out Sprite sprite))
                            iconImage.sprite = sprite;
                        else
                        {
                            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{AlephNetwork.LevelCollectionCoverURL}{id}{FileFormat.JPG.Dot()}", bytes =>
                            {
                                var sprite = SpriteHelper.LoadSprite(bytes);
                                OnlineLevelIcons.Add(id, sprite);
                                if (iconImage)
                                    iconImage.sprite = sprite;
                            }, onError =>
                            {
                                var sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                                OnlineLevelIcons.Add(id, sprite);
                                if (iconImage)
                                    iconImage.sprite = sprite;
                            }));
                        }

                    }

					if (jn["count"] != null)
                        itemCount = jn["count"].AsInt;
				}
				catch (Exception ex)
				{
					CoreHelper.LogException(ex);
				}
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
                                CoroutineHelper.StartCoroutine(RefreshTokens(Search));
                                return;
                            }
                            ShowLoginPopup(Search);
                            break;
                        }
                    default: {
                            EditorManager.inst.DisplayNotification($"Level search failed. Error code: {onError}", 2f, EditorManager.NotificationType.Error);
                            break;
                        }
                }

                if (errorMsg != null)
                    CoreHelper.LogError($"Error Message: {errorMsg}");
            }, headers));

			loadingOnlineLevels = false;
        }

        public void DownloadLevelCollection(JSONNode jn, Action onDownload = null)
        {
            var name = jn["name"].Value;
            EditorManager.inst.DisplayNotification($"Downloading {name}, please wait...", 3f, EditorManager.NotificationType.Success);
            name = RTString.ReplaceFormatting(name); // for cases where a user has used symbols not allowed.
            name = RTFile.ValidateDirectory(name);
            var directory = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.CollectionsPath, $"{name} [{jn["id"].Value}]");
            DownloadLevelCollection(jn["id"], directory, name, onDownload);
        }

        public void DownloadLevelCollection(string id, string directory, string name, Action onDownload = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                EditorManager.inst.DisplayNotification($"No server ID!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{AlephNetwork.LevelCollectionDownloadURL}{id}.zip", bytes =>
            {
                RTFile.DeleteDirectory(directory);
                Directory.CreateDirectory(directory);

                File.WriteAllBytes($"{directory}{FileFormat.ZIP.Dot()}", bytes);

                ZipFile.ExtractToDirectory($"{directory}{FileFormat.ZIP.Dot()}", directory);

                File.Delete($"{directory}{FileFormat.ZIP.Dot()}");

                EditorLevelManager.inst.LoadLevelCollections();
                EditorManager.inst.DisplayNotification($"Downloaded {name}!", 1.5f, EditorManager.NotificationType.Success);

                onDownload?.Invoke();
                RTEditor.inst.ProgressPopup.Close();
            }, onError =>
            {
                EditorManager.inst.DisplayNotification($"Failed to download {name}.", 1.5f, EditorManager.NotificationType.Error);
                CoreHelper.LogError($"OnError: {onError}");
                RTEditor.inst.ProgressPopup.Close();
            }));
        }

        public IEnumerator GetPrefabs()
        {
			if (loadingOnlineLevels)
				yield break;

			loadingOnlineLevels = true;

            Dialog.ClearContent();

            var page = tabSettings[Tab.Prefabs].page;
            int currentPage = page + 1;

            var search = Dialog.SearchTerm;

            string query = AlephNetwork.BuildQuery(SearchURL, search, page, CurrentTabSettings.sort, CurrentTabSettings.ascend);

            CoreHelper.Log($"Search query: {query}");

            var headers = new Dictionary<string, string>();
			if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
				headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

			yield return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile(query, json =>
			{
				try
				{
					var jn = JSON.Parse(json);

                    if (jn["items"] == null)
                        return;

					for (int i = 0; i < jn["items"].Count; i++)
                    {
						var item = jn["items"][i];

                        string id = item["id"];

                        string name = item["name"];
                        string creator = item["creator"];
                        string description = item["description"];
                        string typeName = item["typeName"];
                        int typeColor = item["typeColor"].AsInt;

                        if (id == null || id == "0")
                            continue;

                        var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(Dialog.Content, $"Folder [{name}]");
                        var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                        var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();

                        folderButtonStorage.Text =
                            $"<b>Name</b>: {name}\n" +
                            $"<b>Type</b>: {typeName}\n" +
                            $"<b>Creator</b>: {creator}\n" +
                            $"<b>Description</b>:\n{description}";
                        RectValues.FullAnchored.AnchorMin(0.15f, 0f).SizeDelta(-32f, -8f).AssignToRectTransform(folderButtonStorage.label.rectTransform);

                        gameObject.transform.AsRT().sizeDelta = new Vector2(0f, 200f);

                        //folderButtonStorage.text.horizontalOverflow = horizontalOverflow;
                        //folderButtonStorage.text.verticalOverflow = verticalOverflow;
                        //folderButtonStorage.text.fontSize = fontSize;

                        folderButtonStorage.button.onClick.ClearAll();
                        folderButtonFunction.onClick = pointerEventData =>
                        {
                            if (pointerEventData.button == PointerEventData.InputButton.Right)
                            {
                                EditorContextMenu.inst.ShowContextMenu(
                                    new ButtonFunction("Download to External", () => DownloadPrefab(item, ObjectSource.External)),
                                    new ButtonFunction("Download to Internal", () => DownloadPrefab(item, ObjectSource.Internal)),
                                    new ButtonFunction(true),
                                    new ButtonFunction("Copy Server ID", () =>
                                    {
                                        LSText.CopyToClipboard(id);
                                        EditorManager.inst.DisplayNotification($"Copied ID: {id} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
                                    }));
                                return;
                            }

                            RTEditor.inst.ShowWarningPopup("Are you sure you want to download this Prefab to your editor folder?", () =>
                            {
                                RTEditor.inst.HideWarningPopup();
                                DownloadPrefab(item);
                            }, RTEditor.inst.HideWarningPopup);
                        };

                        EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
                        EditorThemeManager.ApplyLightText(folderButtonStorage.label);

                        var type = Creator.NewUIObject("type", gameObject.transform);
                        var typeImage = type.AddComponent<Image>();
                        type.transform.AsRT().anchoredPosition = new Vector2(-300f, 32f);
                        type.transform.AsRT().sizeDelta = new Vector2(100f, 100f);
                        EditorThemeManager.ApplyGraphic(typeImage, ThemeGroup.Null, true);
                        typeImage.color = RTColors.HexToColor(typeColor.ToString(RTColors.X2));

                        var iconBase = Creator.NewUIObject("icon base", type.transform);
                        var iconBaseImage = iconBase.AddComponent<Image>();
                        iconBase.AddComponent<Mask>().showMaskGraphic = false;
                        iconBase.transform.AsRT().anchoredPosition = new Vector2(0f, 0f);
                        iconBase.transform.AsRT().sizeDelta = new Vector2(90f, 90f);
                        EditorThemeManager.ApplyGraphic(iconBaseImage, ThemeGroup.Null, true);

                        var icon = Creator.NewUIObject("icon", iconBase.transform);
                        var iconImage = icon.AddComponent<Image>();

                        icon.transform.AsRT().anchoredPosition = Vector3.zero;
                        icon.transform.AsRT().sizeDelta = new Vector2(90f, 90f);

                        if (OnlineLevelIcons.TryGetValue(id, out Sprite sprite))
                            iconImage.sprite = sprite;
                        else
                        {
                            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{AlephNetwork.PrefabCoverURL}{id}{FileFormat.JPG.Dot()}", bytes =>
                            {
                                var sprite = SpriteHelper.LoadSprite(bytes);
                                OnlineLevelIcons.Add(id, sprite);
                                if (iconImage)
                                    iconImage.sprite = sprite;
                            }, onError =>
                            {
                                var sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                                OnlineLevelIcons.Add(id, sprite);
                                if (iconImage)
                                    iconImage.sprite = sprite;
                            }));
                        }

                    }

					if (jn["count"] != null)
                        itemCount = jn["count"].AsInt;
				}
				catch (Exception ex)
				{
					CoreHelper.LogException(ex);
				}
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
                                CoroutineHelper.StartCoroutine(RefreshTokens(Search));
                                return;
                            }
                            ShowLoginPopup(Search);
                            break;
                        }
                    default: {
                            EditorManager.inst.DisplayNotification($"Level search failed. Error code: {onError}", 2f, EditorManager.NotificationType.Error);
                            break;
                        }
                }

                if (errorMsg != null)
                    CoreHelper.LogError($"Error Message: {errorMsg}");
            }, headers));

			loadingOnlineLevels = false;
        }

        public void DownloadPrefab(JSONNode jn, ObjectSource source = ObjectSource.External, Action onDownload = null)
        {
            var name = jn["name"].Value;
            EditorManager.inst.DisplayNotification($"Downloading {name}, please wait...", 3f, EditorManager.NotificationType.Success);
            name = RTString.ReplaceFormatting(name); // for cases where a user has used symbols not allowed.
            name = RTFile.ValidateFileName(name);
            DownloadPrefab(jn["id"], name, source, onDownload);
        }

        public void DownloadPrefab(string id, string name, ObjectSource source = ObjectSource.External, Action onDownload = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                EditorManager.inst.DisplayNotification($"No server ID!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{AlephNetwork.PrefabDownloadURL}{id}.lsp", bytes =>
            {
                DownloadPrefabType(id, name, bytes, source);
                onDownload?.Invoke();
            }, onError =>
            {
                EditorManager.inst.DisplayNotification($"Failed to download {name}.", 1.5f, EditorManager.NotificationType.Error);
                CoreHelper.LogError($"OnError: {onError}");
                RTEditor.inst.ProgressPopup.Close();
            }));
        }

        /// <summary>
        /// Checks the Server for a Prefab Type.
        /// </summary>
        /// <param name="id">ID of the Prefab on the Server.</param>
        /// <param name="name">Name of the Prefab.</param>
        /// <param name="bytes">Byte data of the Prefab file.</param>
        public void DownloadPrefabType(string id, string name, byte[] bytes, ObjectSource source = ObjectSource.External)
        {
            // if the user does not have the Prefab's Prefab Type locally, download it off the server.
            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{AlephNetwork.PrefabDownloadURL}{id}_type.lspt", typeBytes =>
            {
                var tempFilePath = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, $"{id}.lspt");
                File.WriteAllBytes(tempFilePath, typeBytes);
                var tempFile = RTFile.ReadFromFile(tempFilePath);
                if (string.IsNullOrEmpty(tempFile))
                {
                    SaveDownloadedPrefab(id, name, bytes, source);
                    RTFile.DeleteFile(tempFilePath);
                    return;
                }

                var jn = JSON.Parse(tempFile);
                var typeID = jn["id"];
                if (!string.IsNullOrEmpty(typeID) && !RTPrefabEditor.inst.prefabTypes.Has(x => x.id == typeID))
                    RTFile.MoveFile(tempFilePath, RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabTypePath, $"{jn["name"].Value} - {typeID.Value}"));
                else
                    RTFile.DeleteFile(tempFilePath);
                SaveDownloadedPrefab(id, name, bytes, source);
            }, onError => SaveDownloadedPrefab(id, name, bytes, source)));
        }

        void SaveDownloadedPrefab(string id, string name, byte[] bytes, ObjectSource source = ObjectSource.External)
        {
            RTEditor.inst.ProgressPopup.Close();

            if (source == ObjectSource.Internal)
            {
                var tempFilePath = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, $"{id}.lsp");
                File.WriteAllBytes(tempFilePath, bytes);
                var tempFile = RTFile.ReadFromFile(tempFilePath);
                if (string.IsNullOrEmpty(tempFile))
                {
                    RTFile.DeleteFile(tempFilePath);
                    return;
                }

                var jn = JSON.Parse(tempFile);
                RTPrefabEditor.inst.ImportPrefabIntoLevel(Prefab.Parse(jn));
                RTFile.DeleteFile(tempFilePath);
                EditorManager.inst.DisplayNotification($"Downloaded {name} and added it to the level!", 1.5f, EditorManager.NotificationType.Success);
                return;
            }

            RTEditor.inst.DisablePrefabWatcher();
            var file = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath, RTFile.FormatLegacyFileName(name) + FileFormat.LSP.Dot());
            if (RTPrefabEditor.inst.PrefabPanels.TryFind(x => x.Item && x.Item.ServerID == id, out PrefabPanel prefabPanel))
                file = prefabPanel.Path;
            File.WriteAllBytes(file, bytes);
            if (prefabPanel)
            {
                prefabPanel.Item = RTFile.CreateFromFile<Prefab>(file);
                prefabPanel.Render();
            }
            else
                CoroutineHelper.StartCoroutine(RTPrefabEditor.inst.LoadPrefabs());
            RTEditor.inst.EnablePrefabWatcher();
            EditorManager.inst.DisplayNotification($"Downloaded {name}!", 1.5f, EditorManager.NotificationType.Success);

        }

        #endregion

        #region Login

        public void ShowLoginPopup(Action onLogin) => RTEditor.inst.ShowWarningPopup("You are not logged in.", () =>
        {
            Application.OpenURL($"{AlephNetwork.ArcadeServerURL}api/auth/login");
            CreateLoginListener(onLogin);
            RTEditor.inst.HideWarningPopup();
        }, RTEditor.inst.HideWarningPopup, "Login", "Cancel");

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

        #endregion
    }
}
