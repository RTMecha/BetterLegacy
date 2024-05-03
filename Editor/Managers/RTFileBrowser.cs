using BetterLegacy.Components;
using BetterLegacy.Core;
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
            if (Directory.Exists(_folder))
            {
                title.text = $"<b>File Browser</b> ({FontManager.TextTranslater.ArrayToString(fileExtensions).ToLower()})";

                var dir = transform.Find("folder-bar").GetComponent<InputField>();
                dir.onValueChanged.ClearAll();
                dir.onValueChanged.AddListener(delegate (string _val)
                {
                    UpdateBrowser(_val, fileExtensions, onSelectFile);
                });

                LSHelpers.DeleteChildren(viewport);
                Debug.LogFormat("Update Browser: [{0}]", new object[]
                {
                    _folder
                });
                var directoryInfo = new DirectoryInfo(_folder);
                defaultDir = _folder;
                string[] directories = Directory.GetDirectories(defaultDir);
                string[] files = Directory.GetFiles(defaultDir);
                if (directoryInfo.Parent != null)
                {
                    string backStr = directoryInfo.Parent.FullName;
                    var gameObject = backPrefab.Duplicate(viewport, backStr);
                    var backButton = gameObject.GetComponent<Button>();
                    backButton.onClick.AddListener(delegate ()
                    {
                        UpdateBrowser(backStr, fileExtensions, onSelectFile);
                    });

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Back_Button, gameObject, new List<Component>
                    {
                        backButton.image,
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Back_Button_Text, gameObject.transform.GetChild(0).gameObject, new List<Component>
                    {
                        gameObject.transform.GetChild(0).GetComponent<Image>(),
                    }));

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Back_Button_Text, gameObject.transform.GetChild(1).gameObject, new List<Component>
                    {
                        gameObject.transform.GetChild(1).GetComponent<Text>(),
                    }));
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
                    folderPrefabStorage.button.onClick.AddListener(delegate ()
                    {
                        UpdateBrowser(folder, fileExtensions, onSelectFile);
                    });

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Folder_Button, gameObject, new List<Component>
                    {
                        folderPrefabStorage.button.image,
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Folder_Button_Text, gameObject, new List<Component>
                    {
                        folderPrefabStorage.text,
                    }));
                }
                array = files;
                for (int i = 0; i < array.Length; i++)
                {
                    string fileName = array[i];
                    var fileInfoFolder = new FileInfo(fileName);
                    string name = fileInfoFolder.Name;
                    if (fileExtensions.Any(x => x.ToLower() == fileInfoFolder.Extension.ToLower()))
                    {
                        var gameObject = filePrefab.Duplicate(viewport, name);
                        var folderPrefabStorage = gameObject.GetComponent<FunctionButtonStorage>();
                        folderPrefabStorage.text.text = name;
                        folderPrefabStorage.button.onClick.ClearAll();
                        folderPrefabStorage.button.onClick.AddListener(delegate ()
                        {
                            onSelectFile?.Invoke(fileInfoFolder.FullName);
                        });

                        EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.File_Button, gameObject, new List<Component>
                        {
                            folderPrefabStorage.button.image,
                        }, true, 1, SpriteManager.RoundedSide.W));

                        EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.File_Button_Text, gameObject, new List<Component>
                        {
                            folderPrefabStorage.text,
                        }));
                    }
                }
                folderBar.text = defaultDir;
                return;
            }
            EditorManager.inst.DisplayNotification("Folder doesn't exist.", 2f, EditorManager.NotificationType.Error);
        }

        public void UpdateBrowser(string _folder, string fileExtension, string specificName = "", Action<string> onSelectFile = null)
        {
            if (Directory.Exists(_folder))
            {
                title.text = $"<b>File Browser</b> ({fileExtension.ToLower()})";

                var dir = transform.Find("folder-bar").GetComponent<InputField>();
                dir.onValueChanged.ClearAll();
                dir.onValueChanged.AddListener(delegate (string _val)
                {
                    UpdateBrowser(_val, fileExtension, specificName, onSelectFile);
                });

                LSHelpers.DeleteChildren(viewport);
                Debug.LogFormat("Update Browser: [{0}]", new object[]
                {
                    _folder
                });
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
                    backButton.onClick.AddListener(delegate ()
                    {
                        UpdateBrowser(backStr, fileExtension, specificName, onSelectFile);
                    });

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Back_Button, gameObject, new List<Component>
                    {
                        backButton.image,
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Back_Button_Text, gameObject.transform.GetChild(0).gameObject, new List<Component>
                    {
                        gameObject.transform.GetChild(0).GetComponent<Image>(),
                    }));

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Back_Button_Text, gameObject.transform.GetChild(1).gameObject, new List<Component>
                    {
                        gameObject.transform.GetChild(1).GetComponent<Text>(),
                    }));
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
                    folderPrefabStorage.button.onClick.AddListener(delegate ()
                    {
                        UpdateBrowser(folder, fileExtension, specificName, onSelectFile);
                    });

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Folder_Button, gameObject, new List<Component>
                    {
                        folderPrefabStorage.button.image,
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Folder_Button_Text, gameObject, new List<Component>
                    {
                        folderPrefabStorage.text,
                    }));
                }
                array = files;
                for (int i = 0; i < array.Length; i++)
                {
                    string fileName = array[i];
                    var fileInfoFolder = new FileInfo(fileName);
                    string name = fileInfoFolder.Name;
                    if (fileInfoFolder.Extension.ToLower() == fileExtension.ToLower() && (specificName == "" || specificName.ToLower() + fileExtension.ToLower() == name.ToLower()))
                    {
                        var gameObject = filePrefab.Duplicate(viewport, name);
                        var folderPrefabStorage = gameObject.GetComponent<FunctionButtonStorage>();
                        folderPrefabStorage.text.text = name;
                        folderPrefabStorage.button.onClick.ClearAll();
                        folderPrefabStorage.button.onClick.AddListener(delegate ()
                        {
                            onSelectFile?.Invoke(fileInfoFolder.FullName);
                        });

                        EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.File_Button, gameObject, new List<Component>
                        {
                            folderPrefabStorage.button.image,
                        }, true, 1, SpriteManager.RoundedSide.W));

                        EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.File_Button_Text, gameObject, new List<Component>
                        {
                            folderPrefabStorage.text,
                        }));
                    }
                }
                folderBar.text = defaultDir;
                return;
            }
            EditorManager.inst.DisplayNotification("Folder doesn't exist.", 2f, EditorManager.NotificationType.Error);
        }

        public void UpdateBrowser(string _folder, string specificName = "", Action<string> onSelectFolder = null)
        {
            if (Directory.Exists(_folder))
            {
                title.text = $"<b>File Browser</b> (Right click on a folder to use)";

                var dir = transform.Find("folder-bar").GetComponent<InputField>();
                dir.onValueChanged.ClearAll();
                dir.onValueChanged.AddListener(delegate (string _val)
                {
                    UpdateBrowser(_val, specificName, onSelectFolder);
                });

                LSHelpers.DeleteChildren(viewport);
                Debug.LogFormat("Update Browser: [{0}]", new object[]
                {
                    _folder
                });
                var directoryInfo = new DirectoryInfo(_folder);
                defaultDir = _folder;
                string[] directories = Directory.GetDirectories(defaultDir);
                if (directoryInfo.Parent != null)
                {
                    string backStr = directoryInfo.Parent.FullName;
                    var gameObject = backPrefab.Duplicate(viewport, backStr);
                    var backButton = gameObject.GetComponent<Button>();
                    backButton.onClick.ClearAll();
                    backButton.onClick.AddListener(delegate ()
                    {
                        UpdateBrowser(backStr, specificName, onSelectFolder);
                    });

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Back_Button, gameObject, new List<Component>
                    {
                        backButton.image,
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Back_Button_Text, gameObject.transform.GetChild(0).gameObject, new List<Component>
                    {
                        gameObject.transform.GetChild(0).GetComponent<Image>(),
                    }));

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Back_Button_Text, gameObject.transform.GetChild(1).gameObject, new List<Component>
                    {
                        gameObject.transform.GetChild(1).GetComponent<Text>(),
                    }));
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
                    folderPrefabStorage.button.onClick.AddListener(delegate ()
                    {
                        UpdateBrowser(folder, specificName, onSelectFolder);
                    });

                    var clickable = gameObject.AddComponent<Clickable>();
                    clickable.onDown = delegate (PointerEventData pointerEventData)
                    {
                        if (pointerEventData.button == PointerEventData.InputButton.Right)
                        {
                            onSelectFolder?.Invoke(folder);
                        }
                    };

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Folder_Button, gameObject, new List<Component>
                    {
                        folderPrefabStorage.button.image,
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Folder_Button_Text, gameObject, new List<Component>
                    {
                        folderPrefabStorage.text,
                    }));
                }
                folderBar.text = defaultDir;
                return;
            }
            EditorManager.inst.DisplayNotification("Folder doesn't exist.", 2f, EditorManager.NotificationType.Error);
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
