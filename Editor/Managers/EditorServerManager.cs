using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

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
        }

        #endregion

        #region Values

        public bool uploading;

        #endregion

        #region Methods
        
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

                CoroutineHelper.StartCoroutine(AlephNetwork.UploadBytes(url, File.ReadAllBytes(path), id =>
                {
                    uploading = false;
                    uploadable.ServerID = id;
                    saveFile?.Invoke();

                    RTFile.DeleteFile(path);

                    EditorManager.inst.DisplayNotification($"Item uploaded! ID: {id}", 3f, EditorManager.NotificationType.Success);
                    onUpload?.Invoke();

                    AchievementManager.inst.UnlockAchievement("upload_level");
                }, (string onError, long responseCode, string errorMsg) =>
                {
                    uploading = false;
                    // Only downgrade if server ID wasn't already assigned.
                    if (string.IsNullOrEmpty(MetaData.Current.serverID))
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
                                    CoroutineHelper.StartCoroutine(RTMetaDataEditor.inst.RefreshTokens(() => Upload(url, fileName, uploadable, transfer, saveFile, onUpload)));
                                    return;
                                }
                                RTMetaDataEditor.inst.ShowLoginPopup(() => Upload(url, fileName, uploadable, transfer, saveFile, onUpload));
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

        public void Delete(string url, IUploadable uploadable, Action saveFile, Action onDelete)
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

                    var id = uploadable.ServerID;

                    var headers = new Dictionary<string, string>();
                    if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                        headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

                    CoroutineHelper.StartCoroutine(AlephNetwork.Delete(RTFile.CombinePaths(url, id), () =>
                    {
                        uploading = false;
                        uploadable.DatePublished = string.Empty;
                        uploadable.ServerID = null;
                        saveFile?.Invoke();

                        EditorManager.inst.DisplayNotification($"Successfully deleted item off the Arcade server.", 2.5f, EditorManager.NotificationType.Success);
                        onDelete?.Invoke();
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
                                        CoroutineHelper.StartCoroutine(RTMetaDataEditor.inst.RefreshTokens(() => Delete(url, uploadable, saveFile, onDelete)));
                                        return;
                                    }
                                    RTMetaDataEditor.inst.ShowLoginPopup(() => Delete(url, uploadable, saveFile, onDelete));
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

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile(RTFile.CombinePaths(url, serverID), json =>
            {
                EditorManager.inst.DisplayNotification($"Item is on server! {serverID}", 3f, EditorManager.NotificationType.Success);
                onVerify?.Invoke(true);
            }, (string onError, long responseCode, string errorMsg) =>
            {
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
                                CoroutineHelper.StartCoroutine(RTMetaDataEditor.inst.RefreshTokens(() => Verify(url, uploadable, saveFile, onVerify)));
                                return;
                            }
                            RTMetaDataEditor.inst.ShowLoginPopup(() => Verify(url, uploadable, saveFile, onVerify));
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

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile(RTFile.CombinePaths(url, serverID), json =>
            {
                pull.Invoke(JSON.Parse(json));
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
                                CoroutineHelper.StartCoroutine(RTMetaDataEditor.inst.RefreshTokens(() => Pull(url, uploadable, pull)));
                                return;
                            }
                            RTMetaDataEditor.inst.ShowLoginPopup(() => Pull(url, uploadable, pull));
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
    }
}
