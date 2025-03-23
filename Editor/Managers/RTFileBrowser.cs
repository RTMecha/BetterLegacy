using System;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using TMPro;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Class used to browse files while still in the editor, allowing users to select files outside of the game directory.
    /// </summary>
    public class RTFileBrowser : MonoBehaviour
    {
        public static RTFileBrowser inst;

        void Awake()
        {
            inst = this;
            title = transform.Find("Panel/Text").GetComponent<TextMeshProUGUI>();
            defaultDir = Directory.GetCurrentDirectory();

            try
            {
                transform.Find("content").gameObject.AddComponent<Button>();
                contentContextMenu = transform.Find("content").gameObject.AddComponent<ContextClickable>();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        ContextClickable contentContextMenu;

        public void UpdateBrowserFile(string[] fileExtensions, Action<string> onSelectFile = null)
        {
            var selectedPath = Directory.GetCurrentDirectory();
            if (EditorConfig.Instance.FileBrowserRemembersLocation.Value && RTFile.DirectoryExists(defaultDir))
                selectedPath = defaultDir;
            UpdateBrowserFile(selectedPath, fileExtensions, onSelectFile);
        }

        public void UpdateBrowserFile(string selectedPath, string[] fileExtensions, Action<string> onSelectFile = null)
        {
            selectedPath = UpdatePath(selectedPath);
            contentContextMenu.onClick = null;
            contentContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(selectedPath, () => UpdateBrowserFile(selectedPath, fileExtensions, onSelectFile))));
            };

            title.text = $"<b>File Browser</b> ({RTString.ArrayToString(fileExtensions).ToLower()})";

            var dir = transform.Find("folder-bar").GetComponent<InputField>();
            dir.onValueChanged.ClearAll();
            dir.onValueChanged.AddListener(_val => UpdateBrowserFile(_val, fileExtensions, onSelectFile));

            LSHelpers.DeleteChildren(viewport);
            CoreHelper.Log($"Update Browser: [{selectedPath}]");
            var directoryInfo = new DirectoryInfo(selectedPath);
            defaultDir = selectedPath;

            string[] directories = Directory.GetDirectories(defaultDir);
            string[] files = Directory.GetFiles(defaultDir);

            if (directoryInfo.Parent != null)
            {
                string backStr = directoryInfo.Parent.FullName;
                var gameObject = backPrefab.Duplicate(viewport, backStr);
                var backButton = gameObject.GetComponent<Button>();
                backButton.onClick.AddListener(() => UpdateBrowserFile(backStr, fileExtensions, onSelectFile));

                EditorThemeManager.ApplyGraphic(backButton.image, ThemeGroup.Back_Button, true);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Back_Button_Text);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(1).GetComponent<Text>(), ThemeGroup.Back_Button_Text);
            }

            string[] array = directories;
            for (int i = 0; i < array.Length; i++)
            {
                string folder = array[i];
                string name = new DirectoryInfo(folder).Name;
                var gameObject = folderPrefab.Duplicate(viewport, name);
                var folderPrefabStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderPrefabStorage.label.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => { UpdateBrowserFile(folder, fileExtensions, onSelectFile); });

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Open", () => UpdateBrowserFile(folder, fileExtensions, onSelectFile)),
                        new ButtonFunction(true),
                        new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(selectedPath, () => UpdateBrowserFile(selectedPath, fileExtensions, onSelectFile))),
                        new ButtonFunction("Create folder inside", () => RTEditor.inst.ShowFolderCreator(folder, () => UpdateBrowserFile(folder, fileExtensions, onSelectFile))),
                        new ButtonFunction(true),
                        new ButtonFunction("Copy Path", () => LSText.CopyToClipboard(RTFile.ReplaceSlash(folder)))
                        );
                };

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.Folder_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.label, ThemeGroup.Folder_Button_Text);
            }

            array = files;
            for (int i = 0; i < array.Length; i++)
            {
                string fileName = array[i];
                var fileInfoFolder = new FileInfo(fileName);
                string name = fileInfoFolder.Name;
                if (!fileExtensions.Any(x => x.ToLower() == fileInfoFolder.Extension.ToLower()))
                    continue;

                var gameObject = filePrefab.Duplicate(viewport, name);
                var folderPrefabStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderPrefabStorage.label.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => onSelectFile?.Invoke(fileInfoFolder.FullName));

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    if (fileExtensions.Any(x => RTFile.FileIsAudio(x)))
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Use", () => onSelectFile?.Invoke(fileInfoFolder.FullName)),
                            new ButtonFunction("Preview", () => PreviewAudio(fileInfoFolder.FullName)),
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(selectedPath, () => UpdateBrowserFile(selectedPath, fileExtensions, onSelectFile))),
                            new ButtonFunction(true),
                            new ButtonFunction("Copy Path", () => LSText.CopyToClipboard(RTFile.ReplaceSlash(fileName)))
                            );
                    else
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Use", () => onSelectFile?.Invoke(fileInfoFolder.FullName)),
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(selectedPath, () => UpdateBrowserFile(selectedPath, fileExtensions, onSelectFile))),
                            new ButtonFunction(true),
                            new ButtonFunction("Copy Path", () => LSText.CopyToClipboard(RTFile.ReplaceSlash(fileName)))
                            );
                };

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.File_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.label, ThemeGroup.File_Button_Text);
            }

            folderBar.text = defaultDir;
        }

        public void UpdateBrowserFile(string fileExtension, string specificName = "", Action<string> onSelectFile = null)
        {
            var selectedPath = Directory.GetCurrentDirectory();
            if (EditorConfig.Instance.FileBrowserRemembersLocation.Value && RTFile.DirectoryExists(defaultDir))
                selectedPath = defaultDir;
            UpdateBrowserFile(selectedPath, fileExtension, specificName, onSelectFile);
        }

        public void UpdateBrowserFile(string selectedPath, string fileExtension, string specificName = "", Action<string> onSelectFile = null)
        {
            selectedPath = UpdatePath(selectedPath);
            contentContextMenu.onClick = null;
            contentContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(selectedPath, () => UpdateBrowserFile(selectedPath, fileExtension, specificName, onSelectFile))));
            };

            title.text = $"<b>File Browser</b> ({fileExtension.ToLower()})";

            var dir = transform.Find("folder-bar").GetComponent<InputField>();
            dir.onValueChanged.ClearAll();
            dir.onValueChanged.AddListener(_val => UpdateBrowserFile(_val, fileExtension, specificName, onSelectFile));

            LSHelpers.DeleteChildren(viewport);
            CoreHelper.Log($"Update Browser: [{selectedPath}]");
            var directoryInfo = new DirectoryInfo(selectedPath);
            defaultDir = selectedPath;

            string[] directories = Directory.GetDirectories(defaultDir);
            string[] files = Directory.GetFiles(defaultDir);

            if (directoryInfo.Parent != null)
            {
                string backStr = directoryInfo.Parent.FullName;
                var gameObject = backPrefab.Duplicate(viewport, backStr);
                var backButton = gameObject.GetComponent<Button>();
                backButton.onClick.ClearAll();
                backButton.onClick.AddListener(() => UpdateBrowserFile(backStr, fileExtension, specificName, onSelectFile));

                EditorThemeManager.ApplyGraphic(backButton.image, ThemeGroup.Back_Button, true);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Back_Button_Text);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(1).GetComponent<Text>(), ThemeGroup.Back_Button_Text);
            }

            string[] array = directories;
            for (int i = 0; i < array.Length; i++)
            {
                string folder = array[i];
                string name = new DirectoryInfo(folder).Name;
                var gameObject = folderPrefab.Duplicate(viewport, name);
                var folderPrefabStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderPrefabStorage.label.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => UpdateBrowserFile(folder, fileExtension, specificName, onSelectFile));

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Open", () => UpdateBrowserFile(folder, fileExtension, specificName, onSelectFile)),
                        new ButtonFunction(true),
                        new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(selectedPath, () => UpdateBrowserFile(selectedPath, fileExtension, specificName, onSelectFile))),
                        new ButtonFunction("Create folder inside", () => RTEditor.inst.ShowFolderCreator(folder, () => UpdateBrowserFile(folder, fileExtension, specificName, onSelectFile))),
                        new ButtonFunction(true),
                        new ButtonFunction("Copy Path", () => LSText.CopyToClipboard(RTFile.ReplaceSlash(folder)))
                        );
                };

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.Folder_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.label, ThemeGroup.Folder_Button_Text);
            }

            array = files;
            for (int i = 0; i < array.Length; i++)
            {
                string fileName = array[i];
                var fileInfoFolder = new FileInfo(fileName);
                string name = fileInfoFolder.Name;
                if (fileInfoFolder.Extension.ToLower() != fileExtension.ToLower() || !(specificName == "" || specificName.ToLower() + fileExtension.ToLower() == name.ToLower()))
                    continue;

                var gameObject = filePrefab.Duplicate(viewport, name);
                var folderPrefabStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderPrefabStorage.label.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => onSelectFile?.Invoke(fileInfoFolder.FullName));

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    var fileFormat = RTFile.GetFileFormat(fileName);
                    if (RTFile.ValidAudio(fileFormat))
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Use", () => onSelectFile?.Invoke(fileInfoFolder.FullName)),
                            new ButtonFunction("Preview", () => PreviewAudio(fileInfoFolder.FullName)),
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(selectedPath, () => UpdateBrowserFile(selectedPath, fileExtension, specificName, onSelectFile))),
                            new ButtonFunction(true),
                            new ButtonFunction("Copy Path", () => LSText.CopyToClipboard(RTFile.ReplaceSlash(fileName)))
                            );
                    else
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Use", () => onSelectFile?.Invoke(fileInfoFolder.FullName)),
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(selectedPath, () => UpdateBrowserFile(selectedPath, fileExtension, specificName, onSelectFile))),
                            new ButtonFunction(true),
                            new ButtonFunction("Copy Path", () => LSText.CopyToClipboard(RTFile.ReplaceSlash(fileName)))
                            );
                };

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.File_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.label, ThemeGroup.File_Button_Text);
            }

            folderBar.text = defaultDir;
        }

        public void UpdateBrowserFolder(Action<string> onSelectFolder = null)
        {
            var selectedPath = Directory.GetCurrentDirectory();
            if (EditorConfig.Instance.FileBrowserRemembersLocation.Value && RTFile.DirectoryExists(defaultDir))
                selectedPath = defaultDir;
            UpdateBrowserFolder(selectedPath, "", onSelectFolder);
        }
        
        public void UpdateBrowserFolder(string selectedPath, string specificName, Action<string> onSelectFolder = null)
        {
            selectedPath = UpdatePath(selectedPath);
            contentContextMenu.onClick = null;
            contentContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(selectedPath, () => UpdateBrowserFolder(selectedPath, specificName, onSelectFolder))));
            };

            title.text = $"<b>File Browser</b> (Right click on a folder to use)";

            var dir = transform.Find("folder-bar").GetComponent<InputField>();
            dir.onValueChanged.ClearAll();
            dir.onValueChanged.AddListener(_val => UpdateBrowserFolder(_val, specificName, onSelectFolder));

            LSHelpers.DeleteChildren(viewport);
            CoreHelper.Log($"Update Browser: [{selectedPath}]");
            var directoryInfo = new DirectoryInfo(selectedPath);
            defaultDir = selectedPath;

            string[] directories = Directory.GetDirectories(defaultDir);

            if (directoryInfo.Parent != null)
            {
                string backStr = directoryInfo.Parent.FullName;
                var gameObject = backPrefab.Duplicate(viewport, backStr);
                var backButton = gameObject.GetComponent<Button>();
                backButton.onClick.ClearAll();
                backButton.onClick.AddListener(() => UpdateBrowserFolder(backStr, specificName, onSelectFolder));

                EditorThemeManager.ApplyGraphic(backButton.image, ThemeGroup.Back_Button, true);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Back_Button_Text);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(1).GetComponent<Text>(), ThemeGroup.Back_Button_Text);
            }

            string[] array = directories;
            for (int i = 0; i < array.Length; i++)
            {
                string folder = array[i];
                string name = new DirectoryInfo(folder).Name;
                var gameObject = folderPrefab.Duplicate(viewport, name);
                var folderPrefabStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderPrefabStorage.label.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => UpdateBrowserFolder(folder, specificName, onSelectFolder));

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Use", () => onSelectFolder?.Invoke(folder)),
                        new ButtonFunction("Open", () => UpdateBrowserFolder(folder, specificName, onSelectFolder)),
                        new ButtonFunction(true),
                        new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(selectedPath, () => UpdateBrowserFolder(selectedPath, specificName, onSelectFolder))),
                        new ButtonFunction("Create folder inside", () => RTEditor.inst.ShowFolderCreator(folder, () => UpdateBrowserFolder(folder, specificName, onSelectFolder))),
                        new ButtonFunction(true),
                        new ButtonFunction("Copy Path", () => LSText.CopyToClipboard(RTFile.ReplaceSlash(folder)))
                        );
                };

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.Folder_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.label, ThemeGroup.Folder_Button_Text, true);
            }

            folderBar.text = defaultDir;
        }

        void PreviewAudio(string file)
        {
            CoreHelper.StartCoroutineAsync(AlephNetwork.DownloadAudioClip($"file://{file}", RTFile.GetAudioType(file), audioClip =>
            {
                CoreHelper.ReturnToUnity(() =>
                {
                    var length = EditorConfig.Instance.FileBrowserAudioPreviewLength.Value;
                    DestroyAudioPreview();
                    if (currentDestroyPreviewCoroutine != null)
                        StopCoroutine(currentDestroyPreviewCoroutine);
                    currentDestroyPreviewCoroutine = null;
                    currentPreview = Camera.main.gameObject.AddComponent<AudioSource>();
                    currentPreview.clip = audioClip;
                    currentPreview.playOnAwake = true;
                    currentPreview.loop = true;
                    currentPreview.volume = AudioManager.inst.sfxVol;
                    currentPreview.time = length < 0f || audioClip.length <= length ? 0f : UnityEngine.Random.Range(0f, audioClip.length);
                    currentPreview.Play();
                    DestroyAudioPreviewAfterSeconds(Mathf.Clamp(length, 0f, audioClip.length));
                });
            }));
        }

        void DestroyAudioPreviewAfterSeconds(float t) => currentDestroyPreviewCoroutine = CoreHelper.StartCoroutine(CoreHelper.IPerformActionAfterSeconds(t, DestroyAudioPreview));

        void DestroyAudioPreview()
        {
            if (!currentPreview)
                return;

            CoreHelper.Destroy(currentPreview);
            currentPreview = null;
            currentDestroyPreviewCoroutine = null;
        }

        string UpdatePath(string selectedPath) => !RTFile.DirectoryExists(selectedPath) ? Directory.GetCurrentDirectory() : selectedPath;

        Coroutine currentDestroyPreviewCoroutine;

        AudioSource currentPreview;

        public Transform viewport;

        public InputField folderBar;

        public GameObject filePrefab;

        public GameObject backPrefab;

        public GameObject folderPrefab;

        public InputField oggFileInput;

        public string defaultDir = "";

        public TextMeshProUGUI title;
    }
}
