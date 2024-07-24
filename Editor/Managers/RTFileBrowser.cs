using BetterLegacy.Components;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        }

        public void UpdateBrowser(string _folder, string[] fileExtensions, Action<string> onSelectFile = null)
        {
            if (!RTFile.DirectoryExists(_folder))
            {
                EditorManager.inst.DisplayNotification("Folder doesn't exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            title.text = $"<b>File Browser</b> ({FontManager.TextTranslater.ArrayToString(fileExtensions).ToLower()})";

            var dir = transform.Find("folder-bar").GetComponent<InputField>();
            dir.onValueChanged.ClearAll();
            dir.onValueChanged.AddListener(_val => { UpdateBrowser(_val, fileExtensions, onSelectFile); });

            LSHelpers.DeleteChildren(viewport);
            CoreHelper.Log($"Update Browser: [{_folder}]");
            var directoryInfo = new DirectoryInfo(_folder);
            defaultDir = _folder;

            string[] directories = Directory.GetDirectories(defaultDir);
            string[] files = Directory.GetFiles(defaultDir);

            if (directoryInfo.Parent != null)
            {
                string backStr = directoryInfo.Parent.FullName;
                var gameObject = backPrefab.Duplicate(viewport, backStr);
                var backButton = gameObject.GetComponent<Button>();
                backButton.onClick.AddListener(() => { UpdateBrowser(backStr, fileExtensions, onSelectFile); });

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
                folderPrefabStorage.text.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => { UpdateBrowser(folder, fileExtensions, onSelectFile); });

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.Folder_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.text, ThemeGroup.Folder_Button_Text);
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
                folderPrefabStorage.text.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => { onSelectFile?.Invoke(fileInfoFolder.FullName); });

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.File_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.text, ThemeGroup.File_Button_Text);
            }

            folderBar.text = defaultDir;
        }

        public void UpdateBrowser(string _folder, string fileExtension, string specificName = "", Action<string> onSelectFile = null)
        {
            if (!RTFile.DirectoryExists(_folder))
            {
                EditorManager.inst.DisplayNotification("Folder doesn't exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            title.text = $"<b>File Browser</b> ({fileExtension.ToLower()})";

            var dir = transform.Find("folder-bar").GetComponent<InputField>();
            dir.onValueChanged.ClearAll();
            dir.onValueChanged.AddListener(_val => { UpdateBrowser(_val, fileExtension, specificName, onSelectFile); });

            LSHelpers.DeleteChildren(viewport);
            CoreHelper.Log($"Update Browser: [{_folder}]");
            var directoryInfo = new DirectoryInfo(_folder);
            defaultDir = _folder;

            string[] directories = Directory.GetDirectories(defaultDir);
            string[] files = Directory.GetFiles(defaultDir);

            if (directoryInfo.Parent != null)
            {
                string backStr = directoryInfo.Parent.FullName;
                var gameObject = backPrefab.Duplicate(viewport, backStr);
                var backButton = gameObject.GetComponent<Button>();
                backButton.onClick.ClearAll();
                backButton.onClick.AddListener(() => { UpdateBrowser(backStr, fileExtension, specificName, onSelectFile); });

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
                folderPrefabStorage.text.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => { UpdateBrowser(folder, fileExtension, specificName, onSelectFile); });

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.Folder_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.text, ThemeGroup.Folder_Button_Text);
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
                    folderPrefabStorage.text.text = name;
                    folderPrefabStorage.button.onClick.ClearAll();
                    folderPrefabStorage.button.onClick.AddListener(() => { onSelectFile?.Invoke(fileInfoFolder.FullName); });

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.File_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.text, ThemeGroup.File_Button_Text);
            }

            folderBar.text = defaultDir;
        }

        public void UpdateBrowser(string _folder, string specificName = "", Action<string> onSelectFolder = null)
        {
            if (!RTFile.DirectoryExists(_folder))
            {
                EditorManager.inst.DisplayNotification("Folder doesn't exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            title.text = $"<b>File Browser</b> (Right click on a folder to use)";

            var dir = transform.Find("folder-bar").GetComponent<InputField>();
            dir.onValueChanged.ClearAll();
            dir.onValueChanged.AddListener(_val => { UpdateBrowser(_val, specificName, onSelectFolder); });

            LSHelpers.DeleteChildren(viewport);
            CoreHelper.Log($"Update Browser: [{_folder}]");
            var directoryInfo = new DirectoryInfo(_folder);
            defaultDir = _folder;

            string[] directories = Directory.GetDirectories(defaultDir);

            if (directoryInfo.Parent != null)
            {
                string backStr = directoryInfo.Parent.FullName;
                var gameObject = backPrefab.Duplicate(viewport, backStr);
                var backButton = gameObject.GetComponent<Button>();
                backButton.onClick.ClearAll();
                backButton.onClick.AddListener(() => { UpdateBrowser(backStr, specificName, onSelectFolder); });

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
                folderPrefabStorage.text.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => { UpdateBrowser(folder, specificName, onSelectFolder); });

                var clickable = gameObject.AddComponent<Clickable>();
                clickable.onDown = pointerEventData =>
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                        onSelectFolder?.Invoke(folder);
                };

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.Folder_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.text, ThemeGroup.Folder_Button_Text, true);
            }

            folderBar.text = defaultDir;
        }

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
